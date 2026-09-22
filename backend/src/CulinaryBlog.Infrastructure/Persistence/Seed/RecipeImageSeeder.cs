using Bogus;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class RecipeImageSeeder
{
    public static List<RecipeImage> Generate(IEnumerable<Recipe> recipes)
    {
        var images = new List<RecipeImage>();
        var faker = new Faker("vi");

        foreach (var recipe in recipes)
        {
            // Mỗi công thức có ngẫu nhiên từ 2 đến 5 hình ảnh
            var numberOfImages = faker.Random.Int(2, 5);

            for (var index = 0; index < numberOfImages; index++)
            {
                var image = new RecipeImage
                {
                    Id = Guid.NewGuid(),
                    RecipeId = recipe.Id,

                    // Dữ liệu URL mẫu để kiểm thử
                    Url = $"https://picsum.photos/seed/{recipe.Id}-{index}/800/600",

                    AltText = $"Hình ảnh {index + 1} của {recipe.Title}",

                    // Ảnh đầu tiên là ảnh chính
                    IsPrimary = index == 0,

                    // Thứ tự hiển thị bắt đầu từ 0
                    SortOrder = index,

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null,
                    IsDeleted = false
                };

                images.Add(image);
            }
        }

        return images;
    }
}