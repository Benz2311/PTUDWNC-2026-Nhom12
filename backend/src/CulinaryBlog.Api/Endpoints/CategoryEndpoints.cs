using System.Security.Claims;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Commands.CreateCategory;
using CulinaryBlog.Application.Features.Categories.Commands.UpdateCategory;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategoryStatistics;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class CategoryEndpoints
{
    private const string CategoryListCacheKey = "categories:all";

    public static void MapCategoryEndpoints(
        this WebApplication app)
    {
        var group =
            app.MapGroup("/api/v1/categories")
                .WithTags("Categories");

        // GET /api/v1/categories & /api/categories
        group.MapGet(
            "/",
            GetCategories)
            .WithName("GetCategories")
            .WithSummary("Lấy danh sách tất cả các danh mục");

        app.MapGet(
            "/api/categories",
            GetCategories)
            .ExcludeFromDescription();

        group.MapPost(
            "/",
            CreateCategory)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("CreateCategory")
            .WithSummary("Tạo danh mục mới")
            .Produces<CategoryDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut(
            "/{id:guid}",
            UpdateCategory)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("UpdateCategory")
            .WithSummary("Cập nhật danh mục")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // GET /api/v1/categories/statistics
        group.MapGet(
            "/statistics",
            async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result =
                    await sender.Send(
                        new GetCategoryStatisticsQuery(),
                        cancellationToken);

                return Results.Ok(result);
            })
            .WithName("GetCategoryStatistics")
            .WithSummary("Lấy thống kê danh mục và công thức món ăn");

        // GET /api/v1/categories/{slug} (SRS FR-CAT-002 & Table 8.2)
        group.MapGet(
            "/{slug}",
            GetRecipesByCategory)
            .WithName("GetCategoryBySlug")
            .WithSummary("Lấy chi tiết danh mục và danh sách công thức thuộc danh mục");

        app.MapGet(
            "/api/categories/{slug}",
            GetRecipesByCategory)
            .ExcludeFromDescription();

        // GET /api/v1/categories/{slug}/recipes (Backward compatibility)
        group.MapGet(
            "/{slug}/recipes",
            GetRecipesByCategory)
            .WithName("GetCategoryRecipes")
            .WithSummary("Lấy danh sách công thức thuộc danh mục theo slug");
    }

    private static async Task<IResult> GetCategories(
        ISender sender,
        ICacheService cache,
        CancellationToken cancellationToken)
    {
        var cachedCategories = await cache.GetAsync<IReadOnlyList<CategoryDto>>(
            CategoryListCacheKey,
            cancellationToken);
        if (cachedCategories is not null)
        {
            return Results.Ok(cachedCategories);
        }

        var categories = await sender.Send(
            new GetCategoriesQuery(),
            cancellationToken);
        await cache.SetAsync(
            CategoryListCacheKey,
            categories,
            TimeSpan.FromMinutes(30),
            cancellationToken);

        return Results.Ok(categories);
    }

    private static async Task<IResult> GetRecipesByCategory(
        string slug,
        int? page,
        int? pageSize,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Guid? authorId = null;
        if (user.IsInRole("Author"))
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                authorId = parsedUserId;
            }
        }

        var result = await sender.Send(
            new GetRecipesByCategoryQuery(
                slug,
                page ?? 1,
                pageSize ?? 12,
                authorId),
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> CreateCategory(
        CreateCategoryCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await sender.Send(command, cancellationToken);
            if (category is null)
            {
                return Results.Problem(
                    title: "Category name already exists",
                    statusCode: StatusCodes.Status409Conflict);
            }

            return Results.Created($"/api/v1/categories/{category.Slug}", category);
        }
        catch (FluentValidation.ValidationException exception)
        {
            var errors = exception.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }
    }

    private static async Task<IResult> UpdateCategory(
        Guid id,
        UpdateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new UpdateCategoryCommand(id, request.Name, request.Description),
                cancellationToken);

            return result switch
            {
                UpdateCategoryResult.Updated => Results.NoContent(),
                UpdateCategoryResult.NotFound => Results.Problem(
                    title: "Category not found",
                    statusCode: StatusCodes.Status404NotFound),
                UpdateCategoryResult.NameAlreadyExists => Results.Problem(
                    title: "Category name already exists",
                    statusCode: StatusCodes.Status409Conflict),
                _ => throw new InvalidOperationException(
                    $"Unsupported category update result: {result}."),
            };
        }
        catch (FluentValidation.ValidationException exception)
        {
            var errors = exception.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }
    }
}
