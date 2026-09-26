using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Interfaces;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id, bool includeDetails = false, CancellationToken cancellationToken = default);
    Task<Recipe?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Recipe?> GetBySlugAsync(string slug, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);
    void ReplaceChildren(Recipe recipe, ICollection<RecipeIngredient> ingredients, ICollection<RecipeStep> steps, ICollection<RecipeImage> images);
    Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default);
    Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default);
    Task AddIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default);
    Task UpdateIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default);
    Task DeleteIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default);
    Task AddStepAsync(RecipeStep step, CancellationToken cancellationToken = default);
    Task UpdateStepAsync(RecipeStep step, CancellationToken cancellationToken = default);
    Task<bool> DeleteStepAndReorderAsync(Guid recipeId, Guid stepId, CancellationToken cancellationToken = default);
    Task<bool> SetPrimaryImageAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken = default);
}
