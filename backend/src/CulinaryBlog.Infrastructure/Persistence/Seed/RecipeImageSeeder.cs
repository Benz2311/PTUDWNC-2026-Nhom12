using Bogus;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class RecipeImageSeeder
{
    private const int DefaultSeed = 20260925;

    public static List<RecipeImage> Generate(IEnumerable<Recipe> recipes, int seed = DefaultSeed)
    {
        var images = new List<RecipeImage>();
        var orderedRecipes = recipes.OrderBy(x => x.Id).ThenBy(x => x.Title).ToList();
        var faker = new Faker("vi")
        {
            Random = new Randomizer(seed)
        };
        var refDate = new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc);

        foreach (var recipe in orderedRecipes)
        {
            // Sinh ngẫu nhiên từ 2 đến 5 ảnh cho mỗi Recipe
            var numberOfImages = faker.Random.Int(2, 5);

            for (var index = 0; index < numberOfImages; index++)
            {
                var image = new RecipeImage
                {
                    Id = faker.Random.Guid(),
                    RecipeId = recipe.Id,
                    Url = faker.Image.PicsumUrl(width: 800, height: 600),
                    MediumUrl = null,
                    ThumbnailUrl = null,
                    AltText = faker.Lorem.Sentence(faker.Random.Int(3, 7)),
                    IsPrimary = index == 0,
                    SortOrder = index,
                    CreatedAt = faker.Date.Recent(30, refDate).ToUniversalTime(),
                    UpdatedAt = null,
                    IsDeleted = false,
                    RowVersion = new byte[8]
                };

                images.Add(image);
            }
        }

        return images;
    }
}