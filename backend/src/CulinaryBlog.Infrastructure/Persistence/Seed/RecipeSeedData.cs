using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class RecipeSeedData
{
    private const string DemoUserId = "11111111-1111-1111-1111-111111111111";

    public static async Task InitializeAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(item => item.Id == DemoUserId, cancellationToken);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = DemoUserId,
                FullName = "Culinary Blog Demo",
                UserName = "culinary-demo",
                Email = "demo@culinaryblog.local",
                PasswordHash = "seeded-demo-password",
                EmailConfirmed = true
            };
            db.Users.Add(user);
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
            if (!await db.Categories.AnyAsync(item => item.Slug == category.Slug, cancellationToken))
            {
                db.Categories.Add(category);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        var categoryBySlug = await db.Categories
            .Where(item => categories.Select(category => category.Slug).Contains(item.Slug))
            .ToDictionaryAsync(item => item.Slug, cancellationToken);

        var recipes = new[]
        {
            CreateRecipe("rcp-001", "Phở bò truyền thống", "cat-001", "Nước dùng trong, thơm mùi quế hồi và thịt bò mềm.", "Nấu nước dùng từ xương bò với gừng nướng, hành nướng và gia vị phở. Trụng bánh phở, xếp thịt rồi chan nước dùng nóng.", 20, 180, DifficultyLevel.Medium, 420, 32, 48, 12, 3, new[] { ("Xương bò", "1", "kg"), ("Bánh phở", "500", "g"), ("Thịt bò", "400", "g") }),
            CreateRecipe("rcp-002", "Cơm chiên rau củ", "cat-002", "Món cơm nhanh gọn, nhiều màu sắc và dễ biến tấu.", "Phi thơm hành tỏi, xào rau củ, thêm cơm nguội và nêm vừa ăn. Đảo trên lửa lớn đến khi hạt cơm tơi.", 10, 25, DifficultyLevel.Easy, 360, 10, 55, 9, 5, new[] { ("Cơm nguội", "3", "chén"), ("Cà rốt", "1", "củ"), ("Đậu Hà Lan", "100", "g") }),
            CreateRecipe("rcp-003", "Mì Ý sốt cà chua", "cat-003", "Mì Ý đơn giản với sốt cà chua tươi và rau thơm.", "Luộc mì al dente. Xào tỏi với dầu ô liu, thêm cà chua và nấu đến khi sốt sánh, trộn cùng mì.", 15, 30, DifficultyLevel.Easy, 390, 14, 62, 11, 6, new[] { ("Mì spaghetti", "200", "g"), ("Cà chua", "4", "quả"), ("Dầu ô liu", "2", "muỗng canh") }),
            CreateRecipe("rcp-004", "Đậu hũ kho nấm", "cat-004", "Món chay đậm vị, dùng ngon cùng cơm nóng.", "Chiên sơ đậu hũ, xào nấm rồi thêm nước tương và gia vị. Kho nhỏ lửa đến khi nước sốt áo đều.", 18, 35, DifficultyLevel.Easy, 280, 18, 24, 12, 8, new[] { ("Đậu hũ", "400", "g"), ("Nấm", "200", "g"), ("Nước tương", "2", "muỗng canh") }),
            CreateRecipe("rcp-005", "Chè hạt sen long nhãn", "cat-005", "Món tráng miệng thanh nhẹ, thơm dịu.", "Nấu mềm hạt sen, thêm đường phèn rồi cho long nhãn vào sau cùng để giữ độ giòn.", 20, 45, DifficultyLevel.Medium, 220, 6, 38, 2, 4, new[] { ("Hạt sen", "150", "g"), ("Long nhãn", "100", "g"), ("Đường phèn", "80", "g") })
        };

        foreach (var recipe in recipes)
        {
            if (await db.Recipes.AnyAsync(item => item.Slug == recipe.Slug, cancellationToken))
            {
                continue;
            }

            recipe.AuthorId = DemoUserId;
            var categorySlug = recipe.Category.Slug;
            recipe.CategoryId = categoryBySlug[categorySlug].Id;
            recipe.Category = null!;
            db.Recipes.Add(recipe);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Category CreateCategory(string slug, string name, string description) =>
        new() { Slug = slug, Name = name, Description = description };

    private static Recipe CreateRecipe(string slug, string title, string categorySlug, string description, string content, int prepTimeMinutes, int cookTime, DifficultyLevel difficulty, decimal calories, decimal protein, decimal carbohydrates, decimal fat, decimal fiber, (string Name, string Quantity, string Unit)[] ingredients) =>
        new()
        {
            Slug = slug,
            Title = title,
            Category = new Category { Slug = categorySlug },
            Description = description,
            Content = content,
            PrepTimeMinutes = prepTimeMinutes,
            CookTimeMinutes = cookTime,
            Difficulty = difficulty,
            Status = RecipeStatus.Published,
            Nutrition = new RecipeNutrition
            {
                Calories = calories,
                Protein = protein,
                Carbohydrates = carbohydrates,
                Fat = fat,
                Fiber = fiber
            },
            Ingredients = ingredients.Select((ingredient, index) => new RecipeIngredient
            {
                Name = ingredient.Name,
                Quantity = decimal.TryParse(ingredient.Quantity, out var quantity) ? quantity : null,
                Unit = ingredient.Unit,
                SortOrder = index
            }).ToList()
        };
}
