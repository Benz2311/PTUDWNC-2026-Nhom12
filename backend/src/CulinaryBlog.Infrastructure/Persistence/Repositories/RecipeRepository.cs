using CulinaryBlog.Application.Features.Recipes.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Cài đặt RecipeRepository với các truy vấn chi tiết Recipe, Step, Image (Lab 3)
/// </summary>
public class RecipeRepository : Repository<Recipe>, IRecipeRepository
{
    public RecipeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Recipe?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Slug == slug && !r.IsDeleted, cancellationToken);
    }

    public async Task<Recipe?> GetByIdWithImagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    public async Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(r => r.Steps)
            .Include(r => r.Images)
            .Include(r => r.Ingredients)
            .Include(r => r.Author)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<RecipeImage>> GetActiveImagesAsync(Guid recipeId, CancellationToken cancellationToken = default)
    {
        return await _context.RecipeImages
            .Where(img => img.RecipeId == recipeId && !img.IsDeleted)
            .OrderBy(img => img.OrderIndex)
            .ToListAsync(cancellationToken);
    }
}
