using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Application.Features.Recipes;

public sealed class RecipeWriteService : IRecipeWriteService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICulinaryBlogTelemetry? _telemetry;

    public RecipeWriteService(
        IRecipeRepository recipeRepository,
        IUnitOfWork unitOfWork,
        ICulinaryBlogTelemetry? telemetry = null)
    {
        _recipeRepository = recipeRepository;
        _unitOfWork = unitOfWork;
        _telemetry = telemetry;
    }

    public async Task CreateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        await _recipeRepository.AddAsync(recipe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _telemetry?.RecordRecipeCreated(recipe.CategoryId.ToString());
        if (recipe.Status == RecipeStatus.Published)
        {
            _telemetry?.RecordRecipePublished(recipe.CategoryId.ToString());
        }
    }

    public async Task PublishAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        recipe.Status = RecipeStatus.Published;
        recipe.PublishedAt ??= DateTime.UtcNow;
        await _recipeRepository.UpdateAsync(recipe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _telemetry?.RecordRecipePublished(recipe.CategoryId.ToString());
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        await _recipeRepository.UpdateAsync(recipe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceContentsAsync(Recipe recipe, ICollection<RecipeIngredient> ingredients, ICollection<RecipeStep> steps, ICollection<RecipeImage> images, CancellationToken cancellationToken = default)
    {
        _recipeRepository.ReplaceChildren(recipe, ingredients, steps, images);
        await _recipeRepository.UpdateAsync(recipe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        await _recipeRepository.DeleteAsync(recipe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AddIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        await _recipeRepository.AddIngredientAsync(ingredient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        await _recipeRepository.UpdateIngredientAsync(ingredient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        await _recipeRepository.DeleteIngredientAsync(ingredient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AddStepAsync(RecipeStep step, CancellationToken cancellationToken = default)
    {
        await _recipeRepository.AddStepAsync(step, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStepAsync(RecipeStep step, CancellationToken cancellationToken = default)
    {
        await _recipeRepository.UpdateStepAsync(step, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteStepAndReorderAsync(Guid recipeId, Guid stepId, CancellationToken cancellationToken = default)
    {
        if (!await _recipeRepository.DeleteStepAndReorderAsync(recipeId, stepId, cancellationToken))
        {
            return false;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetPrimaryImageAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken = default)
    {
        if (!await _recipeRepository.SetPrimaryImageAsync(recipeId, imageId, cancellationToken))
        {
            return false;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteImageAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken = default)
    {
        if (!await _recipeRepository.DeleteImageAsync(recipeId, imageId, cancellationToken))
        {
            return false;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}