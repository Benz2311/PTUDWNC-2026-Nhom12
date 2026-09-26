using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class RecipeSeedData
{
    private static readonly Guid DemoUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Random Random = new(20260922);

    private static readonly string[] RecipeBases =
    [
        "Phở bò",
        "Bún thịt nướng",
        "Cơm chiên",
        "Gỏi cuốn",
        "Bún chả",
        "Canh chua",
        "Cá kho",
        "Thịt kho",
        "Bánh xèo",
        "Chả giò",
        "Mì Ý",
        "Pizza rau củ",
        "Pasta sốt kem",
        "Salad gà",
        "Cơm cà ri",
        "Mì xào",
        "Gà nướng",
        "Bò xào rau củ",
        "Cá áp chảo",
        "Súp bí đỏ",
        "Đậu hũ kho",
        "Nấm xào",
        "Rau củ nướng",
        "Cháo gà",
        "Chè hạt sen",
        "Bánh chuối",
        "Pudding sữa",
        "Sinh tố xoài",
        "Kem dừa",
        "Bánh flan"
    ];

    private static readonly string[] IngredientNames =
    [
        "Thịt bò",
        "Thịt heo",
        "Thịt gà",
        "Thịt vịt",
        "Cá hồi",
        "Cá basa",
        "Tôm",
        "Mực",
        "Trứng gà",
        "Đậu hũ",
        "Nấm hương",
        "Nấm rơm",
        "Cà rốt",
        "Khoai tây",
        "Khoai lang",
        "Bí đỏ",
        "Bông cải xanh",
        "Đậu que",
        "Đậu Hà Lan",
        "Cà chua",
        "Dưa leo",
        "Hành tây",
        "Hành lá",
        "Tỏi",
        "Gừng",
        "Sả",
        "Ớt",
        "Rau mùi",
        "Húng quế",
        "Chanh",
        "Me",
        "Dứa",
        "Xoài",
        "Chuối",
        "Chanh dây",
        "Bánh phở",
        "Bún tươi",
        "Mì spaghetti",
        "Mì trứng",
        "Gạo",
        "Cơm nguội",
        "Bột mì",
        "Bột gạo",
        "Bột bắp",
        "Nước tương",
        "Nước mắm",
        "Dầu ăn",
        "Dầu ô liu",
        "Đường",
        "Muối",
        "Tiêu",
        "Đường phèn",
        "Nước cốt dừa",
        "Sữa tươi",
        "Phô mai",
        "Bơ",
        "Giấm",
        "Dầu hào",
        "Tương ớt",
        "Rau cải",
        "Xà lách",
        "Ngô ngọt",
        "Hạt sen",
        "Long nhãn",
        "Đậu đỏ",
        "Mè trắng",
        "Đậu phộng",
        "Hạt điều"
    ];

    private static readonly string[] Units =
    [
        "g",
        "kg",
        "ml",
        "l",
        "quả",
        "củ",
        "bó",
        "muỗng canh",
        "muỗng cà phê",
        "chén"
    ];

    public static async Task InitializeAsync(
        ApplicationDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var demoPassword = configuration["Seed:DemoPassword"];
        if (string.IsNullOrWhiteSpace(demoPassword) || demoPassword.Length < 12)
        {
            throw new InvalidOperationException("Seed:DemoPassword must be configured with at least 12 characters.");
        }

        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var user = await db.Users
            .SingleOrDefaultAsync(item => item.Id == DemoUserId, cancellationToken);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = DemoUserId,
                FullName = "Culinary Blog Demo",
                UserName = "culinary-demo",
                Email = "demo@culinaryblog.local",
                EmailConfirmed = true
            };
            user.PasswordHash = passwordHasher.HashPassword(user, demoPassword);

            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (NeedsPasswordReset(user, passwordHasher, demoPassword))
        {
            user.PasswordHash = passwordHasher.HashPassword(user, demoPassword);
            await db.SaveChangesAsync(cancellationToken);
        }

        var categories = new[]
        {
            CreateCategory("cat-001", "Món Việt", "Công thức đậm đà hương vị Việt."),
            CreateCategory("cat-002", "Món Á", "Những món ăn châu Á dễ thực hiện."),
            CreateCategory("cat-003", "Món Âu", "Công thức phương Tây cho căn bếp gia đình."),
            CreateCategory("cat-004", "Món Chay", "Món chay cân bằng và giàu dinh dưỡng."),
            CreateCategory("cat-005", "Tráng Miệng", "Các món ngọt cho ngày thêm vui.")
        };

        foreach (var category in categories)
        {
            var exists = await db.Categories
                .AnyAsync(item => item.Slug == category.Slug, cancellationToken);

            if (!exists)
            {
                db.Categories.Add(category);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var categoryBySlug = await db.Categories
            .Where(item => categories
                .Select(category => category.Slug)
                .Contains(item.Slug))
            .ToDictionaryAsync(item => item.Slug, cancellationToken);

        // Tạo dữ liệu ngẫu nhiên cho đến khi database có ít nhất 100 Recipes.
        var existingRecipeCount = await db.Recipes.CountAsync(cancellationToken);
        var recipesToCreate = Math.Max(0, 100 - existingRecipeCount);

        for (var i = 0; i < recipesToCreate; i++)
        {
            var recipeNumber = existingRecipeCount + i + 1;

            var category = categories[Random.Next(categories.Length)];
            var categoryEntity = categoryBySlug[category.Slug];

            var recipe = CreateRandomRecipe(
                recipeNumber,
                categoryEntity);

            db.Recipes.Add(recipe);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Đảm bảo tất cả Recipes, kể cả 5 Recipes cũ,
        // đều có ít nhất 10 Ingredients.
        var allRecipes = await db.Recipes
            .Include(recipe => recipe.Ingredients)
            .ToListAsync(cancellationToken);

        foreach (var recipe in allRecipes)
{
    var missingIngredients = 10 - recipe.Ingredients.Count;

    if (missingIngredients <= 0)
    {
        continue;
    }

    var selectedIngredients = IngredientNames
        .OrderBy(_ => Random.Next())
        .Take(missingIngredients)
        .ToArray();

    var startOrder = recipe.Ingredients.Count;

    for (var i = 0; i < selectedIngredients.Length; i++)
    {
        // Add trực tiếp vào DbSet RecipeIngredients thay vì dùng Navigation Property
        db.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = recipe.Id, // Gán Id của Recipe
            Name = selectedIngredients[i],
            Quantity = RandomQuantity(),
            Unit = Units[Random.Next(Units.Length)],
            Notes = Random.Next(4) == 0
                ? "Có thể điều chỉnh theo khẩu vị."
                : null,
            SortOrder = startOrder + i
        });
    }
}

await db.SaveChangesAsync(cancellationToken);
    }

    private static Category CreateCategory(
        string slug,
        string name,
        string description) =>
        new()
        {
            Slug = slug,
            Name = name,
            Description = description
        };

    private static bool NeedsPasswordReset(ApplicationUser user, PasswordHasher<ApplicationUser> passwordHasher, string demoPassword)
    {
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return true;
        }

        try
        {
            return passwordHasher.VerifyHashedPassword(user, user.PasswordHash, demoPassword) == PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return true;
        }
    }

    private static Recipe CreateRandomRecipe(
        int recipeNumber,
        Category category)
    {
        var baseName = RecipeBases[Random.Next(RecipeBases.Length)];

        var title = $"{baseName} phiên bản {recipeNumber:000}";
        var slug = $"recipe-{recipeNumber:000}";

        var prepTime = Random.Next(5, 46);
        var cookTime = Random.Next(10, 121);
        var servings = Random.Next(1, 7);

        var ingredients = CreateRandomIngredients();

        var nutrition = new RecipeNutrition
        {
            Calories = RandomDecimal(180, 850),
            Protein = RandomDecimal(5, 45),
            Carbohydrates = RandomDecimal(10, 100),
            Fat = RandomDecimal(3, 40),
            Fiber = RandomDecimal(1, 15),
            Sodium = RandomDecimal(50, 1200)
        };

        return new Recipe
        {
            AuthorId = DemoUserId,
            CategoryId = category.Id,
            Title = title,
            Slug = slug,
            Description =
                $"Công thức {baseName.ToLowerInvariant()} được sinh tự động " +
                $"với dữ liệu nguyên liệu và dinh dưỡng ngẫu nhiên.",
            Content =
                $"Chuẩn bị nguyên liệu, sơ chế và nấu {baseName.ToLowerInvariant()} " +
                $"trong khoảng {cookTime} phút. Nêm nếm phù hợp với khẩu vị.",
            PrepTimeMinutes = prepTime,
            CookTimeMinutes = cookTime,
            Servings = servings,
            Difficulty = RandomDifficulty(),
            Status = RecipeStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-Random.Next(0, 365)),
            Nutrition = nutrition,
            Ingredients = ingredients,
            Steps =
            [
                new RecipeStep
                {
                    StepNumber = 1,
                    Title = "Thực hiện",
                    Description = $"Sơ chế và nấu {baseName.ToLowerInvariant()} theo hướng dẫn."
                }
            ]
        };
    }

    private static List<RecipeIngredient> CreateRandomIngredients()
    {
        var count = Random.Next(10, 16);

        return IngredientNames
            .OrderBy(_ => Random.Next())
            .Take(count)
            .Select((name, index) => new RecipeIngredient
            {
                Name = name,
                Quantity = RandomQuantity(),
                Unit = Units[Random.Next(Units.Length)],
                Notes = Random.Next(5) == 0
                    ? "Có thể thay thế bằng nguyên liệu tương đương."
                    : null,
                SortOrder = index
            })
            .ToList();
    }

    private static decimal RandomQuantity()
    {
        var value = Random.Next(1, 501);

        return Math.Round(value / 10m, 1);
    }

    private static decimal RandomDecimal(decimal min, decimal max)
    {
        var value = (decimal)Random.NextDouble() * (max - min) + min;

        return Math.Round(value, 2);
    }

    private static DifficultyLevel RandomDifficulty()
    {
        return (DifficultyLevel)Random.Next(1, 5);
    }
}
