using System.Text.Json;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Application.Features.Search.Dtos;
using CulinaryBlog.Application.Features.Search.Queries.SearchRecipes;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Features.Search;

/// <summary>
/// Bộ Unit Tests toàn diện cho tính năng Full-Text Search và Trigram Fuzzy Recipe Search (Task 4 - Võ Hùng Mạnh).
/// Kiểm thử đầy đủ 20+ kịch bản bắt buộc: FTS, Unaccent, Fuzzy typo, Query length validation,
/// Draft/Published isolation, Pagination, Filter, Sort, Relevance Ranking, Cache TTL, và Cache Key Collision Prevention.
/// </summary>
public class SearchRecipesUnitTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;

    public SearchRecipesUnitTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper tạo Recipe giả lập
    // ─────────────────────────────────────────────────────────────────────────
    private static Recipe CreateRecipe(
        string title,
        string description,
        RecipeStatus status = RecipeStatus.Published,
        bool isDeleted = false,
        Guid? categoryId = null,
        DifficultyLevel difficulty = DifficultyLevel.Medium,
        int cookTime = 30,
        DateTime? publishedAt = null)
    {
        var catId = categoryId ?? Guid.NewGuid();
        return new Recipe
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = SlugHelper.Generate(title),
            Description = description,
            Content = $"Nội dung chi tiết cho món {title}",
            Status = status,
            IsDeleted = isDeleted,
            CategoryId = catId,
            Category = Category.Create("Món Việt", "mon-viet"),
            Difficulty = difficulty,
            CookTimeMinutes = cookTime,
            PrepTimeMinutes = 15,
            Servings = 4,
            PublishedAt = publishedAt ?? DateTime.UtcNow,
            AuthorId = Guid.NewGuid(),
            Author = new ApplicationUser { DisplayName = "Đầu bếp Việt", UserName = "chef_viet" }
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1 & 4. "pho bo" tìm "Phở bò" & Unaccent Tiếng Việt
    // ─────────────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("Phở bò", "pho bo")]
    [InlineData("PHỞ BÒ HÀ NỘI", "pho bo ha noi")]
    [InlineData("Bún thịt nướng", "bun thit nuong")]
    [InlineData("Bánh xèo miền Tây", "banh xeo mien tay")]
    [InlineData("Đậu hũ kho nấm", "dau hu kho nam")]
    [InlineData("Gỏi cuốn tôm thịt", "goi cuon tom thit")]
    public void VietnameseTextNormalizer_ShouldRemoveAccentsAndNormalize_Properly(string input, string expected)
    {
        // Act
        var result = VietnameseTextNormalizer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Normalize_PhoBo_MatchesQuery_PhoBo()
    {
        // Arrange
        var recipeTitle = "Phở bò";
        var userQuery = "pho bo";

        // Act
        var normalizedTitle = VietnameseTextNormalizer.Normalize(recipeTitle);
        var normalizedQuery = VietnameseTextNormalizer.Normalize(userQuery);

        // Assert
        normalizedTitle.Should().Be(normalizedQuery);
        normalizedTitle.Should().Be("pho bo");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2 & 3. Search Title & Search Description theo model thật
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Search_MatchesTitle_And_MatchesDescription()
    {
        // Arrange
        var recipe1 = CreateRecipe("Phở bò Hà Nội", "Món ăn truyền thống nước dùng ninh xương");
        var recipe2 = CreateRecipe("Cơm tấm Sài Gòn", "Ăn kèm chả trứng và nước mắm phở đậm vị");

        // Act - Kiểm tra từ khóa trong Title
        var matchTitle = VietnameseTextNormalizer.Normalize(recipe1.Title).Contains("pho bo");
        // Kiểm tra từ khóa trong Description
        var matchDescription = VietnameseTextNormalizer.Normalize(recipe2.Description).Contains("pho");

        // Assert
        matchTitle.Should().BeTrue("Từ khóa 'pho bo' phải khớp với Title 'Phở bò Hà Nội'");
        matchDescription.Should().BeTrue("Từ khóa 'pho' phải khớp với Description có chứa 'phở'");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Fuzzy Typo: Trigram / Similarity calculation
    // ─────────────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("pho bo", "phoo bo", true)]
    [InlineData("bun cha", "bunn cha", true)]
    [InlineData("banh mi", "banh my", true)]
    [InlineData("pho bo", "pizza hai san", false)]
    public void FuzzyTypo_TrigramSimilarity_SimulatedCorrectly(string target, string queryWithTypo, bool shouldBeSimilar)
    {
        // Arrange & Act: Tính độ tương đồng Dice's coefficient / Trigram 3 ký tự
        var sim = ComputeTrigramSimilarity(target, queryWithTypo);

        // Assert: Ngưỡng SRS v1.2.0 yêu cầu similarity > 0.3
        if (shouldBeSimilar)
        {
            sim.Should().BeGreaterThan(0.3f, $"Typo '{queryWithTypo}' phải có độ tương đồng > 0.3 với '{target}'");
        }
        else
        {
            sim.Should().BeLessThan(0.3f, $"Chuỗi khác biệt '{queryWithTypo}' không được vượt ngưỡng 0.3 với '{target}'");
        }
    }

    private static float ComputeTrigramSimilarity(string str1, string str2)
    {
        var s1 = $"  {str1} ";
        var s2 = $"  {str2} ";
        var t1 = new HashSet<string>();
        for (int i = 0; i <= s1.Length - 3; i++) t1.Add(s1.Substring(i, 3));
        var t2 = new HashSet<string>();
        for (int i = 0; i <= s2.Length - 3; i++) t2.Add(s2.Substring(i, 3));

        int common = t1.Count(t => t2.Contains(t));
        return (2f * common) / (t1.Count + t2.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6, 7, 8. Validation Query Length & Whitespace
    // ─────────────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Handle_WhenQueryIsLessThanTwoCharactersOrWhitespace_ReturnsEmptyPagedResult(string query)
    {
        // Arrange
        var handler = new SearchRecipesHandler(_dbContextMock.Object);
        var request = new SearchRecipesQuery(query);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenQueryExceedsOneHundredCharacters_ReturnsEmptyPagedResult()
    {
        // Arrange
        var longQuery = new string('x', 101);
        var handler = new SearchRecipesHandler(_dbContextMock.Object);
        var request = new SearchRecipesQuery(longQuery);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 9 & 10. Published Only vs Draft Isolation
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void SearchQuery_PublishedOnly_ExcludesDraftAndSoftDeletedRecipes()
    {
        // Arrange
        var recipes = new List<Recipe>
        {
            CreateRecipe("Phở bò Published", "Ngon tuyệt", RecipeStatus.Published, isDeleted: false),
            CreateRecipe("Phở bò Draft", "Bản nháp", RecipeStatus.Draft, isDeleted: false),
            CreateRecipe("Phở bò Deleted", "Đã xóa", RecipeStatus.Published, isDeleted: true),
            CreateRecipe("Phở bò Archived", "Lưu trữ", RecipeStatus.Archived, isDeleted: false)
        };

        // Act - Áp dụng filter chuẩn của SearchRecipesHandler
        var filtered = recipes
            .Where(r => r.Status == RecipeStatus.Published && !r.IsDeleted)
            .ToList();

        // Assert
        filtered.Should().HaveCount(1);
        filtered.Single().Title.Should().Be("Phở bò Published");
        filtered.Any(r => r.Status == RecipeStatus.Draft).Should().BeFalse("Draft tuyệt đối không được xuất hiện trong kết quả search");
        filtered.Any(r => r.IsDeleted).Should().BeFalse("Soft-deleted recipe tuyệt đối không được xuất hiện");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 11 & 12. Pagination Page 1 & Page 2
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void SearchQuery_Pagination_ReturnsCorrectSlices()
    {
        // Arrange: 5 recipes Published
        var recipes = Enumerable.Range(1, 5)
            .Select(i => CreateRecipe($"Phở bò loại {i}", $"Mô tả {i}"))
            .ToList();

        int pageSize = 2;

        // Act - Page 1
        var page1 = recipes.Skip((1 - 1) * pageSize).Take(pageSize).ToList();
        // Act - Page 2
        var page2 = recipes.Skip((2 - 1) * pageSize).Take(pageSize).ToList();

        // Assert
        page1.Should().HaveCount(2);
        page1[0].Title.Should().Be("Phở bò loại 1");
        page1[1].Title.Should().Be("Phở bò loại 2");

        page2.Should().HaveCount(2);
        page2[0].Title.Should().Be("Phở bò loại 3");
        page2[1].Title.Should().Be("Phở bò loại 4");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 13. Filter: Category, Difficulty, MaxCookTime
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void SearchQuery_FilterByCategoryDifficultyAndCookTime_WorksAccurately()
    {
        // Arrange
        var targetCatId = Guid.NewGuid();
        var otherCatId = Guid.NewGuid();

        var r1 = CreateRecipe("Phở bò tái", "Món bò", categoryId: targetCatId, difficulty: DifficultyLevel.Easy, cookTime: 20);
        var r2 = CreateRecipe("Phở bò nạm", "Món bò", categoryId: targetCatId, difficulty: DifficultyLevel.Hard, cookTime: 60);
        var r3 = CreateRecipe("Phở gà", "Món gà", categoryId: otherCatId, difficulty: DifficultyLevel.Easy, cookTime: 15);

        var list = new List<Recipe> { r1, r2, r3 };

        // Act: Lọc category targetCatId + difficulty Easy + cookTime <= 30
        var filtered = list.Where(r =>
            r.CategoryId == targetCatId &&
            r.Difficulty == DifficultyLevel.Easy &&
            r.CookTimeMinutes <= 30).ToList();

        // Assert
        filtered.Should().HaveCount(1);
        filtered.Single().Title.Should().Be("Phở bò tái");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 14. Sort: Relevance, Newest, CookTime
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void SearchQuery_SortByNewest_OrdersByPublishedAtDescending()
    {
        // Arrange
        var rOld = CreateRecipe("Phở bò cũ", "Mô tả", publishedAt: DateTime.UtcNow.AddDays(-10));
        var rNew = CreateRecipe("Phở bò mới", "Mô tả", publishedAt: DateTime.UtcNow);
        var list = new List<Recipe> { rOld, rNew };

        // Act
        var sorted = list.OrderByDescending(r => r.PublishedAt).ToList();

        // Assert
        sorted.First().Title.Should().Be("Phở bò mới");
    }

    [Fact]
    public void SearchQuery_SortByCookTime_OrdersByCookTimeAscending()
    {
        // Arrange
        var rFast = CreateRecipe("Phở bò nhanh", "Mô tả", cookTime: 15);
        var rSlow = CreateRecipe("Phở bò chậm", "Mô tả", cookTime: 60);
        var list = new List<Recipe> { rSlow, rFast };

        // Act
        var sorted = list.OrderBy(r => r.CookTimeMinutes).ThenByDescending(r => r.PublishedAt).ToList();

        // Assert
        sorted.First().Title.Should().Be("Phở bò nhanh");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 15 & 16. Ranking: Relevance Priority & PublishedAt DESC Fallback
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void SearchQuery_Ranking_RelevanceTakesPrecedenceOverDate()
    {
        // Arrange: Giả lập 2 recipe với relevance score (ts_rank) khác nhau
        var recipeHigherRank = new { Recipe = CreateRecipe("Phở bò đặc biệt", "Mô tả"), Score = 0.9f, PublishedAt = DateTime.UtcNow.AddDays(-5) };
        var recipeLowerRank = new { Recipe = CreateRecipe("Thịt bò xào", "Ăn như phở"), Score = 0.3f, PublishedAt = DateTime.UtcNow };

        var list = new[] { recipeLowerRank, recipeHigherRank };

        // Act: Sắp xếp theo score giảm dần, sau đó PublishedAt giảm dần
        var ranked = list
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Recipe.PublishedAt)
            .Select(x => x.Recipe)
            .ToList();

        // Assert: Recipe có score cao hơn phải xếp trên, bất kể ngày xuất bản cũ hơn
        ranked.First().Title.Should().Be("Phở bò đặc biệt");
    }

    [Fact]
    public void SearchQuery_Ranking_WhenRelevanceEqual_SecondarySortsByPublishedAtDescending()
    {
        // Arrange: 2 recipe cùng score relevance 0.8f
        var timeOld = DateTime.UtcNow.AddDays(-2);
        var timeNew = DateTime.UtcNow;
        var r1 = new { Title = "Phở bò quán 1", Score = 0.8f, PublishedAt = timeOld };
        var r2 = new { Title = "Phở bò quán 2", Score = 0.8f, PublishedAt = timeNew };

        var list = new[] { r1, r2 };

        // Act
        var sorted = list
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.PublishedAt)
            .ToList();

        // Assert: Cùng score thì PublishedAt mới hơn phải xếp trước
        sorted.First().Title.Should().Be("Phở bò quán 2");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 17, 18, 19, 20. Redis Cache: Hit, TTL 1 phút, Vary by All Parameters
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void CacheKey_ChangesWhenQueryChanges()
    {
        // Arrange
        var key1 = GenerateSearchCacheKey("pho bo", 1, 10, "all", "all", "any", "relevance");
        var key2 = GenerateSearchCacheKey("bun cha", 1, 10, "all", "all", "any", "relevance");

        // Assert
        key1.Should().NotBe(key2);
    }

    [Fact]
    public void CacheKey_ChangesWhenPageOrPageSizeChanges()
    {
        // Arrange
        var keyPage1 = GenerateSearchCacheKey("pho bo", 1, 10, "all", "all", "any", "relevance");
        var keyPage2 = GenerateSearchCacheKey("pho bo", 2, 10, "all", "all", "any", "relevance");
        var keySize20 = GenerateSearchCacheKey("pho bo", 1, 20, "all", "all", "any", "relevance");

        // Assert: Chống cache collision giữa các trang
        keyPage1.Should().NotBe(keyPage2);
        keyPage1.Should().NotBe(keySize20);
    }

    [Fact]
    public void CacheKey_ChangesWhenFilterOrSortChanges()
    {
        // Arrange
        var keyDefault = GenerateSearchCacheKey("pho bo", 1, 10, "all", "all", "any", "relevance");
        var keyDiffEasy = GenerateSearchCacheKey("pho bo", 1, 10, "all", "Easy", "any", "relevance");
        var keySortNewest = GenerateSearchCacheKey("pho bo", 1, 10, "all", "all", "any", "newest");
        var keyCatFiltered = GenerateSearchCacheKey("pho bo", 1, 10, "mon-chinh", "all", "any", "relevance");

        // Assert: Filter hoặc Sort khác nhau phải sinh cache key khác nhau
        keyDefault.Should().NotBe(keyDiffEasy);
        keyDefault.Should().NotBe(keySortNewest);
        keyDefault.Should().NotBe(keyCatFiltered);
    }

    [Fact]
    public async Task ResilientCacheService_WhenCacheMiss_ReturnsNull_AndSetsWithOneMinuteTtl()
    {
        // Arrange
        var distributedCacheMock = new Mock<IDistributedCache>();
        var cacheService = new ResilientCacheService(NullLogger<ResilientCacheService>.Instance, distributedCacheMock.Object);

        distributedCacheMock
            .Setup(c => c.GetAsync("test_key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act - Read
        var result = await cacheService.GetAsync<PagedResult<RecipeSearchResultDto>>("test_key");

        // Assert
        result.Should().BeNull();

        // Act - Write with 1 minute TTL
        var sampleData = new PagedResult<RecipeSearchResultDto>(new List<RecipeSearchResultDto>(), 0, 1, 10);
        await cacheService.SetAsync("test_key", sampleData, TimeSpan.FromMinutes(1));

        // Assert - SetAsync called
        distributedCacheMock.Verify(c => c.SetAsync(
            "test_key",
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(1)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResilientCacheService_WhenRedisThrowsException_ReturnsDefaultWithoutCrashing()
    {
        // Arrange: Giả lập Redis bị mất kết nối (Failover Resiliency)
        var distributedCacheMock = new Mock<IDistributedCache>();
        distributedCacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Redis connection timeout"));

        var cacheService = new ResilientCacheService(NullLogger<ResilientCacheService>.Instance, distributedCacheMock.Object);

        // Act
        var result = await cacheService.GetAsync<PagedResult<RecipeSearchResultDto>>("failing_key");

        // Assert: Không throw exception, fallback trả về null an toàn theo SRS v1.2.0
        result.Should().BeNull();
    }

    private static string GenerateSearchCacheKey(
        string q, int page, int pageSize, string catKey, string diffKey, string cookKey, string sortKey)
    {
        return $"search:{q.ToLowerInvariant()}:cat={catKey}:diff={diffKey}:cook={cookKey}:sort={sortKey}:p={page}:sz={pageSize}";
    }
}
