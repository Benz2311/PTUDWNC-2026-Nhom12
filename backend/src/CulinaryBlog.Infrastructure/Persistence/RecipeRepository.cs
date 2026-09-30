using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

public class RecipeRepository : IRecipeRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RecipeRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Recipe?> GetByIdAsync(Guid id, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Recipe> query = _dbContext.Recipes
            .AsNoTracking();

        if (includeDetails)
        {
            query = query
                .Include(recipe => recipe.Category)
                .Include(recipe => recipe.Author)
                .Include(recipe => recipe.Ingredients.OrderBy(ingredient => ingredient.SortOrder))
                .Include(recipe => recipe.Steps.OrderBy(step => step.StepNumber))
                .Include(recipe => recipe.Images.OrderBy(image => image.SortOrder))
                .Include(recipe => recipe.Nutrition)
                .AsSplitQuery();
        }

        return query.SingleOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);
    }

    public Task<Recipe?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Recipes
            .Include(recipe => recipe.Ingredients)
            .Include(recipe => recipe.Steps)
            .Include(recipe => recipe.Images)
            .Include(recipe => recipe.Nutrition)
            .AsSplitQuery()
            .SingleOrDefaultAsync(recipe => recipe.Id == id && !recipe.IsDeleted, cancellationToken);
    }

    public Task<Recipe?> GetBySlugAsync(string slug, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Recipes
            .AsNoTracking()
            .Include(recipe => recipe.Category)
            .Include(recipe => recipe.Author)
            .Include(recipe => recipe.Ingredients.OrderBy(ingredient => ingredient.SortOrder))
            .Include(recipe => recipe.Steps.OrderBy(step => step.StepNumber))
            .Include(recipe => recipe.Images.OrderBy(image => image.SortOrder))
            .Include(recipe => recipe.Nutrition)
            .AsSplitQuery()
            .Where(recipe => recipe.Slug == slug);

        if (!includeDeleted)
        {
            query = query.Where(recipe => !recipe.IsDeleted);
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim();
        return _dbContext.Recipes
            .AnyAsync(recipe => recipe.Slug == normalizedSlug && (!excludeId.HasValue || recipe.Id != excludeId.Value) && !recipe.IsDeleted, cancellationToken);
    }

    public Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        _dbContext.Recipes.Add(recipe);
        return Task.CompletedTask;
    }

    public void ReplaceChildren(Recipe recipe, ICollection<RecipeIngredient> ingredients, ICollection<RecipeStep> steps, ICollection<RecipeImage> images)
    {
        _dbContext.RecipeIngredients.RemoveRange(recipe.Ingredients);
        _dbContext.RecipeSteps.RemoveRange(recipe.Steps);
        _dbContext.RecipeImages.RemoveRange(recipe.Images);
        recipe.Ingredients = ingredients;
        recipe.Steps = steps;
        recipe.Images = images;
        _dbContext.RecipeIngredients.AddRange(ingredients);
        _dbContext.RecipeSteps.AddRange(steps);
        _dbContext.RecipeImages.AddRange(images);
    }

    public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(recipe).State == EntityState.Detached)
        {
            throw new InvalidOperationException("Recipe must be loaded as a tracked aggregate before it can be updated.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        recipe.IsDeleted = true;
        recipe.UpdatedAt = DateTime.UtcNow;
        var entry = _dbContext.Entry(recipe);
        if (entry.State == EntityState.Detached)
        {
            _dbContext.Recipes.Attach(recipe);
            entry.Property(item => item.IsDeleted).IsModified = true;
            entry.Property(item => item.UpdatedAt).IsModified = true;
        }

        return Task.CompletedTask;
    }

    public Task AddIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        _dbContext.RecipeIngredients.Add(ingredient);
        return Task.CompletedTask;
    }

    public Task UpdateIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        _dbContext.Entry(ingredient).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task DeleteIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
    {
        _dbContext.RecipeIngredients.Remove(ingredient);
        return Task.CompletedTask;
    }

    public Task AddStepAsync(RecipeStep step, CancellationToken cancellationToken = default)
    {
        _dbContext.RecipeSteps.Add(step);
        return Task.CompletedTask;
    }

    public Task UpdateStepAsync(RecipeStep step, CancellationToken cancellationToken = default)
    {
        _dbContext.Entry(step).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteStepAndReorderAsync(Guid recipeId, Guid stepId, CancellationToken cancellationToken = default)
    {
        var step = await _dbContext.RecipeSteps
            .SingleOrDefaultAsync(item => item.Id == stepId && item.RecipeId == recipeId, cancellationToken);
        if (step is null)
        {
            return false;
        }

        _dbContext.RecipeSteps.Remove(step);
        var remainingSteps = await _dbContext.RecipeSteps
            .Where(item => item.RecipeId == recipeId && item.Id != stepId)
            .OrderBy(item => item.StepNumber)
            .ToListAsync(cancellationToken);
        for (var index = 0; index < remainingSteps.Count; index++)
        {
            remainingSteps[index].StepNumber = index + 1;
        }

        return true;
    }

    public async Task<bool> SetPrimaryImageAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var images = await _dbContext.RecipeImages
            .Where(item => item.RecipeId == recipeId)
            .ToListAsync(cancellationToken);
        if (images.All(image => image.Id != imageId))
        {
            return false;
        }

        foreach (var image in images)
        {
            image.IsPrimary = image.Id == imageId;
        }

        return true;
    }

    public async Task<bool> DeleteImageAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var images = await _dbContext.RecipeImages
            .Where(item => item.RecipeId == recipeId)
            .ToListAsync(cancellationToken);
        var image = images.SingleOrDefault(item => item.Id == imageId);
        if (image is null)
        {
            return false;
        }

        _dbContext.RecipeImages.Remove(image);
        if (image.IsPrimary)
        {
            var replacement = images
                .Where(item => item.Id != imageId)
                .OrderBy(item => item.SortOrder)
                .FirstOrDefault();
            if (replacement is not null)
            {
                replacement.IsPrimary = true;
            }
        }

        return true;
    }
}
