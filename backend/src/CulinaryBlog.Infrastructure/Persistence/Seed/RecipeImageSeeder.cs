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
            // Sinh ngẫu nhiên từ 2 đến 5 ảnh cho mỗi Recipe
            var numberOfImages = faker.Random.Int(2, 5);

            for (var index = 0;
                 index < numberOfImages;
                 index++)
            {
                var imageFaker = new Faker<RecipeImage>("vi")
                    .RuleFor(x => x.Id, f => f.Random.Guid())
                    .RuleFor(x => x.RecipeId, _ => recipe.Id)

                    // Sinh URL ảnh ngẫu nhiên
                    .RuleFor(
                        x => x.Url,
                        f => f.Image.PicsumUrl(
                            width: 800,
                            height: 600))

                    // Sinh mô tả ảnh
                    .RuleFor(
                        x => x.AltText,
                        f => f.Lorem
                            .Sentence(f.Random.Int(3, 7)))

                    // Ảnh đầu tiên là ảnh đại diện
                    .RuleFor(
                        x => x.IsPrimary,
                        _ => index == 0)

                    // Thứ tự ảnh
                    .RuleFor(
                        x => x.SortOrder,
                        _ => index)

                    // Thời gian tạo
                    .RuleFor(
                        x => x.CreatedAt,
                        f => f.Date
                            .Recent(30)
                            .ToUniversalTime())

                    .RuleFor(x => x.UpdatedAt, _ => null)
                    .RuleFor(x => x.IsDeleted, _ => false)

                    // PostgreSQL không tự sinh RowVersion
                    // nên Seeder chủ động gán giá trị
                    .RuleFor(
                        x => x.RowVersion,
                        _ => new byte[8]);

                images.Add(imageFaker.Generate());
            }
        }

        return images;
    }
}