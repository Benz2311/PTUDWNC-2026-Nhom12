using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using ReadRecipeRepo = CulinaryBlog.Application.Contracts.Persistence.IRecipeRepository;
using WriteRecipeRepo = CulinaryBlog.Application.Interfaces.IRecipeRepository;

namespace CulinaryBlog.Application.Features.Recipes;

public sealed class RecipeService : IRecipeService
{
    private readonly WriteRecipeRepo _recipeWriteRepository;
    private readonly ReadRecipeRepo _recipeReadRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecipeService(
        WriteRecipeRepo recipeWriteRepository,
        ReadRecipeRepo recipeReadRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _recipeWriteRepository = recipeWriteRepository;
        _recipeReadRepository = recipeReadRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecipeDetailDto> CreateAsync(CreateRecipeDto dto, Guid authorId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category", dto.CategoryId);

        var slug = string.IsNullOrWhiteSpace(dto.Slug)
            ? CulinaryBlog.Domain.Common.SlugHelper.Generate(dto.Title)
            : dto.Slug;

        var recipe = new Recipe(
            dto.Title,
            slug,
            dto.Description,
            dto.PrepTimeMinutes,
            dto.CookTimeMinutes,
            dto.Servings,
            Enum.Parse<DifficultyLevel>(dto.Difficulty, true),
            RecipeStatus.Draft,
            dto.CategoryId)
        {
            AuthorId = authorId,
            Content = dto.Content
        };

        // Map ingredients
        if (dto.Ingredients != null)
        {
            foreach (var ingredientDto in dto.Ingredients)
            {
                recipe.Ingredients.Add(new RecipeIngredient
                {
                    RecipeId = recipe.Id,
                    Name = ingredientDto.Name,
                    Quantity = ingredientDto.Quantity,
                    Unit = ingredientDto.Unit,
                    Notes = ingredientDto.Notes,
                    SortOrder = ingredientDto.SortOrder
                });
            }
        }

        // Map steps
        if (dto.Steps != null)
        {
            foreach (var stepDto in dto.Steps)
            {
                recipe.Steps.Add(new RecipeStep
                {
                    RecipeId = recipe.Id,
                    StepNumber = stepDto.StepNumber,
                    Title = stepDto.Title,
                    Description = stepDto.Description,
                    TimerMinutes = stepDto.TimerMinutes,
                    ImageUrl = stepDto.ImageUrl
                });
            }
        }

        // Map nutrition (Owned Entity)
        if (dto.Nutrition != null)
        {
            recipe.Nutrition = new RecipeNutrition
            {
                Calories = dto.Nutrition.Calories,
                Protein = dto.Nutrition.Protein,
                Carbohydrates = dto.Nutrition.Carbohydrates,
                Fat = dto.Nutrition.Fat,
                Fiber = dto.Nutrition.Fiber,
                Sodium = dto.Nutrition.Sodium
            };
        }

        await _recipeWriteRepository.AddAsync(recipe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with all related data for return
        return await GetDetailByIdAsync(recipe.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created recipe.");
    }

    public async Task<RecipeDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetDetailByIdAsync(id, cancellationToken);
    }

    public async Task<RecipeDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await GetDetailBySlugAsync(slug, cancellationToken);
    }

    public async Task<PagedResultDto<RecipeListItemDto>> GetPagedListAsync(RecipeSearchQueryDto query, CancellationToken cancellationToken = default)
    {
        var options = new RecipeListOptions
        {
            Search = query.Search,
            CategoryId = query.CategoryId,
            CategorySlug = query.CategorySlug,
            Difficulty = query.Difficulty,
            MaxTotalTimeMinutes = query.MaxTotalTimeMinutes,
            MaxCookTimeMinutes = query.MaxCookTimeMinutes,
            SortBy = Enum.TryParse<RecipeSortField>(query.SortBy, true, out var sortField) ? sortField : RecipeSortField.PublishedAt,
            SortDescending = query.SortDescending
        };

        return await _recipeReadRepository.GetPublishedAsync(query.Page, query.PageSize, options, cancellationToken);
    }

    public Task<PagedResultDto<RecipeListItemDto>> SearchRecipesAsync(RecipeSearchQueryDto query, CancellationToken cancellationToken = default)
    {
        return _recipeReadRepository.SearchFullTextAsync(query, cancellationToken);
    }

    public async Task<RecipeDetailDto> UpdateAsync(Guid id, UpdateRecipeDto dto, Guid authorId, CancellationToken cancellationToken = default)
    {
        var recipe = await _recipeWriteRepository.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException("Recipe", id);

        if (recipe.AuthorId != authorId)
        {
            throw new UnauthorizedAuthException("You are not authorized to update this recipe.");
        }

        // Optimistic Concurrency: đối chiếu RowVersion từ request với phiên bản đang lưu trong DB
        if (dto.RowVersion != null && !dto.RowVersion.SequenceEqual(recipe.RowVersion))
        {
            throw new ConcurrencyConflictException(nameof(Recipe), id);
        }

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category", dto.CategoryId);

        // Update main properties
        recipe.Title = dto.Title;
        recipe.Description = dto.Description;
        recipe.Content = dto.Content;
        recipe.PrepTimeMinutes = dto.PrepTimeMinutes;
        recipe.CookTimeMinutes = dto.CookTimeMinutes;
        recipe.Servings = dto.Servings;
        recipe.Difficulty = Enum.Parse<DifficultyLevel>(dto.Difficulty, true);
        recipe.CategoryId = dto.CategoryId;
        recipe.UpdatedAt = DateTime.UtcNow;
        // Cấp token đồng thời mới (PostgreSQL không tự sinh rowversion như SQL Server)
        recipe.RowVersion = NewRowVersion();

        // Replace ingredients
        var ingredients = dto.Ingredients?.Select(ingredientDto => new RecipeIngredient
        {
            RecipeId = recipe.Id,
            Name = ingredientDto.Name,
            Quantity = ingredientDto.Quantity,
            Unit = ingredientDto.Unit,
            Notes = ingredientDto.Notes,
            SortOrder = ingredientDto.SortOrder
        }).ToList() ?? new List<RecipeIngredient>();

        var steps = dto.Steps?.Select(stepDto => new RecipeStep
        {
            RecipeId = recipe.Id,
            StepNumber = stepDto.StepNumber,
            Title = stepDto.Title,
            Description = stepDto.Description,
            TimerMinutes = stepDto.TimerMinutes,
            ImageUrl = stepDto.ImageUrl
        }).ToList() ?? new List<RecipeStep>();

        var images = recipe.Images.ToList(); // Keep existing images

        // Update nutrition
        RecipeNutrition nutrition;
        if (dto.Nutrition != null)
        {
            nutrition = new RecipeNutrition
            {
                Calories = dto.Nutrition.Calories,
                Protein = dto.Nutrition.Protein,
                Carbohydrates = dto.Nutrition.Carbohydrates,
                Fat = dto.Nutrition.Fat,
                Fiber = dto.Nutrition.Fiber,
                Sodium = dto.Nutrition.Sodium
            };
        }
        else
        {
            nutrition = new RecipeNutrition();
        }

        _recipeWriteRepository.ReplaceChildren(recipe, ingredients, steps, images);
        recipe.Nutrition = nutrition;

        try
        {
            await _recipeWriteRepository.UpdateAsync(recipe, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(nameof(Recipe), id, ex);
        }

        return await GetDetailByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated recipe.");
    }

    private static byte[] NewRowVersion()
    {
        var token = new byte[8];
        Random.Shared.NextBytes(token);
        return token;
    }

    public async Task DeleteAsync(Guid id, Guid authorId, CancellationToken cancellationToken = default)
    {
        var recipe = await _recipeWriteRepository.GetByIdAsync(id, false, cancellationToken)
            ?? throw new NotFoundException("Recipe", id);

        if (recipe.AuthorId != authorId)
        {
            throw new UnauthorizedAuthException("You are not authorized to delete this recipe.");
        }

        await _recipeWriteRepository.DeleteAsync(recipe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<RecipeDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await _recipeWriteRepository.GetByIdAsync(id, true, cancellationToken);
        if (recipe == null)
        {
            return null;
        }

        return MapToDetailDto(recipe);
    }

    private async Task<RecipeDetailDto?> GetDetailBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var recipe = await _recipeWriteRepository.GetBySlugAsync(slug, false, cancellationToken);
        if (recipe == null)
        {
            return null;
        }

        return MapToDetailDto(recipe);
    }

    private static RecipeDetailDto MapToDetailDto(Recipe recipe)
    {
        return new RecipeDetailDto(
            recipe.Id,
            recipe.Title,
            recipe.Slug,
            recipe.Description,
            recipe.Content,
            recipe.PrepTimeMinutes,
            recipe.CookTimeMinutes,
            recipe.Servings,
            recipe.Difficulty.ToString(),
            recipe.PublishedAt,
            new RecipeCategoryDto(recipe.Category.Id, recipe.Category.Name, recipe.Category.Slug, recipe.Category.Description),
            recipe.Ingredients
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.SortOrder)
                .Select(i => new RecipeIngredientDto(i.Id, i.Name, i.Quantity, i.Unit, i.Notes, i.SortOrder))
                .ToList(),
            recipe.Steps
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.StepNumber)
                .Select(s => new RecipeStepDto(s.Id, s.StepNumber, s.Title, s.Description))
                .ToList(),
            recipe.Images
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.SortOrder)
                .Select(i => new RecipeImageDto(i.Id, i.Url, i.AltText, i.IsPrimary, i.SortOrder))
                .ToList(),
            new RecipeNutritionDto(
                recipe.Nutrition.Calories,
                recipe.Nutrition.Protein,
                recipe.Nutrition.Carbohydrates,
                recipe.Nutrition.Fat,
                recipe.Nutrition.Fiber,
                recipe.Nutrition.Sodium),
            recipe.Author != null
                ? new RecipeAuthorDto(recipe.Author.Id, recipe.Author.DisplayName ?? recipe.Author.UserName ?? "", recipe.Author.AvatarUrl)
                : null,
            recipe.Status);
    }
}