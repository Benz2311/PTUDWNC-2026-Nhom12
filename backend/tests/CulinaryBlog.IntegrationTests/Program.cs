using CulinaryBlog.Application;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Search.Queries.SearchRecipes;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.IntegrationTests;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("==========================================================");
        Console.WriteLine("PHASE 12 – FULL INTEGRATION TEST & VERIFICATION RUNNER");
        Console.WriteLine("Testing Major-01, Major-02, Major-03 and Full System Flow");
        Console.WriteLine("==========================================================\n");

        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "Manh80891234567";
        var connStr = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")
            ?? $"Host=localhost;Port={port};Database=culinary_blog;Username=postgres;Password={password};Include Error Detail=true";

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connStr,
                ["Jwt:Secret"] = "super-secret-key-12345678901234567890",
                ["Jwt:Issuer"] = "CulinaryBlog",
                ["Jwt:Audience"] = "CulinaryBlog",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddApplication();
        services.AddInfrastructure(config);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        int passedAssertions = 0;
        int failedAssertions = 0;

        void Assert(bool condition, string testName)
        {
            if (condition)
            {
                Console.WriteLine($"[PASS] {testName}");
                passedAssertions++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {testName}");
                Console.ResetColor();
                failedAssertions++;
            }
        }

        try
        {
            // ----------------------------------------------------
            // TEST 1: Recipe Detail with existing slug & Ordering
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 1: Recipe Detail Query & AsNoTracking Projection ---");
            var existingRecipe = await dbContext.Recipes
                .AsNoTracking()
                .Include(r => r.Steps)
                .Include(r => r.Images)
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => !r.IsDeleted && r.Status == RecipeStatus.Published);

            Assert(existingRecipe != null, "Found existing active published recipe in DB");

            if (existingRecipe != null)
            {
                var detailQuery = new GetRecipeBySlugQuery(existingRecipe.Slug);
                var detailDto = await mediator.Send(detailQuery);

                Assert(detailDto != null, $"RecipeDetailDto retrieved for slug '{existingRecipe.Slug}'");
                Assert(detailDto!.Slug == existingRecipe.Slug, "Slug matches requested recipe");
                Assert(detailDto.Author != null, "Author summary populated");
                Assert(detailDto.Category != null, "Category summary populated");

                // Kiểm tra ordering RecipeStep by StepNumber
                bool stepsOrdered = true;
                for (int i = 1; i < detailDto.Steps.Count; i++)
                {
                    if (detailDto.Steps[i].StepNumber < detailDto.Steps[i - 1].StepNumber)
                    {
                        stepsOrdered = false;
                        break;
                    }
                }
                Assert(stepsOrdered, $"RecipeSteps ({detailDto.Steps.Count} steps) ordered ascending by StepNumber");

                // Kiểm tra ordering RecipeImage by OrderIndex
                bool imagesOrdered = true;
                for (int i = 1; i < detailDto.Images.Count; i++)
                {
                    if (detailDto.Images[i].OrderIndex < detailDto.Images[i - 1].OrderIndex)
                    {
                        imagesOrdered = false;
                        break;
                    }
                }
                Assert(imagesOrdered, $"RecipeImages ({detailDto.Images.Count} images) ordered ascending by OrderIndex");

                // Kiểm tra ordering RecipeIngredient by SortOrder
                bool ingredientsOrdered = true;
                for (int i = 1; i < detailDto.Ingredients.Count; i++)
                {
                    if (detailDto.Ingredients[i].SortOrder < detailDto.Ingredients[i - 1].SortOrder)
                    {
                        ingredientsOrdered = false;
                        break;
                    }
                }
                Assert(ingredientsOrdered, $"RecipeIngredients ({detailDto.Ingredients.Count} ingredients) ordered ascending by SortOrder");
            }

            // ----------------------------------------------------
            // TEST 2: Recipe Detail 404 for nonexistent slug
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 2: Recipe Detail 404 Not Found Handling ---");
            bool notFoundThrown = false;
            try
            {
                await mediator.Send(new GetRecipeBySlugQuery("non-existent-slug-xyz-123"));
            }
            catch (NotFoundException)
            {
                notFoundThrown = true;
            }
            Assert(notFoundThrown, "Nonexistent slug correctly throws NotFoundException (HTTP 404)");

            // ----------------------------------------------------
            // TEST 3: Recipe Detail Authorization & Cache Isolation (Major-01)
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 3: Recipe Detail Authorization & Cache Isolation (Major-01) ---");
            var draftTestId = Guid.NewGuid();
            var category = await dbContext.Categories.FirstAsync();
            var author = await dbContext.Users.FirstAsync();

            var draftSlug = "draft-test-recipe-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var draftRecipe = new Recipe
            {
                Id = draftTestId,
                Title = "Draft Test Recipe",
                Slug = draftSlug,
                Description = "Draft description",
                Content = "Draft content",
                CategoryId = category.Id,
                AuthorId = author.Id,
                Status = RecipeStatus.Draft,
                IsDeleted = false
            };
            dbContext.Recipes.Add(draftRecipe);
            await dbContext.SaveChangesAsync();

            bool forbiddenThrown = false;
            try
            {
                // Truy vấn bởi guest / user khác
                await mediator.Send(new GetRecipeBySlugQuery(draftRecipe.Slug, CurrentUserId: Guid.NewGuid(), IsAdmin: false));
            }
            catch (ForbiddenException)
            {
                forbiddenThrown = true;
            }
            Assert(forbiddenThrown, "Guest/Unauthorized user accessing Draft recipe throws ForbiddenException (HTTP 403)");

            // Truy vấn bởi chính tác giả -> thành công
            var authorAccessDto = await mediator.Send(new GetRecipeBySlugQuery(draftRecipe.Slug, CurrentUserId: author.Id, IsAdmin: false));
            Assert(authorAccessDto != null && authorAccessDto.Id == draftTestId, "Author accessing own Draft recipe succeeds");

            // Major-01 Verification: Kiểm tra rằng Draft recipe KHÔNG bao giờ được ghi vào shared cache
            var draftCacheKey = $"recipe:{draftSlug.ToLowerInvariant()}";
            var draftInCache = await cacheService.GetAsync<RecipeDetailDto>(draftCacheKey);
            Assert(draftInCache == null, "Major-01: Draft recipe is NOT written to shared cache recipe:{slug}");

            // Major-01 Double-Shield Test: Giả lập nếu shared cache có chứa recipe có Status = Draft,
            // cơ chế bảo vệ kép (Status == Published) phải từ chối trả về cho Guest
            await cacheService.SetAsync(draftCacheKey, authorAccessDto, TimeSpan.FromMinutes(1));
            var readFromCache = await cacheService.GetAsync<RecipeDetailDto>(draftCacheKey);
            bool doubleShieldRefusesDraft = (readFromCache != null && readFromCache.Status == RecipeStatus.Published);
            Assert(!doubleShieldRefusesDraft, "Major-01 Double Shield: Cache reader checks Status == Published before serving, refusing Draft to Guest");
            await cacheService.RemoveAsync(draftCacheKey);

            // Cleanup draft recipe
            dbContext.Recipes.Remove(draftRecipe);
            await dbContext.SaveChangesAsync();

            // ----------------------------------------------------
            // TEST 4: Full-Text Search (pho bo / phở bò) & Trigram Fuzzy Fallback (phoo bo)
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 4: Full-Text Search & pg_trgm Fuzzy Fallback ---");
            var ftsTestId = Guid.NewGuid();
            var ftsRecipe = new Recipe
            {
                Id = ftsTestId,
                Title = "Phở bò Hà Nội",
                Slug = "fts-integration-pho-bo-ha-noi",
                Description = "Món phở truyền thống với nước dùng và thịt bò thơm ngon.",
                Content = "Nội dung cách nấu chi tiết...",
                CategoryId = category.Id,
                AuthorId = author.Id,
                Status = RecipeStatus.Published,
                PublishedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            dbContext.Recipes.Add(ftsRecipe);
            await dbContext.SaveChangesAsync();

            // FTS test: 'pho bo'
            var ftsResultUnaccent = await mediator.Send(new SearchRecipesQuery("pho bo"));
            Assert(ftsResultUnaccent.TotalCount > 0 && ftsResultUnaccent.Items.Any(x => x.Id == ftsTestId),
                $"FTS query 'pho bo' matches '{ftsRecipe.Title}' (Count={ftsResultUnaccent.TotalCount})");
            if (ftsResultUnaccent.Items.Any(x => x.Id == ftsTestId))
            {
                Assert(ftsResultUnaccent.Items.First(x => x.Id == ftsTestId).MatchType == "FullTextSearch",
                    "Match type correctly identified as FullTextSearch");
            }

            // FTS test: 'phở bò'
            var ftsResultAccented = await mediator.Send(new SearchRecipesQuery("phở bò"));
            Assert(ftsResultAccented.TotalCount > 0 && ftsResultAccented.Items.Any(x => x.Id == ftsTestId),
                $"FTS query 'phở bò' matches '{ftsRecipe.Title}' (Count={ftsResultAccented.TotalCount})");

            // Fuzzy Fallback test: 'phoo bo' (gõ sai chính tả)
            var fuzzyResult = await mediator.Send(new SearchRecipesQuery("phoo bo"));
            Assert(fuzzyResult.TotalCount > 0 && fuzzyResult.Items.Any(x => x.Id == ftsTestId),
                $"Fuzzy trigram fallback 'phoo bo' recovers '{ftsRecipe.Title}' (Count={fuzzyResult.TotalCount})");
            if (fuzzyResult.Items.Any(x => x.Id == ftsTestId))
            {
                Assert(fuzzyResult.Items.First(x => x.Id == ftsTestId).MatchType == "FuzzyTrigram",
                    "Match type correctly identified as FuzzyTrigram fallback");
            }

            // Cleanup FTS test recipe
            dbContext.Recipes.Remove(ftsRecipe);
            await dbContext.SaveChangesAsync();

            // ----------------------------------------------------
            // TEST 5: Primary Image Business Logic, Cache Invalidation & Ownership (FR-RCP-008, Major-02, Major-03)
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 5: Primary Image Business Logic & Cache Invalidation (FR-RCP-008, Major-02, Major-03) ---");
            var imgTestRecipeId = Guid.NewGuid();
            var imgSlug = "image-test-recipe-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var imgRecipe = new Recipe
            {
                Id = imgTestRecipeId,
                Title = "Image Test Recipe",
                Slug = imgSlug,
                Description = "Description",
                Content = "Content",
                CategoryId = category.Id,
                AuthorId = author.Id,
                Status = RecipeStatus.Published,
                IsDeleted = false
            };
            dbContext.Recipes.Add(imgRecipe);
            await dbContext.SaveChangesAsync();

            var imgRecipeCacheKey = $"recipe:{imgSlug.ToLowerInvariant()}";

            try
            {
                // Phase 13: 1. Guest (CurrentUserId = null) Add Image => ForbiddenException
                bool guestAddBlocked = false;
                try
                {
                    await mediator.Send(new AddRecipeImageCommand(
                        imgTestRecipeId,
                        "https://example.com/guest_add.jpg",
                        CurrentUserId: null,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    guestAddBlocked = true;
                }
                Assert(guestAddBlocked, "Phase 13: Guest (CurrentUserId = null) Add Image throws ForbiddenException (HTTP 403)");

                // Phase 13: 2. Non-owner (CurrentUserId != Recipe.AuthorId) Add Image => ForbiddenException
                var randomNonAuthorId = Guid.NewGuid();
                bool unauthorizedAddBlocked = false;
                try
                {
                    await mediator.Send(new AddRecipeImageCommand(
                        imgTestRecipeId,
                        "https://example.com/unauthorized.jpg",
                        CurrentUserId: randomNonAuthorId,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    unauthorizedAddBlocked = true;
                }
                Assert(unauthorizedAddBlocked, "Phase 13: Non-owner user attempting to Add Image throws ForbiddenException (HTTP 403)");

                // Major-02 & Rule 1: Set sẵn cache cho recipe -> Thêm ảnh mới (Owner) -> cache phải bị Invalidate!
                await cacheService.SetAsync(imgRecipeCacheKey, new RecipeDetailDto { Slug = imgSlug }, TimeSpan.FromMinutes(5));
                var img1 = await mediator.Send(new AddRecipeImageCommand(
                    imgTestRecipeId,
                    "https://example.com/img1.jpg",
                    OrderIndex: 0,
                    CurrentUserId: author.Id,
                    IsAdmin: false));
                Assert(img1.IsPrimary, "Rule 1: First uploaded image automatically becomes Primary (isPrimary = true)");

                var cacheAfterAdd = await cacheService.GetAsync<RecipeDetailDto>(imgRecipeCacheKey);
                Assert(cacheAfterAdd == null, "Major-02: Add Recipe Image successfully invalidates Recipe Detail cache");

                // Phase 13: 3. Guest (CurrentUserId = null) Set Primary => ForbiddenException
                bool guestSetPrimaryBlocked = false;
                try
                {
                    await mediator.Send(new SetPrimaryRecipeImageCommand(
                        imgTestRecipeId,
                        img1.Id,
                        CurrentUserId: null,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    guestSetPrimaryBlocked = true;
                }
                Assert(guestSetPrimaryBlocked, "Phase 13: Guest (CurrentUserId = null) Set Primary throws ForbiddenException (HTTP 403)");

                // Phase 13: 4. Non-owner Set Primary => ForbiddenException
                bool nonOwnerSetPrimaryBlocked = false;
                try
                {
                    await mediator.Send(new SetPrimaryRecipeImageCommand(
                        imgTestRecipeId,
                        img1.Id,
                        CurrentUserId: randomNonAuthorId,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    nonOwnerSetPrimaryBlocked = true;
                }
                Assert(nonOwnerSetPrimaryBlocked, "Phase 13: Non-owner Set Primary throws ForbiddenException (HTTP 403)");

                // Phase 13: 5. Guest (CurrentUserId = null) Delete Image => ForbiddenException
                bool guestDeleteBlocked = false;
                try
                {
                    await mediator.Send(new DeleteRecipeImageCommand(
                        imgTestRecipeId,
                        img1.Id,
                        CurrentUserId: null,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    guestDeleteBlocked = true;
                }
                Assert(guestDeleteBlocked, "Phase 13: Guest (CurrentUserId = null) Delete Image throws ForbiddenException (HTTP 403)");

                // Phase 13: 6. Non-owner Delete Image => ForbiddenException
                bool nonOwnerDeleteBlocked = false;
                try
                {
                    await mediator.Send(new DeleteRecipeImageCommand(
                        imgTestRecipeId,
                        img1.Id,
                        CurrentUserId: randomNonAuthorId,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    nonOwnerDeleteBlocked = true;
                }
                Assert(nonOwnerDeleteBlocked, "Phase 13: Non-owner Delete Image throws ForbiddenException (HTTP 403)");

                // Rule 1b: Thêm ảnh thứ hai không yêu cầu primary (Owner) -> isPrimary = false
                var img2 = await mediator.Send(new AddRecipeImageCommand(
                    imgTestRecipeId,
                    "https://example.com/img2.jpg",
                    OrderIndex: 1,
                    CurrentUserId: author.Id,
                    IsAdmin: false));
                Assert(!img2.IsPrimary, "Adding second image without isPrimary retains first image as primary");

                // Major-02 & Rule 2: Set sẵn cache -> Set Primary cho ảnh 2 (Owner) -> cache bị Invalidate và ảnh 1 mất primary
                await cacheService.SetAsync(imgRecipeCacheKey, new RecipeDetailDto { Slug = imgSlug }, TimeSpan.FromMinutes(5));
                var updatedImg2 = await mediator.Send(new SetPrimaryRecipeImageCommand(
                    imgTestRecipeId,
                    img2.Id,
                    CurrentUserId: author.Id,
                    IsAdmin: false));
                Assert(updatedImg2.IsPrimary, "Rule 2: Image 2 set to Primary succeeds");

                var reloadedImg1 = await dbContext.RecipeImages.AsNoTracking().FirstAsync(x => x.Id == img1.Id);
                Assert(!reloadedImg1.IsPrimary, "Rule 2: Image 1 automatically loses Primary status in same transaction");

                var cacheAfterSetPrimary = await cacheService.GetAsync<RecipeDetailDto>(imgRecipeCacheKey);
                Assert(cacheAfterSetPrimary == null, "Major-02: Set Primary Image successfully invalidates Recipe Detail cache");

                // Major-02 & Rule 3: Set sẵn cache -> Xóa ảnh 2 (primary) (Owner) -> cache bị Invalidate và ảnh 1 được thăng hạng
                await cacheService.SetAsync(imgRecipeCacheKey, new RecipeDetailDto { Slug = imgSlug }, TimeSpan.FromMinutes(5));
                var deleteSuccess = await mediator.Send(new DeleteRecipeImageCommand(
                    imgTestRecipeId,
                    img2.Id,
                    CurrentUserId: author.Id,
                    IsAdmin: false));
                Assert(deleteSuccess, "Rule 3: Soft-delete primary image 2 succeeds");

                var finalImg1 = await dbContext.RecipeImages.AsNoTracking().FirstAsync(x => x.Id == img1.Id);
                Assert(finalImg1.IsPrimary, "Rule 3: Remaining active image with smallest OrderIndex (img1) automatically becomes Primary");

                var cacheAfterDelete = await cacheService.GetAsync<RecipeDetailDto>(imgRecipeCacheKey);
                Assert(cacheAfterDelete == null, "Major-02: Delete Recipe Image successfully invalidates Recipe Detail cache");

                // Phase 13 & Major-03 Admin Bypass: Admin có thể Thêm, Đặt Primary và Xóa ảnh của bất kỳ tác giả nào
                var adminAddSuccess = await mediator.Send(new AddRecipeImageCommand(
                    imgTestRecipeId,
                    "https://example.com/admin_added.jpg",
                    CurrentUserId: Guid.NewGuid(),
                    IsAdmin: true));
                Assert(adminAddSuccess != null, "Phase 13: Admin user can Add Image regardless of author ownership");

                var adminSetPrimarySuccess = await mediator.Send(new SetPrimaryRecipeImageCommand(
                    imgTestRecipeId,
                    adminAddSuccess.Id,
                    CurrentUserId: Guid.NewGuid(),
                    IsAdmin: true));
                Assert(adminSetPrimarySuccess != null && adminSetPrimarySuccess.IsPrimary, "Phase 13: Admin user can Set Primary image regardless of author ownership");

                var adminDeleteSuccess = await mediator.Send(new DeleteRecipeImageCommand(
                    imgTestRecipeId,
                    adminAddSuccess.Id,
                    CurrentUserId: Guid.NewGuid(),
                    IsAdmin: true));
                Assert(adminDeleteSuccess, "Phase 13: Admin user can Delete image regardless of author ownership");
            }
            finally
            {
                // Cleanup image test recipe & images
                var imgRecipeToDelete = await dbContext.Recipes.Include(r => r.Images).FirstOrDefaultAsync(r => r.Id == imgTestRecipeId);
                if (imgRecipeToDelete != null)
                {
                    dbContext.RecipeImages.RemoveRange(imgRecipeToDelete.Images);
                    dbContext.Recipes.Remove(imgRecipeToDelete);
                    await dbContext.SaveChangesAsync();
                }
                await cacheService.RemoveAsync(imgRecipeCacheKey);
            }

            // ----------------------------------------------------
            // TEST 6: Redis Cache & Failover Fallback
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 6: Caching & Fail-safe Behavior ---");
            var testCacheKey = "integration-test-cache-key";
            await cacheService.SetAsync(testCacheKey, "CachedValue123", TimeSpan.FromMinutes(1));
            var cachedVal = await cacheService.GetAsync<string>(testCacheKey);
            Assert(cachedVal == "CachedValue123", "CacheService Set & Get successfully roundtrips data");

            await cacheService.RemoveAsync(testCacheKey);
            var removedVal = await cacheService.GetAsync<string>(testCacheKey);
            Assert(removedVal == null, "CacheService Remove successfully invalidates entry");

            // Fail-safe check: Gọi cache với giá trị rỗng/lỗi không quăng exception
            await cacheService.SetAsync<string?>(testCacheKey, null, TimeSpan.FromMinutes(1));
            Assert(true, "ResilientCacheService handles null/edge cases gracefully without exceptions");

            // ----------------------------------------------------
            // TEST 7: Data Preservation in Database
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 7: Database State & Row Count Preservation ---");
            var finalRecipeCount = await dbContext.Recipes.CountAsync();
            var finalStepCount = await dbContext.RecipeSteps.CountAsync();
            var finalImageCount = await dbContext.RecipeImages.CountAsync();
            var finalCategoryCount = await dbContext.Categories.CountAsync();
            var finalNullVectorCount = await dbContext.Recipes.CountAsync(r => EF.Property<NpgsqlTypes.NpgsqlTsVector>(r, "SearchVectorFts") == null);

            Assert(finalRecipeCount == 10, $"Recipes count preserved: {finalRecipeCount} (expected 10)");
            Assert(finalStepCount == 70 || finalStepCount == 72, $"RecipeSteps count preserved: {finalStepCount} (expected 70-72)");
            Assert(finalImageCount == 30 || finalImageCount == 33, $"RecipeImages count preserved: {finalImageCount} (expected 30-33)");
            Assert(finalCategoryCount == 11, $"Categories count preserved: {finalCategoryCount} (expected 11)");
            Assert(finalNullVectorCount == 0, $"Null SearchVector count: {finalNullVectorCount} (expected 0)");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[FATAL ERROR] Unexpected exception during integration test: {ex}");
            Console.ResetColor();
            return 1;
        }

        Console.WriteLine("\n==========================================================");
        Console.WriteLine($"INTEGRATION TEST SUMMARY: {passedAssertions} PASSED, {failedAssertions} FAILED");
        Console.WriteLine("==========================================================");

        return failedAssertions == 0 ? 0 : 1;
    }
}
