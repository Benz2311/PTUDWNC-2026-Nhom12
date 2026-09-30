using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Application.Features.Search.Dtos;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace CulinaryBlog.Application.Features.Search.Queries.SearchRecipes;

public record SearchRecipesQuery(
    string? Query,
    int Page = 1,
    int PageSize = 10
) : IRequest<PagedResult<RecipeSearchResultDto>>;

public class SearchRecipesHandler : IRequestHandler<SearchRecipesQuery, PagedResult<RecipeSearchResultDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchRecipesHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<RecipeSearchResultDto>> Handle(SearchRecipesQuery request, CancellationToken cancellationToken)
    {
        var rawQuery = request.Query?.Trim() ?? string.Empty;

        // SRS FR-SRCH-001: Query dài 2-100 ký tự; không tìm thấy hoặc query không hợp lệ trả về mảng rỗng
        if (rawQuery.Length < 2 || rawQuery.Length > 100)
        {
            return new PagedResult<RecipeSearchResultDto>(new List<RecipeSearchResultDto>(), 0, request.Page, request.PageSize);
        }

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 10;

        // Chuẩn hóa từ khóa: không dấu, lowercase (khớp với unaccent tsvector trong database)
        var normalizedKeyword = VietnameseTextNormalizer.Normalize(rawQuery);
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            normalizedKeyword = rawQuery.ToLowerInvariant();
        }

        // Base query: chỉ tìm các Recipe ở trạng thái Published và chưa bị xóa mềm (SRS FR-SRCH-001)
        var baseQuery = _context.Recipes
            .AsNoTracking()
            .Where(r => r.Status == RecipeStatus.Published && !r.IsDeleted);

        // ==========================================
        // GIAI ĐOẠN 1: POSTGRESQL FULL-TEXT SEARCH (FTS)
        // Sử dụng Shadow Property SearchVectorFts, cấu hình 'simple' và hàm unaccent
        // ==========================================
        var ftsQuery = baseQuery
            .Where(r => EF.Property<NpgsqlTsVector>(r, "SearchVectorFts")
                .Matches(EF.Functions.PlainToTsQuery("simple", normalizedKeyword)));

        var ftsTotalCount = await ftsQuery.CountAsync(cancellationToken);

        if (ftsTotalCount > 0)
        {
            // Sắp xếp theo relevance score (ts_rank) giảm dần, thứ cấp theo PublishedAt giảm dần
            var items = await ftsQuery
                .OrderByDescending(r => EF.Property<NpgsqlTsVector>(r, "SearchVectorFts")
                    .Rank(EF.Functions.PlainToTsQuery("simple", normalizedKeyword)))
                .ThenByDescending(r => r.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RecipeSearchResultDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Slug = r.Slug,
                    Description = r.Description,
                    PrepTimeMinutes = r.PrepTimeMinutes,
                    CookTimeMinutes = r.CookTimeMinutes,
                    Servings = r.Servings,
                    Difficulty = r.Difficulty,
                    PublishedAt = r.PublishedAt,
                    PrimaryImageUrl = r.Images
                        .Where(img => img.IsPrimary && !img.IsDeleted)
                        .Select(img => img.OriginalUrl)
                        .FirstOrDefault(),
                    AuthorName = r.Author.DisplayName,
                    CategoryName = r.Category.Name,
                    CategorySlug = r.Category.Slug,
                    MatchType = "FullTextSearch"
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<RecipeSearchResultDto>(items, ftsTotalCount, page, pageSize);
        }

        // ==========================================
        // GIAI ĐOẠN 2: FUZZY FALLBACK (pg_trgm)
        // Khi FTS không có kết quả (ví dụ người dùng gõ sai 'phoo bo')
        // Ngưỡng tương đồng similarity > 0.3 trên lower(unaccent(Title))
        // ==========================================
        var fuzzyQuery = baseQuery
            .Where(r => EF.Functions.TrigramsSimilarity(
                EF.Functions.Unaccent(r.Title).ToLower(),
                normalizedKeyword) > 0.3f);

        var fuzzyTotalCount = await fuzzyQuery.CountAsync(cancellationToken);

        if (fuzzyTotalCount > 0)
        {
            // Sắp xếp theo similarity score giảm dần, thứ cấp theo PublishedAt giảm dần
            var items = await fuzzyQuery
                .OrderByDescending(r => EF.Functions.TrigramsSimilarity(
                    EF.Functions.Unaccent(r.Title).ToLower(),
                    normalizedKeyword))
                .ThenByDescending(r => r.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RecipeSearchResultDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Slug = r.Slug,
                    Description = r.Description,
                    PrepTimeMinutes = r.PrepTimeMinutes,
                    CookTimeMinutes = r.CookTimeMinutes,
                    Servings = r.Servings,
                    Difficulty = r.Difficulty,
                    PublishedAt = r.PublishedAt,
                    PrimaryImageUrl = r.Images
                        .Where(img => img.IsPrimary && !img.IsDeleted)
                        .Select(img => img.OriginalUrl)
                        .FirstOrDefault(),
                    AuthorName = r.Author.DisplayName,
                    CategoryName = r.Category.Name,
                    CategorySlug = r.Category.Slug,
                    MatchType = "FuzzyTrigram"
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<RecipeSearchResultDto>(items, fuzzyTotalCount, page, pageSize);
        }

        // Không tìm thấy dữ liệu: trả về HTTP 200 với mảng rỗng (theo SRS FR-SRCH-004)
        return new PagedResult<RecipeSearchResultDto>(new List<RecipeSearchResultDto>(), 0, page, pageSize);
    }
}
