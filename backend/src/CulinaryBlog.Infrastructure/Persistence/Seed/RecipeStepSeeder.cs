using Bogus;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class RecipeStepSeeder
{
    public static List<RecipeStep> Generate(IEnumerable<Recipe> recipes)
    {
        var steps = new List<RecipeStep>();
        var faker = new Faker("vi");

        foreach (var recipe in recipes)
        {
            // Mỗi công thức có ngẫu nhiên từ 5 đến 8 bước
            var numberOfSteps = faker.Random.Int(5, 8);

            for (var stepNumber = 1; stepNumber <= numberOfSteps; stepNumber++)
            {
                var step = new RecipeStep
                {
                    Id = Guid.NewGuid(),
                    RecipeId = recipe.Id,
                    StepNumber = stepNumber,
                    Title = $"Bước {stepNumber}",
                    Description = faker.Lorem.Sentence(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null,
                    IsDeleted = false
                };

                steps.Add(step);
            }
        }

        return steps;
    }
}