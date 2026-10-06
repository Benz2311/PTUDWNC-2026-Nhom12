using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class RecipeSampleImageSeeder
{
    private const int SampleImageCount = 8;

    public static async Task EnsureImagesAsync(
        ApplicationDbContext db,
        Guid sampleAuthorId,
        CancellationToken cancellationToken = default)
    {
        var recipesWithoutImages = await db.Recipes
            .AsNoTracking()
            .Where(recipe => recipe.AuthorId == sampleAuthorId &&
                !db.RecipeImages.Any(image => image.RecipeId == recipe.Id && !image.IsDeleted))
            .Select(recipe => new { recipe.Id, recipe.Slug, recipe.Title })
            .ToListAsync(cancellationToken);

        foreach (var recipe in recipesWithoutImages)
        {
            var sampleIndex = recipe.Slug.Sum(character => character) % SampleImageCount + 1;
            db.RecipeImages.Add(new RecipeImage
            {
                RecipeId = recipe.Id,
                Url = $"/sample-images/food-{sampleIndex:00}.svg",
                AltText = $"Ảnh minh họa món {recipe.Title}",
                IsPrimary = true,
                SortOrder = 0
            });
        }

        if (recipesWithoutImages.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
