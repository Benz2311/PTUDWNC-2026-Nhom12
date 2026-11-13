using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class RecipeSeedData
{
    private static readonly Guid DemoUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid NgocPhuocUserId = Guid.Parse("25525525-5255-5255-5255-255255255255");

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
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Roles = ["Author"]
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

        await EnsureNgocPhuocRecipesAsync(
            db,
            passwordHasher,
            demoPassword,
            categoryBySlug,
            cancellationToken);

        await RecipeSampleImageSeeder.EnsureImagesAsync(db, DemoUserId, cancellationToken);
    }

    private static async Task EnsureNgocPhuocRecipesAsync(
        ApplicationDbContext db,
        PasswordHasher<ApplicationUser> passwordHasher,
        string demoPassword,
        IReadOnlyDictionary<string, Category> categoryBySlug,
        CancellationToken cancellationToken)
    {
        const string email = "ngocphuoc255@gmail.com";

        var targetUser = await db.Users
            .SingleOrDefaultAsync(item => item.Email.ToLower() == email, cancellationToken);

        if (targetUser is null)
        {
            targetUser = new ApplicationUser
            {
                Id = NgocPhuocUserId,
                FullName = "Ngoc Phuoc",
                UserName = "ngocphuoc255",
                NormalizedUserName = "NGOCPHUOC255",
                Email = email,
                NormalizedEmail = "NGOCPHUOC255@GMAIL.COM",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Roles = ["Author"]
            };
            targetUser.PasswordHash = passwordHasher.HashPassword(targetUser, demoPassword);
            db.Users.Add(targetUser);
        }
        else
        {
            var needsUpdate =
                !targetUser.EmailConfirmed ||
                !targetUser.IsActive ||
                string.IsNullOrWhiteSpace(targetUser.FullName) ||
                targetUser.NormalizedEmail != "NGOCPHUOC255@GMAIL.COM" ||
                NeedsPasswordReset(targetUser, passwordHasher, demoPassword);

            targetUser.EmailConfirmed = true;
            targetUser.IsActive = true;
            targetUser.FullName = string.IsNullOrWhiteSpace(targetUser.FullName) ? "Ngoc Phuoc" : targetUser.FullName;
            targetUser.NormalizedEmail = "NGOCPHUOC255@GMAIL.COM";
            targetUser.Roles = ["Author"];

            if (needsUpdate || string.IsNullOrWhiteSpace(targetUser.PasswordHash))
            {
                targetUser.PasswordHash = passwordHasher.HashPassword(targetUser, demoPassword);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var category = categoryBySlug.Values.First();
        var existingRecipes = await db.Recipes
            .Include(item => item.Ingredients)
            .Include(item => item.Steps)
            .Include(item => item.Images)
            .Where(item => item.AuthorId == targetUser.Id && item.Slug.StartsWith("ngocphuoc-mau-"))
            .ToDictionaryAsync(item => item.Slug, cancellationToken);
        var existingSlugs = await db.Recipes
            .IgnoreQueryFilters()
            .Where(item => item.Slug.StartsWith("ngocphuoc-mau-"))
            .Select(item => item.Slug)
            .ToHashSetAsync(cancellationToken);

        for (var i = 1; i <= 100; i++)
        {
            var slug = $"ngocphuoc-mau-{i:000}";
            if (existingSlugs.Add(slug))
            {
                var baseName = RecipeBases[(i - 1) % RecipeBases.Length];
                existingRecipes[slug] = CreateNgocPhuocRecipe(
                    i,
                    baseName,
                    targetUser.Id,
                    category.Id);
                db.Recipes.Add(existingRecipes[slug]);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var targetRecipes = await db.Recipes
            .Include(item => item.Ingredients)
            .Include(item => item.Steps)
            .Include(item => item.Images)
            .Where(item => item.AuthorId == targetUser.Id && item.Slug.StartsWith("ngocphuoc-mau-"))
            .ToListAsync(cancellationToken);

        foreach (var recipe in targetRecipes)
        {
            var index = int.TryParse(
                recipe.Slug.Replace("ngocphuoc-mau-", string.Empty),
                out var parsedIndex)
                ? parsedIndex
                : 1;
            var baseName = RecipeBases[(index - 1) % RecipeBases.Length];
            recipe.Title = baseName;
            recipe.Description =
                $"Công thức {baseName.ToLowerInvariant()} dành cho thực hành và kiểm thử dữ liệu.";
            recipe.Content =
                $"Chuẩn bị nguyên liệu đầy đủ, sơ chế sạch và chế biến {baseName.ToLowerInvariant()} theo từng bước.";

            foreach (var ingredient in recipe.Ingredients
                         .Where(item => item.Name.StartsWith("Nguyên liệu chính", StringComparison.OrdinalIgnoreCase)))
            {
                ingredient.Name = "Nguyên liệu chính";
            }

            foreach (var image in recipe.Images.Where(item =>
                         item.AltText != null &&
                         (item.AltText.Contains("mẫu", StringComparison.OrdinalIgnoreCase) ||
                          item.AltText.Contains("số", StringComparison.OrdinalIgnoreCase))))
            {
                image.AltText = $"Hình ảnh {baseName}";
            }

            if (recipe.Ingredients.Count < 10)
            {
                var startOrder = recipe.Ingredients.Count;
                var selected = IngredientNames
                    .Skip((index * 7) % IngredientNames.Length)
                    .Concat(IngredientNames.Take((index * 7) % IngredientNames.Length))
                    .Take(10 - startOrder);

                var order = startOrder;
                foreach (var name in selected)
                {
                    db.RecipeIngredients.Add(new RecipeIngredient
                    {
                        RecipeId = recipe.Id,
                        Name = name,
                        Quantity = RandomQuantity(),
                        Unit = Units[(index + order) % Units.Length],
                        Notes = "Sơ chế sạch và điều chỉnh lượng theo khẩu vị.",
                        SortOrder = order++
                    });
                }
            }

            if (recipe.Steps.Count < 3)
            {
                var startOrder = recipe.Steps.Count + 1;
                for (var stepNumber = startOrder; stepNumber <= 3; stepNumber++)
                {
                    db.RecipeSteps.Add(CreateNgocPhuocStep(recipe.Id, stepNumber, index));
                }
            }

            if (!recipe.Images.Any(item => item.IsPrimary && !item.IsDeleted))
            {
                var primary = recipe.Images
                    .FirstOrDefault(item => !item.IsDeleted && item.OriginalUrl != string.Empty);
                if (primary is null)
                {
                    db.RecipeImages.Add(new RecipeImage
                    {
                        RecipeId = recipe.Id,
                        OriginalUrl =
                            $"https://images.unsplash.com/photo-1547592180-85f173990554?w=800&seed=ngocphuoc-{index}",
                        MediumUrl =
                            $"https://images.unsplash.com/photo-1547592180-85f173990554?w=800&seed=ngocphuoc-{index}",
                        ThumbnailUrl =
                            $"https://images.unsplash.com/photo-1547592180-85f173990554?w=300&seed=ngocphuoc-{index}",
                        AltText = $"Hình ảnh {baseName}",
                        IsPrimary = true,
                        OrderIndex = 0
                    });
                }
                else
                {
                    primary.IsPrimary = true;
                }
            }

            if (recipe.Nutrition.Calories is null || recipe.Nutrition.Protein is null)
            {
                recipe.Nutrition = new RecipeNutrition
                {
                    Source = NutritionSource.Manual,
                    Calories = 250 + (index % 500),
                    Protein = 10 + (index % 35),
                    Carbohydrates = 20 + (index % 70),
                    Fat = 5 + (index % 30),
                    Fiber = 2 + (index % 10),
                    Sodium = 200 + (index % 900)
                };
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Recipe CreateNgocPhuocRecipe(
        int index,
        string baseName,
        Guid authorId,
        Guid categoryId)
    {
        var recipe = new Recipe
        {
            AuthorId = authorId,
            CategoryId = categoryId,
            Title = baseName,
            Slug = $"ngocphuoc-mau-{index:000}",
            Description =
                $"Công thức {baseName.ToLowerInvariant()} dành cho thực hành và kiểm thử dữ liệu.",
            Content =
                $"Chuẩn bị nguyên liệu đầy đủ, sơ chế sạch, chế biến {baseName.ToLowerInvariant()} theo từng bước và điều chỉnh gia vị trước khi hoàn thiện.",
            PrepTimeMinutes = 10 + (index % 30),
            CookTimeMinutes = 20 + (index % 60),
            Servings = 2 + (index % 6),
            Difficulty = (DifficultyLevel)(index % 3 + 1),
            Status = RecipeStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-(index % 365)),
            Nutrition = new RecipeNutrition
            {
                Source = NutritionSource.Manual,
                Calories = 250 + (index % 500),
                Protein = 10 + (index % 35),
                Carbohydrates = 20 + (index % 70),
                Fat = 5 + (index % 30),
                Fiber = 2 + (index % 10),
                Sodium = 200 + (index % 900)
            }
        };

        for (var order = 0; order < 10; order++)
        {
            recipe.Ingredients.Add(new RecipeIngredient
            {
                RecipeId = recipe.Id,
                Name = IngredientNames[(index * 7 + order) % IngredientNames.Length],
                Quantity = RandomQuantity(),
                Unit = Units[(index + order) % Units.Length],
                Notes = order == 0 ? "Nguyên liệu chính, sơ chế sạch trước khi nấu." : null,
                SortOrder = order
            });
        }

        for (var stepNumber = 1; stepNumber <= 3; stepNumber++)
        {
            recipe.Steps.Add(CreateNgocPhuocStep(recipe.Id, stepNumber, index));
        }

        recipe.Images.Add(new RecipeImage
        {
            RecipeId = recipe.Id,
            OriginalUrl =
                $"https://images.unsplash.com/photo-1547592180-85f173990554?w=800&seed=ngocphuoc-{index}",
            MediumUrl =
                $"https://images.unsplash.com/photo-1547592180-85f173990554?w=800&seed=ngocphuoc-{index}",
            ThumbnailUrl =
                $"https://images.unsplash.com/photo-1547592180-85f173990554?w=300&seed=ngocphuoc-{index}",
            AltText = $"Hình ảnh {baseName}",
            IsPrimary = true,
            OrderIndex = 0
        });

        return recipe;
    }

    private static RecipeStep CreateNgocPhuocStep(Guid recipeId, int stepNumber, int index) =>
        new()
        {
            RecipeId = recipeId,
            StepNumber = stepNumber,
            Title = stepNumber switch
            {
                1 => "Chuẩn bị",
                2 => "Chế biến",
                _ => "Hoàn thiện và thưởng thức"
            },
            Description = stepNumber switch
            {
                1 => "Sơ chế nguyên liệu, căn chỉnh dụng cụ và chuẩn bị gia vị.",
                2 => $"Nấu trong lửa vừa, đảo đều khoảng {8 + index % 12} phút và kiểm tra độ chín.",
                _ => "Bày ra đĩa, trang trí đẹp mắt và thưởng thức khi còn ấm."
            },
            TimerMinutes = stepNumber == 2 ? 8 + index % 12 : null
        };

    private static Category CreateCategory(
        string slug,
        string name,
        string description) =>
        Category.Create(
            name: name,
            slug: slug,
            description: description);

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

        var title = baseName;
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
