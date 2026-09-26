using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;
using CulinaryBlog.Domain.Enums;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/recipes")
            .WithTags("Recipes");

        group.MapGet("/", GetRecipes)
            .WithName("GetRecipes")
            .WithSummary("Lấy danh sách công thức đã xuất bản");

        group.MapGet("/category/{categorySlug}", GetRecipesByCategory)
            .WithName("GetRecipesByCategory")
            .WithSummary("Lấy danh sách công thức theo danh mục");

        group.MapGet("/{slug}", GetRecipeBySlug)
            .WithName("GetRecipeBySlug")
            .WithSummary("Lấy chi tiết công thức đã xuất bản");
    }

    private static async Task<IResult> GetRecipes(
        int? page,
        int? pageSize,
        string? search,
        string? categorySlug,
        Guid? categoryId,
        string? difficulty,
        int? maxCookTimeMinutes,
        int? maxTotalTimeMinutes,
        string? sortBy,
        string? sortDirection,
        string? sort,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateListOptions(
            search,
            difficulty,
            maxCookTimeMinutes,
            maxTotalTimeMinutes,
            sortBy,
            sortDirection,
            sort,
            out var options);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await sender.Send(
            new GetRecipesQuery(page ?? 1, pageSize ?? 12, options with
            {
                CategorySlug = string.IsNullOrWhiteSpace(categorySlug)
                    ? null
                    : categorySlug.Trim(),
                CategoryId = categoryId
            }),
            cancellationToken);

        return Results.Ok(result);
    }

    private static IResult? ValidateListOptions(
        string? search,
        string? difficulty,
        int? maxCookTimeMinutes,
        int? maxTotalTimeMinutes,
        string? sortBy,
        string? sortDirection,
        string? sort,
        out RecipeListOptions options)
    {
        options = new RecipeListOptions();

        if (search?.Length > 200)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["search"] = ["Từ khóa tìm kiếm không được vượt quá 200 ký tự."]
            });
        }

        DifficultyLevel? parsedDifficulty = null;
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            var difficultyName = Enum.GetNames<DifficultyLevel>()
                .FirstOrDefault(name => string.Equals(
                    name,
                    difficulty.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (difficultyName is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["difficulty"] = ["Độ khó phải là Easy, Medium, Hard hoặc Expert."]
                });
            }

            parsedDifficulty = Enum.Parse<DifficultyLevel>(difficultyName);
        }

        if (maxCookTimeMinutes is <= 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["maxCookTimeMinutes"] = ["Thời gian nấu tối đa phải lớn hơn 0."]
            });
        }

        if (maxTotalTimeMinutes is <= 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["maxTotalTimeMinutes"] = ["Thời gian tối đa phải lớn hơn 0."]
            });
        }

        // Hỗ trợ tham số sort theo SRS FR-SRCH-003 (ví dụ: sort=-createdAt, sort=title)
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var trimmedSort = sort.Trim();
            if (trimmedSort.StartsWith('-'))
            {
                sortDirection = "desc";
                sortBy = trimmedSort[1..];
            }
            else if (trimmedSort.StartsWith('+'))
            {
                sortDirection = "asc";
                sortBy = trimmedSort[1..];
            }
            else
            {
                sortDirection = "asc";
                sortBy = trimmedSort;
            }
        }

        var sortByValue = string.IsNullOrWhiteSpace(sortBy)
            ? "publishedat"
            : sortBy.Trim().ToLowerInvariant();

        if (sortByValue is "createdat" or "-createdat")
        {
            sortByValue = "publishedat";
        }

        var parsedSortBy = sortByValue switch
        {
            "publishedat" => RecipeSortField.PublishedAt,
            "title" => RecipeSortField.Title,
            "preptime" => RecipeSortField.PrepTime,
            "cooktime" => RecipeSortField.CookTime,
            "totaltime" => RecipeSortField.TotalTime,
            _ => (RecipeSortField?)null
        };

        if (!parsedSortBy.HasValue)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sortBy"] = ["SortBy phải là publishedAt, createdAt, title, prepTime, cookTime hoặc totalTime."]
            });
        }

        var sortDirectionValue = string.IsNullOrWhiteSpace(sortDirection)
            ? "desc"
            : sortDirection.Trim().ToLowerInvariant();
        var parsedSortDirection = sortDirectionValue switch
        {
            "asc" => false,
            "desc" => true,
            _ => (bool?)null
        };

        if (!parsedSortDirection.HasValue)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sortDirection"] = ["SortDirection phải là asc hoặc desc."]
            });
        }

        options = new RecipeListOptions(
            string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Difficulty: parsedDifficulty,
            MaxTotalTimeMinutes: maxTotalTimeMinutes,
            SortBy: parsedSortBy.Value,
            SortDescending: parsedSortDirection.Value,
            MaxCookTimeMinutes: maxCookTimeMinutes);
        return null;
    }

    private static async Task<IResult> GetRecipeBySlug(
        string slug,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetRecipeBySlugQuery(slug),
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> GetRecipesByCategory(
        string categorySlug,
        int? page,
        int? pageSize,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetRecipesByCategoryQuery(categorySlug, page ?? 1, pageSize ?? 12),
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }
}