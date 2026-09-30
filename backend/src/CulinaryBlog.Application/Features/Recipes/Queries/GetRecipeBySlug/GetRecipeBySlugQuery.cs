using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Dtos;
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
            .Select(r => new RecipeDetailDto
            {
                Id = r.Id,
                Title = r.Title,
                Slug = r.Slug,
                Description = r.Description,
                Content = r.Content,
                PrepTimeMinutes = r.PrepTimeMinutes,
                CookTimeMinutes = r.CookTimeMinutes,
                Servings = r.Servings,
                Difficulty = r.Difficulty,
                Status = r.Status,
                PublishedAt = r.PublishedAt,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                Author = new AuthorSummaryDto
                {
                    Id = r.Author.Id,
                    DisplayName = r.Author.DisplayName,
                    AvatarUrl = r.Author.AvatarUrl
                },
                Category = new CategorySummaryDto
                {
                    Id = r.Category.Id,
                    Name = r.Category.Name,
                    Slug = r.Category.Slug
                },
                Nutrition = r.Nutrition != null ? new RecipeNutritionDto
                {
                    Calories = r.Nutrition.Calories,
                    Protein = r.Nutrition.Protein,
                    Carbohydrates = r.Nutrition.Carbohydrates,
                    Fat = r.Nutrition.Fat,
                    Fiber = r.Nutrition.Fiber,
                    Sodium = r.Nutrition.Sodium
                } : null,
                Steps = r.Steps
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.StepNumber)
                    .Select(s => new RecipeStepDto
                    {
                        Id = s.Id,
                        StepNumber = s.StepNumber,
                        Title = s.Title,
                        Description = s.Description,
                        TimerMinutes = s.TimerMinutes,
                        ImageUrl = s.ImageUrl
                    })
                    .ToList(),
                Ingredients = r.Ingredients
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new RecipeIngredientDto
                    {
                        Id = i.Id,
                        Name = i.Name,
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        Notes = i.Notes,
                        SortOrder = i.SortOrder
                    })
                    .ToList(),
                Images = r.Images
                    .Where(img => !img.IsDeleted)
                    .OrderBy(img => img.OrderIndex)
                    .Select(img => new RecipeImageDto
                    {
                        Id = img.Id,
                        OriginalUrl = img.OriginalUrl,
                        MediumUrl = img.MediumUrl,
                        ThumbnailUrl = img.ThumbnailUrl,
                        AltText = img.AltText,
                        IsPrimary = img.IsPrimary,
                        OrderIndex = img.OrderIndex
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (recipeDto == null)
        {
            throw new NotFoundException($"Recipe with slug '{request.Slug}' was not found.");
        }

        return recipeDto;
    }
}