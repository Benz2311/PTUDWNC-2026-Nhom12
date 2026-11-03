using Bogus;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class RecipeSampleFactory
{
    private static readonly string[] CookingMethods =
    [
        "Kho",
        "Xào",
        "Nướng",
        "Chiên",
        "Hấp",
        "Luộc",
        "Om",
        "Rim",
        "Áp chảo",
        "Nấu canh",
        "Trộn",
        "Hầm"
    ];

    private static readonly string[] MainIngredients =
    [
        "thịt bò",
        "thịt heo",
        "thịt gà",
        "cá hồi",
        "cá basa",
        "tôm",
        "mực",
        "đậu hũ",
        "nấm",
        "trứng",
        "bí đỏ",
        "rau củ"
    ];

    private static readonly string[] FlavorStyles =
    [
        "sả ớt",
        "tiêu xanh",
        "nước dừa",
        "gừng hành",
        "chua ngọt",
        "ngũ vị",
        "lá chanh",
        "mè rang",
        "thảo mộc",
        "sốt cà chua",
        "riềng mẻ",
        "tỏi thơm"
    ];

    private static readonly string[] IngredientNames =
    [
        "Thịt bò", "Thịt heo", "Thịt gà", "Thịt vịt", "Cá hồi", "Cá basa", "Tôm", "Mực",
        "Trứng gà", "Đậu hũ", "Nấm hương", "Nấm rơm", "Cà rốt", "Khoai tây", "Khoai lang",
        "Bí đỏ", "Bông cải xanh", "Đậu que", "Đậu Hà Lan", "Cà chua", "Dưa leo", "Hành tây",
        "Hành lá", "Tỏi", "Gừng", "Sả", "Ớt", "Rau mùi", "Húng quế", "Chanh", "Me",
        "Dứa", "Xoài", "Chuối", "Bánh phở", "Bún tươi", "Mì spaghetti", "Mì trứng", "Gạo",
        "Cơm nguội", "Bột mì", "Bột gạo", "Nước tương", "Nước mắm", "Dầu ăn", "Dầu ô liu",
        "Đường", "Muối", "Tiêu", "Nước cốt dừa", "Sữa tươi", "Phô mai", "Bơ", "Giấm",
        "Dầu hào", "Rau cải", "Xà lách", "Ngô ngọt", "Hạt sen", "Đậu đỏ", "Mè trắng", "Đậu phộng"
    ];

    private static readonly string[] Units =
    [
        "g", "kg", "ml", "l", "quả", "củ", "bó", "muỗng canh", "muỗng cà phê", "chén"
    ];

    public static Recipe Create(Guid authorId, Category category)
    {
        var faker = new Faker("vi");
        var method = faker.PickRandom(CookingMethods);
        var mainIngredient = faker.PickRandom(MainIngredients);
        var flavor = faker.PickRandom(FlavorStyles);
        var title = $"{method} {mainIngredient} {flavor}";
        var recipe = new Recipe
        {
            AuthorId = authorId,
            CategoryId = category.Id,
            Title = title,
            Slug = $"mon-{Guid.NewGuid():N}",
            Description = $"{title} với cách nêm nếm {faker.Lorem.Sentence(8)}",
            Content = $"Sơ chế nguyên liệu thật sạch. {faker.Lorem.Sentences(3)} " +
                $"Nêm nếm món {title.ToLowerInvariant()} theo khẩu vị và trình bày khi còn nóng.",
            PrepTimeMinutes = faker.Random.Int(10, 60),
            CookTimeMinutes = faker.Random.Int(10, 150),
            Servings = faker.Random.Int(2, 8),
            Difficulty = faker.PickRandom<DifficultyLevel>(),
            Status = RecipeStatus.Published,
            PublishedAt = faker.Date.Recent(365).ToUniversalTime(),
            Nutrition = CreateNutrition(faker)
        };
        recipe.Ingredients = CreateIngredients(recipe.Id, faker.Random.Int(10, 15), [], faker);
        recipe.Steps = RecipeStepSeeder.Generate([recipe]);
        recipe.Images = RecipeImageSeeder.Generate([recipe]);
        return recipe;
    }

    public static List<RecipeIngredient> CreateIngredients(
        Guid recipeId,
        int count,
        IEnumerable<string> existingNames,
        Faker? faker = null)
    {
        faker ??= new Faker("vi");
        var excludedNames = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var availableNames = IngredientNames
            .Where(name => !excludedNames.Contains(name))
            .ToList();
        var selectedNames = faker.Random.Shuffle(availableNames)
            .Take(Math.Min(count, availableNames.Count));

        return selectedNames
            .Select((name, index) => new RecipeIngredient
            {
                RecipeId = recipeId,
                Name = name,
                Quantity = Math.Round((decimal)faker.Random.Double(5, 800), 1),
                Unit = faker.PickRandom(Units),
                Notes = faker.Random.Bool(0.25f) ? faker.Lorem.Sentence(5) : null,
                SortOrder = index
            })
            .ToList();
    }

    public static RecipeNutrition CreateNutrition(Faker? faker = null)
    {
        faker ??= new Faker("vi");
        return new RecipeNutrition
        {
            Source = NutritionSource.Manual,
            Calories = faker.Random.Int(180, 900),
            Protein = faker.Random.Int(5, 55),
            Carbohydrates = faker.Random.Int(10, 120),
            Fat = faker.Random.Int(3, 50),
            Fiber = faker.Random.Int(1, 20),
            Sodium = faker.Random.Int(50, 1500)
        };
    }
}