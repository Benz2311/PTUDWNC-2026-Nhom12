using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;

public record GetRecipeBySlugQuery(
    string Slug,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<RecipeDetailDto>;

public class GetRecipeBySlugHandler : IRequestHandler<GetRecipeBySlugQuery, RecipeDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetRecipeBySlugHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RecipeDetailDto> Handle(GetRecipeBySlugQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new NotFoundException("Recipe not found.");
        }

        var normalizedSlug = request.Slug.Trim();

        // 1. Kiểm tra tồn tại và quyền truy cập trước khi lấy toàn bộ payload (theo FR-RCP-002)
        var recipeHeader = await _context.Recipes
            .AsNoTracking()
            .Where(r => r.Slug == normalizedSlug && !r.IsDeleted)
            .Select(r => new
            {
                r.Id,
                r.AuthorId,
                r.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (recipeHeader == null)
        {
            throw new NotFoundException($"Recipe with slug '{request.Slug}' was not found.");
        }

        // FR-RCP-002: Recipe Draft/Archived chỉ được xem bởi tác giả sở hữu hoặc Admin
        if (recipeHeader.Status == RecipeStatus.Draft || recipeHeader.Status == RecipeStatus.Archived)
        {
            var isAuthor = request.CurrentUserId.HasValue && request.CurrentUserId.Value == recipeHeader.AuthorId;
            if (!request.IsAdmin && !isAuthor)
            {
                throw new ForbiddenException("You do not have permission to view this unpublished recipe.");
            }
        }

        // 2. Query chi tiết với AsNoTracking và Projection tối ưu, triệt tiêu hoàn toàn N+1
        var recipeDto = await _context.Recipes
            .AsNoTracking()
            .Where(r => r.Id == recipeHeader.Id)
            .Select(r => new RecipeDetailDto(
                r.Id,
                r.Title,
                r.Slug,
                r.Description,
                r.Content,
                r.PrepTimeMinutes,
                r.CookTimeMinutes,
                r.Servings,
                r.Difficulty.ToString(),
                r.PublishedAt,
                new RecipeCategoryDto(
                    r.Category.Id,
                    r.Category.Name,
                    r.Category.Slug,
                    r.Category.Description),
                r.Ingredients
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new RecipeIngredientDto(
                        i.Id,
                        i.Name,
                        i.Quantity,
                        i.Unit,
                        i.Notes,
                        i.SortOrder))
                    .ToList(),
                r.Steps
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.StepNumber)
                    .Select(s => new RecipeStepDto(
                        s.Id,
                        s.StepNumber,
                        s.Title ?? string.Empty,
                        s.Description))
                    .ToList(),
                r.Images
                    .Where(img => !img.IsDeleted)
                    .OrderBy(img => img.OrderIndex)
                    .Select(img => new RecipeImageDto(
                        img.Id,
                        img.OriginalUrl,
                        img.AltText,
                        img.IsPrimary,
                        img.OrderIndex))
                    .ToList(),
                new RecipeNutritionDto(
                    r.Nutrition.Calories,
                    r.Nutrition.Protein,
                    r.Nutrition.Carbohydrates,
                    r.Nutrition.Fat,
                    r.Nutrition.Fiber,
                    r.Nutrition.Sodium),
                r.Author != null
                    ? new RecipeAuthorDto(
                        r.Author.Id,
                        r.Author.DisplayName ?? r.Author.UserName ?? "",
                        r.Author.AvatarUrl)
                    : null,
                r.Status))
            .FirstOrDefaultAsync(cancellationToken);

        if (recipeDto == null)
        {
            throw new NotFoundException($"Recipe with slug '{request.Slug}' was not found.");
        }

        return recipeDto;
    }
}