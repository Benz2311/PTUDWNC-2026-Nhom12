using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class RecipeSeedData
{
    // Recipe.AuthorId hiện đang là string
    private const string DemoUserId =
        "11111111-1111-1111-1111-111111111111";

    // ApplicationUser.Id hiện đang là Guid
    private static readonly Guid DemoUserGuid =
        Guid.Parse(DemoUserId);

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
        CancellationToken cancellationToken = default)
    {
        // ApplicationUser.Id là Guid
        var user = await db.Users
            .SingleOrDefaultAsync(
                item => item.Id == DemoUserGuid,
                cancellationToken);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = DemoUserGuid,
                UserName = "culinary-demo",
                Email = "demo@culinaryblog.local",
                PasswordHash = "seeded-demo-password"
            };

            db.Users.Add(user);

            await db.SaveChangesAsync(cancellationToken);
        }

        var categories = new[]
        {
            CreateCategory(
                "cat-001",
                "Món Việt",
                "Công thức đậm đà hương vị Việt."),

            CreateCategory(
                "cat-002",
                "Món Á",
                "Những món ăn châu Á dễ thực hiện."),

            CreateCategory(
                "cat-003",
                "Món Âu",
                "Công thức phương Tây cho căn bếp gia đình."),

            CreateCategory(
                "cat-004",
                "Món Chay",
                "Món chay cân bằng và giàu dinh dưỡng."),

            CreateCategory(
                "cat-005",
                "Tráng Miệng",
                "Các món ngọt cho ngày thêm vui.")
        };

        foreach (var category in categories)
        {
            var exists = await db.Categories
                .AnyAsync(
                    item => item.Slug == category.Slug,
                    cancellationToken);

            if (!exists)
            {
                db.Categories.Add(category);
            }
        }



        await db.SaveChangesAsync(cancellationToken);

        var categorySlugs = categories
            .Select(category => category.Slug)
            .ToArray();

        var categoryBySlug = await db.Categories
            .Where(item => categorySlugs.Contains(item.Slug))
            .ToDictionaryAsync(
                item => item.Slug,
                cancellationToken);

        // Đảm bảo có ít nhất 100 Recipe.
        var existingRecipeCount =
            await db.Recipes.CountAsync(cancellationToken);

        var recipesToCreate =
            Math.Max(0, 100 - existingRecipeCount);

        for (var i = 0; i < recipesToCreate; i++)
        {
            var recipeNumber =
                existingRecipeCount + i + 1;

            var category =
                categories[Random.Next(categories.Length)];

            var categoryEntity =
                categoryBySlug[category.Slug];

            var recipe = CreateRandomRecipe(
                recipeNumber,
                categoryEntity);

            db.Recipes.Add(recipe);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Đảm bảo mỗi Recipe có ít nhất 10 Ingredient.
        var allRecipes = await db.Recipes
            .Include(recipe => recipe.Ingredients)
            .ToListAsync(cancellationToken);

        foreach (var recipe in allRecipes)
        {
            var missingIngredients =
                10 - recipe.Ingredients.Count;

            if (missingIngredients <= 0)
            {
                continue;
            }

            var selectedIngredients = IngredientNames
                .OrderBy(_ => Random.Next())
                .Take(missingIngredients)
                .ToArray();

            var startOrder =
                recipe.Ingredients.Count;

            for (var i = 0;
                 i < selectedIngredients.Length;
                 i++)
            {
                db.RecipeIngredients.Add(
                    new RecipeIngredient
                    {
                        RecipeId = recipe.Id,
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
        string description)
    {
        return Category.Create(
            name: name,
            slug: slug,
            description: description,
            imageUrl: null,
            orderIndex: 0);
    }

    private static Recipe CreateRandomRecipe(
        int recipeNumber,
        Category category)
    {
        var baseName =
            RecipeBases[Random.Next(RecipeBases.Length)];

        var title =
            $"{baseName} phiên bản {recipeNumber:000}";

        var slug =
            $"recipe-{recipeNumber:000}";

        var prepTime =
            Random.Next(5, 46);

        var cookTime =
            Random.Next(10, 121);

        var servings =
            Random.Next(1, 7);

        var ingredients =
            CreateRandomIngredients();

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
            // Recipe.AuthorId hiện đang là string
            AuthorId = DemoUserGuid,

            CategoryId = category.Id,
            Title = title,
            Slug = slug,

            Description =
                $"Công thức {baseName.ToLowerInvariant()} được sinh tự động " +
                $"với dữ liệu nguyên liệu và dinh dưỡng ngẫu nhiên.",

            Content =
                $"Chuẩn bị nguyên liệu, sơ chế và nấu " +
                $"{baseName.ToLowerInvariant()} trong khoảng {cookTime} phút. " +
                $"Nêm nếm phù hợp với khẩu vị.",

            PrepTimeMinutes = prepTime,
            CookTimeMinutes = cookTime,
            Servings = servings,
            Difficulty = RandomDifficulty(),
            Status = RecipeStatus.Published,

            PublishedAt =
                DateTime.UtcNow.AddDays(
                    -Random.Next(0, 365)),

            Nutrition = nutrition,
            Ingredients = ingredients
        };
    }

    private static List<RecipeIngredient> CreateRandomIngredients()
    {
        var count =
            Random.Next(10, 16);

        return IngredientNames
            .OrderBy(_ => Random.Next())
            .Take(count)
            .Select((name, index) =>
                new RecipeIngredient
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
        var value =
            Random.Next(1, 501);

        return Math.Round(
            value / 10m,
            1);
    }

    private static decimal RandomDecimal(
        decimal min,
        decimal max)
    {
        var value =
            (decimal)Random.NextDouble()
            * (max - min)
            + min;

        return Math.Round(
            value,
            2);
    }

    private static DifficultyLevel RandomDifficulty()
    {
        return (DifficultyLevel)Random.Next(1, 5);
    }

    public record SeedDish(
        string Title,
        string Slug,
        string CategorySlug,
        int PrepTimeMinutes,
        int CookTimeMinutes,
        int Servings,
        DifficultyLevel Difficulty,
        string Description,
        string Content,
        string? CoverImageUrl,
        (decimal? Cal, decimal? Pro, decimal? Carb, decimal? Fat, decimal? Fiber, decimal? Sod) Nutrition,
        (string Name, decimal? Qty, string? Unit, string? Notes)[] Ingredients,
        (int Step, string Title, string Desc, int? Timer)[] Steps,
        (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[] Images
    );

    public static async Task SeedRealVietnameseRecipesAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var author = await db.Users.FirstOrDefaultAsync(u => u.UserName == "culinary-demo" || u.UserName == "demo_user" || u.UserName == "admin", cancellationToken)
            ?? await db.Users.FirstOrDefaultAsync(cancellationToken);

        if (author == null)
        {
            author = new ApplicationUser
            {
                Id = DemoUserGuid,
                UserName = "culinary-demo",
                Email = "demo@culinaryblog.local",
                PasswordHash = "seeded-demo-password",
                DisplayName = "Bếp Trưởng Việt Nam"
            };
            db.Users.Add(author);
            await db.SaveChangesAsync(cancellationToken);
        }

        var defaultCategory = await db.Categories.FirstOrDefaultAsync(cancellationToken);

        foreach (var dish in RealVietnameseDishes)
        {
            var exists = await db.Recipes.AnyAsync(r => r.Slug == dish.Slug, cancellationToken);
            if (exists)
            {
                continue;
            }

            var category = await db.Categories.FirstOrDefaultAsync(c => c.Slug == dish.CategorySlug, cancellationToken);

            if (category == null)
            {
                var categoryName = dish.CategorySlug switch
                {
                    "bun-mi-pho" => "Bún, Mì & Phở",
                    "mon-chinh" => "Món Chính",
                    "mon-canh" => "Món Canh",
                    "mon-nuong-bbq" => "Món Nướng & BBQ",
                    "mon-kho-rim" => "Món Kho & Rim",
                    "mon-xao" => "Món Xào",
                    "mon-chien-ran" => "Món Chiên & Rán",
                    "mon-hap-luoc" => "Món Hấp & Luộc",
                    "mon-lau-nuoc-dung" => "Món Lẩu & Nước Dùng",
                    "mon-khai-vi" => "Món Khai Vị",
                    "mon-trang-mieng" => "Món Tráng Miệng",
                    "an-vat" => "Ăn Vặt",
                    "goi-salad" => "Gỏi & Salad",
                    "do-uong" => "Đồ Uống",
                    _ => "Món Việt"
                };

                category = Category.Create(
                    name: categoryName,
                    slug: dish.CategorySlug,
                    description: $"Công thức {categoryName} thơm ngon đậm đà.",
                    imageUrl: null,
                    orderIndex: 10);
                db.Categories.Add(category);
                await db.SaveChangesAsync(cancellationToken);
            }

            var recipeId = Guid.NewGuid();
            var recipe = new Recipe
            {
                Id = recipeId,
                AuthorId = author.Id,
                CategoryId = category.Id,
                Title = dish.Title,
                Slug = dish.Slug,
                Description = dish.Description,
                Content = dish.Content,
                PrepTimeMinutes = dish.PrepTimeMinutes,
                CookTimeMinutes = dish.CookTimeMinutes,
                Servings = dish.Servings,
                Difficulty = dish.Difficulty,
                Status = RecipeStatus.Published,
                PublishedAt = DateTime.UtcNow,
                CoverImageUrl = dish.CoverImageUrl,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                Nutrition = new RecipeNutrition
                {
                    Calories = dish.Nutrition.Cal,
                    Protein = dish.Nutrition.Pro,
                    Carbohydrates = dish.Nutrition.Carb,
                    Fat = dish.Nutrition.Fat,
                    Fiber = dish.Nutrition.Fiber,
                    Sodium = dish.Nutrition.Sod
                }
            };

            db.Recipes.Add(recipe);

            var ingredients = dish.Ingredients.Select((ing, idx) => new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = ing.Name,
                Quantity = ing.Qty,
                Unit = ing.Unit,
                Notes = ing.Notes,
                SortOrder = idx,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }).ToList();

            var steps = dish.Steps.Select(st => new RecipeStep
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                StepNumber = st.Step,
                Title = st.Title,
                Description = st.Desc,
                TimerMinutes = st.Timer,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }).ToList();

            var images = dish.Images.Select((img, idx) => new RecipeImage
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                OriginalUrl = img.Orig,
                MediumUrl = img.Med,
                ThumbnailUrl = img.Thumb,
                AltText = img.Alt,
                IsPrimary = img.Primary,
                SortOrder = idx,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }).ToList();

            db.RecipeIngredients.AddRange(ingredients);
            db.RecipeSteps.AddRange(steps);
            db.RecipeImages.AddRange(images);

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public static readonly SeedDish[] RealVietnameseDishes = new SeedDish[]
    {
        new SeedDish(
            Title: "Phở Bò Tái Nạm Hà Nội",
            Slug: "pho-bo-tai-nam-ha-noi",
            CategorySlug: "bun-mi-pho",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 180,
            Servings: 4,
            Difficulty: DifficultyLevel.Easy,
            Description: "Phở bò Hà Nội truyền thống với nước dùng ninh từ xương ống bò 10 tiếng, thơm nức quế, hồi, gừng nướng, bánh phở mềm mướt và thịt bò tái nạm ngọt đậm đà.",
            Content: "Phở bò là linh hồn của ẩm thực Hà Thành. Nước dùng phở chuẩn vị phải trong, ngọt sâu từ tủy xương bò ninh kỹ, thoang thoảng hương quế, hoa hồi, thảo quả và hành gừng nướng. Khi thưởng thức, bánh phở mềm mướt được chần nóng, phủ lên từng lát thịt bò nạm luộc mềm và thịt thăn bò tái mỏng dính, chan ngập nước dùng sôi sùng sục, rắc hành hoa, đầu hành chần và ngò gai.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/729700c4-f266-40e2-a504-1296cfa0ca89/images/cf27ba27-7d27-4c3b-8070-011b818b0f4b.jpg",
            Nutrition: (450.00m, 28.00m, 55.00m, 14.00m, 3.00m, 920.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Xương ống bò", 1.500m, "kg", "Chặt khúc, ngâm nước muối loãng"),
                ("Thịt nạm bò", 400.000m, "g", "Rửa sạch, cuộn chỉ chặt"),
                ("Thịt thăn bò tươi", 300.000m, "g", "Thái lát thật mỏng để ăn tái"),
                ("Bánh phở tươi", 800.000m, "g", "Trụng qua nước sôi trước khi dùng"),
                ("Hành tây", 2.000m, "củ", "Nướng xém vỏ thơm"),
                ("Gừng tươi", 1.000m, "củ lớn", "Nướng đập dập"),
                ("Hoa hồi, quế chi, thảo quả", 1.000m, "gói gia vị", "Rang thơm cho vào túi lọc"),
                ("Hành hoa, ngò gai, chanh ớt", 1.000m, "bó", "Rửa sạch thái nhỏ"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Sơ chế và hầm xương bò", "Rửa sạch xương ống bò, chần qua nước sôi 5 phút rồi rửa lại sạch bọt bẩn. Cho xương vào nồi cùng 4 lít nước, ninh lửa nhỏ liu riu trong ít nhất 3 giờ, thường xuyên hớt bọt để nước dùng thật trong.", 120),
                (2, "Nướng gia vị và tạo hương nước dùng", "Nướng thơm hành tây, gừng tươi trên lửa than, cạo sạch muội đen rồi đập dập. Rang thơm hoa hồi, quế, thảo quả rồi cho vào túi vải buộc chặt thả vào nồi nước dùng ninh cùng thịt nạm bò.", 45),
                (3, "Nêm nếm gia vị và luộc nạm bò", "Vớt thịt nạm bò ra ngâm nước đá lạnh cho giòn rồi thái lát mỏng. Nêm nước mắm ngon hảo hạng, muối hạt và chút đường phèn cho nước dùng có vị ngọt thanh tự nhiên vừa miệng.", 15),
                (4, "Hoàn thiện bát phở và thưởng thức", "Chần bánh phở qua nước sôi xếp vào tô. Xếp thịt nạm luộc và thịt bò tái lên trên, rắc hành hoa, ngò gai thái nhỏ. Chan nước dùng đang sôi sùng sục ngập mặt phở. Dùng kèm tương ớt Bắc, chanh tươi và giấm tỏi.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/729700c4-f266-40e2-a504-1296cfa0ca89/images/cf27ba27-7d27-4c3b-8070-011b818b0f4b.jpg", "http://localhost:9000/culinaryblog/recipes/729700c4-f266-40e2-a504-1296cfa0ca89/images/cf27ba27-7d27-4c3b-8070-011b818b0f4b_medium.jpg", "http://localhost:9000/culinaryblog/recipes/729700c4-f266-40e2-a504-1296cfa0ca89/images/cf27ba27-7d27-4c3b-8070-011b818b0f4b_thumb.jpg", "Phở Bò Tái Nạm Hà Nội", true),
            }
        ),
        new SeedDish(
            Title: "Bún Bò Huế Cố Đô",
            Slug: "bun-bo-hue-co-do",
            CategorySlug: "bun-mi-pho",
            PrepTimeMinutes: 40,
            CookTimeMinutes: 120,
            Servings: 4,
            Difficulty: DifficultyLevel.Medium,
            Description: "Hương vị đậm đà xứ Huế với sợi bún to, nước dùng thơm lừng sả cây, đậm đà mắm ruốc, ăn cùng bắp bò hoa, chả cua, tiết luộc và ớt sa tế cay nồng.",
            Content: "Bún bò Huế mang đậm nét văn hóa cung đình và dân gian xứ Huế. Nước dùng được ninh từ xương bò và giò heo, hòa quyện cùng mùi thơm nồng nàn của sả đập dập và vị đậm đà rất riêng của mắm ruốc Huế đã được lọc trong. Bát bún đỏ au màu dầu hạt điều, phủ đầy thịt bắp bò mềm ngậy, chả cua thơm bùi và đĩa rau sống búp chuối thái mỏng.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/862ff01c-9050-46cc-859d-f63ea95c038c/images/93098bc8-d233-459f-aea5-1e7f513dd6f3.jpg",
            Nutrition: (520.00m, 32.00m, 60.00m, 18.00m, 4.00m, 1050.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Bắp bò hoa", 600.000m, "g", "Thịt bắp bò tươi hoa săn chắc"),
                ("Giò heo (móng giò)", 500.000m, "g", "Chặt khúc vừa ăn, làm sạch"),
                ("Bún sợi to Huế", 800.000m, "g", "Trụng nóng"),
                ("Mắm ruốc Huế nguyên chất", 3.000m, "muỗng canh", "Khuấy nước lạnh lóng lấy nước trong"),
                ("Sả cây", 8.000m, "nhánh", "Đập dập bó tròn"),
                ("Huyết heo luộc", 200.000m, "g", "Cắt miếng vuông vừa ăn"),
                ("Chả cua hoặc chả bò Huế", 200.000m, "g", "Hấp chín"),
                ("Bắp chuối bào, rau thơm, giá đỗ", 300.000m, "g", "Rửa sạch để ráo"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Ninh giò heo và bắp bò", "Trần giò heo và bắp bò qua nước sôi có gừng đập dập. Rửa sạch lại rồi cho vào nồi cùng 3.5 lít nước và sả cây đập dập, ninh đến khi thịt chín mềm vừa tới thì vớt bắp bò ngâm nước đá rồi thái lát.", 60),
                (2, "Lọc mắm ruốc và nêm nước dùng", "Hòa tan mắm ruốc Huế với 1 chén nước lạnh, để lắng cặn rồi chắt lấy phần nước trong đổ vào nồi nước dùng. Phi thơm hành tím băm với ớt bột và dầu màu điều rồi trút vào nồi tạo màu đỏ cam óng ả đặc trưng.", 30),
                (3, "Nêm gia vị hoàn thiện", "Thêm huyết heo, chả cua vào nồi nước dùng. Nêm hạt nêm, muối và đường phèn sao cho vị ngọt thanh cân bằng vị mặn mà của ruốc sả.", 20),
                (4, "Trình bày bát bún bò Huế", "Cho bún sợi to vào tô, xếp bắp bò thái lát, khoanh giò heo, chả cua, miếng huyết. Chan nước dùng sôi ngập bát, rắc hành tây thái mỏng và hành lá ngò gai. Thưởng thức cùng ớt sa tế cay xé lưỡi và rau bắp chuối.", 10),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/862ff01c-9050-46cc-859d-f63ea95c038c/images/93098bc8-d233-459f-aea5-1e7f513dd6f3.jpg", "http://localhost:9000/culinaryblog/recipes/862ff01c-9050-46cc-859d-f63ea95c038c/images/93098bc8-d233-459f-aea5-1e7f513dd6f3_medium.jpg", "http://localhost:9000/culinaryblog/recipes/862ff01c-9050-46cc-859d-f63ea95c038c/images/93098bc8-d233-459f-aea5-1e7f513dd6f3_thumb.jpg", "Bún Bò Huế Cố Đô", true),
            }
        ),
        new SeedDish(
            Title: "Bún Chả Hà Nội Nướng Than Hoa",
            Slug: "bun-cha-ha-noi-nuong-than-hoa",
            CategorySlug: "mon-nuong-bbq",
            PrepTimeMinutes: 35,
            CookTimeMinutes: 30,
            Servings: 3,
            Difficulty: DifficultyLevel.Easy,
            Description: "Chả viên và chả miếng ba chỉ nướng than hoa thơm phức, ăn kèm bún lá tươi, rau sống kinh giới tía tô và bát nước mắm chua ngọt ấm nóng kèm đu đủ giòn.",
            Content: "Bún chả Hà Nội nức tiếng gần xa bởi hương vị đậm đà của thịt ba chỉ và thịt nạc vai băm ướp hành tím, nước mắm, tiêu sọ và nước hàng dừa rồi kẹp vỉ nướng trên than hoa đỏ rực. Từng miếng chả xém vàng óng, mỡ thơm ngậy chấm vào bát nước chấm chua ngọt ấm nồng đu đủ cà rốt giòn sần sật.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/3ae12d33-c58a-439a-bd40-141028e387c3/images/ca68c7ce-64b5-46af-967a-325b64904e76.jpg",
            Nutrition: (480.00m, 26.00m, 52.00m, 20.00m, 3.00m, 850.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Thịt ba chỉ heo", 400.000m, "g", "Thái lát mỏng vừa ăn để làm chả miếng"),
                ("Thịt nạc vai xay", 400.000m, "g", "Xay lẫn chút mỡ để chả viên không bị khô"),
                ("Hành khô, tỏi", 50.000m, "g", "Băm nhuyễn vắt lấy nước cốt ướp"),
                ("Nước mắm ngon, dầu hào, mật ong", 3.000m, "muỗng canh", "Gia vị ướp"),
                ("Đu đủ xanh, cà rốt", 200.000m, "g", "Thái mỏng ngâm giấm đường giòn"),
                ("Bún tươi", 600.000m, "g", "Bún lá hoặc bún rối tươi"),
                ("Rau sống: xà lách, tía tô, kinh giới", 300.000m, "g", "Rửa sạch"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Ướp chả miếng và nặn chả viên", "Ướp thịt ba chỉ và thịt băm với nước mắm, đường nâu, dầu hào, tiêu và nước cốt hành tỏi trong ít nhất 30 phút. Nặn thịt băm thành từng viên tròn dẹt vừa ăn.", 30),
                (2, "Làm đồ chua đu đủ cà rốt", "Đu đủ xanh và cà rốt thái lát mỏng, bóp muối rửa sạch rồi ngâm cùng giấm gạo, đường và tỏi ớt băm để tạo độ chua ngọt giòn rụm ăn kèm.", 15),
                (3, "Nướng chả trên than hoa", "Kẹp chả miếng và chả viên vào vỉ sắt. Nướng trên bếp than hoa đỏ rực, lật đều tay và phết chút mỡ cho chả không khô, đến khi xém cạnh vàng nâu thơm nức mũi.", 20),
                (4, "Pha nước mắm và thưởng thức", "Pha nước mắm với nước ấm, đường, chanh và tỏi ớt theo tỉ lệ vàng chua ngọt thanh nhẹ. Múc chả nướng và đồ chua vào bát nước mắm còn ấm, gắp bún và rau sống chấm cùng.", 10),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/3ae12d33-c58a-439a-bd40-141028e387c3/images/ca68c7ce-64b5-46af-967a-325b64904e76.jpg", "http://localhost:9000/culinaryblog/recipes/3ae12d33-c58a-439a-bd40-141028e387c3/images/ca68c7ce-64b5-46af-967a-325b64904e76_medium.jpg", "http://localhost:9000/culinaryblog/recipes/3ae12d33-c58a-439a-bd40-141028e387c3/images/ca68c7ce-64b5-46af-967a-325b64904e76_thumb.jpg", "Bún Chả Hà Nội Nướng Than Hoa", true),
            }
        ),
        new SeedDish(
            Title: "Cơm Tấm Sườn Nướng Mỡ Hành",
            Slug: "com-tam-suon-nuong-mo-hanh",
            CategorySlug: "mon-chinh",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 40,
            Servings: 2,
            Difficulty: DifficultyLevel.Easy,
            Description: "Cơm tấm dẻo thơm hạt gạo tấm, miếng sườn cốt lết nướng mật ong óng ả, chả trứng hấp thơm bùi, bì heo dai giòn, chan mỡ hành và nước mắm kẹo tỏi ớt.",
            Content: "Cơm tấm là biểu tượng ẩm thực đường phố Sài Gòn. Hạt gạo tấm vỡ nấu chín tới tơi xốp, thơm mùi lá dứa. Miếng sườn cốt lết dày dặn ướp sả, mật ong, dầu hào nướng than hồng thơm nức mũi. Dĩa cơm tròn vị khi có thêm chả trứng béo mềm, bì heo trộn thính thơm giòn và chén nước mắm chua ngọt keo đặc.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/50b6c842-a2b4-4d7b-bdae-3f36921eed35/images/462cbfe6-752c-4fc9-aa96-68429af15ddc.jpg",
            Nutrition: (650.00m, 35.00m, 75.00m, 24.00m, 2.00m, 980.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Gạo tấm thơm", 300.000m, "g", "Vo sạch, ngâm 15 phút"),
                ("Sườn cốt lết heo", 2.000m, "miếng lớn", "Dần mềm thớ thịt"),
                ("Mật ong, sả băm, tỏi băm", 2.000m, "muỗng canh", "Ướp sườn"),
                ("Hành lá", 50.000m, "g", "Thái nhỏ làm mỡ hành"),
                ("Dưa leo, cà chua, đồ chua", 100.000m, "g", "Ăn kèm"),
                ("Nước mắm kẹo tỏi ớt", 1.000m, "chén", "Nấu nước mắm đường sệt"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Ướp sườn cốt lết", "Dùng búa dần mềm thịt sườn. Ướp sườn với mật ong, nước mắm, sả băm, tỏi ớt, chút sữa đặc và dầu ăn trong 40 phút cho thấm sâu.", 40),
                (2, "Nấu cơm tấm", "Cho gạo tấm vào nồi cơm điện với lượng nước vừa xăm xắp mặt gạo, thêm nhánh lá dứa thơm rồi nấu chín tới hạt cơm tơi xốp.", 30),
                (3, "Nướng sườn và làm mỡ hành", "Nướng sườn trên than hoa hoặc nồi chiên không dầu ở 180 độ C cho đến khi hai mặt sườn chín vàng óng ả. Đun sôi dầu ăn rồi rót vào bát hành lá thái nhỏ làm mỡ hành thơm phức.", 20),
                (4, "Trình bày dĩa cơm tấm", "Xới cơm tấm ra đĩa, đặt miếng sườn nướng nóng hổi lên trên, rưới mỡ hành óng ánh, dưa leo, cà chua lát và chén nước mắm kẹo chua cay đậm đà.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/50b6c842-a2b4-4d7b-bdae-3f36921eed35/images/462cbfe6-752c-4fc9-aa96-68429af15ddc.jpg", "http://localhost:9000/culinaryblog/recipes/50b6c842-a2b4-4d7b-bdae-3f36921eed35/images/462cbfe6-752c-4fc9-aa96-68429af15ddc_medium.jpg", "http://localhost:9000/culinaryblog/recipes/50b6c842-a2b4-4d7b-bdae-3f36921eed35/images/462cbfe6-752c-4fc9-aa96-68429af15ddc_thumb.jpg", "Cơm Tấm Sườn Nướng Mỡ Hành", true),
            }
        ),
        new SeedDish(
            Title: "Bánh Mì Thịt Nguội Pate Giòn Rụm",
            Slug: "banh-mi-thit-nguoi-pate-gion-rum",
            CategorySlug: "an-vat",
            PrepTimeMinutes: 15,
            CookTimeMinutes: 10,
            Servings: 2,
            Difficulty: DifficultyLevel.Easy,
            Description: "Ổ bánh mì giòn tan rụm phết pate gan heo béo ngậy, sốt bơ trứng vàng ươm, kẹp giò lụa, thịt xá xíu, dưa leo, đồ chua cà rốt củ cải và ngò rí cay the ớt hiểm.",
            Content: "Bánh mì Việt Nam vang danh thế giới bởi sự kết hợp hoàn hảo giữa lớp vỏ bánh mì nướng giòn tan xốp ruột cùng lớp nhân đậm đà bản sắc. Pate gan béo ngậy được phết đều, quyện cùng sốt bơ trứng gà thơm lừng, thịt chả lụa dai ngọt và đồ chua thanh mát chống ngấy.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/3cd6094b-04ac-4f82-aca7-e46ac0479a68/images/c6fc0000-fe5d-4217-8e98-a7100d083955.jpg",
            Nutrition: (410.00m, 18.00m, 48.00m, 16.00m, 2.00m, 780.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Bánh mì ổ giòn", 2.000m, "ổ", "Nướng lại cho giòn nóng"),
                ("Pate gan heo hảo hạng", 60.000m, "g", "Pate béo mềm thơm mùi hạt tiêu"),
                ("Chả lụa / Giò thủ thái lát", 100.000m, "g", "Thái mỏng"),
                ("Thịt xá xíu hoặc thịt nguội", 80.000m, "g", "Thái lát mỏng"),
                ("Đồ chua củ cải cà rốt", 50.000m, "g", "Vắt ráo nước"),
                ("Dưa leo, ngò rí, ớt sừng", 1.000m, "phần", "Rửa sạch thái sợi"),
                ("Bơ trứng gà tươi", 30.000m, "g", "Đánh bông thơm ngậy"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Làm nóng vỏ bánh mì", "Cho bánh mì vào lò nướng hoặc áp chảo nhanh 2-3 phút cho vỏ bánh đạt độ giòn rụm hoàn hảo.", 5),
                (2, "Phết pate và bơ trứng", "Dùng dao rạch dọc thân bánh, phết một lớp bơ trứng gà vàng ươm rồi trét đều pate gan béo ngậy vào hai bên thành bánh.", 3),
                (3, "Xếp nhân thịt và rau ghém", "Lần lượt xếp chả lụa, thịt nguội xá xíu vào giữa bánh. Thêm dưa leo thái dọc, đồ chua cà rốt củ cải giòn ngọt, vài cọng ngò rí và lát ớt hiểm tươi.", 4),
                (4, "Rưới sốt và thưởng thức", "Rưới nhẹ vài giọt nước tương Maggi và tiêu xay thơm lừng. Cắn miếng bánh mì giòn tan rụm tận hưởng bản hòa tấu hương vị tuyệt hảo.", 2),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/3cd6094b-04ac-4f82-aca7-e46ac0479a68/images/c6fc0000-fe5d-4217-8e98-a7100d083955.jpg", "http://localhost:9000/culinaryblog/recipes/3cd6094b-04ac-4f82-aca7-e46ac0479a68/images/c6fc0000-fe5d-4217-8e98-a7100d083955_medium.jpg", "http://localhost:9000/culinaryblog/recipes/3cd6094b-04ac-4f82-aca7-e46ac0479a68/images/c6fc0000-fe5d-4217-8e98-a7100d083955_thumb.jpg", "Bánh Mì Thịt Nguội Pate Giòn Rụm", true),
            }
        ),
        new SeedDish(
            Title: "Bánh Xèo Tôm Thịt Giòn Rụm",
            Slug: "banh-xeo-tom-thit-gion-rum",
            CategorySlug: "mon-chien-ran",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 30,
            Servings: 4,
            Difficulty: DifficultyLevel.Easy,
            Description: "Vỏ bánh mỏng giòn rụm từ bột gạo pha nước cốt dừa và bột nghệ, nhân tôm sú tươi, thịt ba rọi, giá đỗ, ăn cuốn cùng cải xanh, xà lách và rau rừng Tây Nam Bộ.",
            Content: "Bánh xèo miền Tây được tráng trên chảo gang lớn rực lửa, tiếng 'xèo' vang lên giòn giã đặc trưng. Vỏ bánh màu vàng nghệ bắt mắt, thơm béo vị nước cốt dừa và hành lá cắt nhỏ. Nhân bánh đầy ắp tôm sông tươi rói, thịt ba chỉ xào săn và giá đỗ giòn ngọt.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/7fd97e37-00f2-46ab-aef3-e6d4cc7a68fd/images/b6968940-c2da-4ce8-b032-5bd64e8acbbd.jpg",
            Nutrition: (390.00m, 16.00m, 45.00m, 17.00m, 3.00m, 620.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Bột bánh xèo", 400.000m, "g", "Pha cùng nước cốt dừa và hành lá"),
                ("Tôm sú tươi", 300.000m, "g", "Lột vỏ chừa đuôi, xào sơ"),
                ("Thịt ba rọi heo", 250.000m, "g", "Thái mỏng xào chín"),
                ("Giá đỗ tươi", 200.000m, "g", "Rửa sạch để ráo"),
                ("Nước cốt dừa", 200.000m, "ml", "Tạo vị béo ngậy cho vỏ bánh"),
                ("Rau sống cải xanh, xà lách, rau diếp cá", 400.000m, "g", "Rửa sạch ăn kèm"),
                ("Nước mắm tỏi ớt chua ngọt", 1.000m, "bát", "Pha tỏi ớt chanh đường"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Pha bột bánh xèo", "Khuấy đều bột gạo, bột nghệ, nước cốt dừa, nước lọc và hành lá thái nhỏ. Để bột nghỉ 20 phút cho bột nở đều dẻo mịn.", 20),
                (2, "Xào nhân tôm thịt", "Phi thơm hành tím, xào săn thịt ba chỉ và tôm tươi, nêm chút hạt nêm tiêu xay vừa chín tới rồi múc ra đĩa riêng.", 10),
                (3, "Đổ bánh xèo giòn rụm", "Làm nóng chảo với chút dầu ăn. Múc một vá bột tráng mỏng đều khắp lòng chảo, rải tôm thịt và nắm giá đỗ lên trên. Đậy nắp chảo 3 phút cho chín, sau đó mở nắp chiên lửa vừa cho vành bánh giòn rụm vàng ươm rồi gập đôi bánh lại.", 8),
                (4, "Thưởng thức bánh xèo", "Gắp bánh xèo ra đĩa. Cuộn miếng bánh giòn tan vào lá cải bẹ xanh cùng các loại rau thơm, chấm ngập chén nước mắm chua ngọt tỏi ớt.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/7fd97e37-00f2-46ab-aef3-e6d4cc7a68fd/images/b6968940-c2da-4ce8-b032-5bd64e8acbbd.jpg", "http://localhost:9000/culinaryblog/recipes/7fd97e37-00f2-46ab-aef3-e6d4cc7a68fd/images/b6968940-c2da-4ce8-b032-5bd64e8acbbd_medium.jpg", "http://localhost:9000/culinaryblog/recipes/7fd97e37-00f2-46ab-aef3-e6d4cc7a68fd/images/b6968940-c2da-4ce8-b032-5bd64e8acbbd_thumb.jpg", "Bánh Xèo Tôm Thịt Giòn Rụm", true),
            }
        ),
        new SeedDish(
            Title: "Gỏi Cuốn Tôm Thịt Thanh Mát",
            Slug: "goi-cuon-tom-thit-thanh-mat",
            CategorySlug: "mon-khai-vi",
            PrepTimeMinutes: 25,
            CookTimeMinutes: 15,
            Servings: 3,
            Difficulty: DifficultyLevel.Easy,
            Description: "Món khai vị thanh nhẹ với tôm tươi luộc đỏ au, thịt ba chỉ thái lát mỏng, bún tươi, hẹ xanh cuộn khéo trong bánh tráng, chấm tương đen bơ đậu phộng béo bùi.",
            Content: "Gỏi cuốn là một trong những món ăn lành mạnh và tinh tế nhất của Việt Nam. Cuốn bánh tráng trong suốt để lộ màu đỏ tươi của tôm sú luộc, lát thịt ba rọi nạc mỡ đan xen cùng nhánh hẹ xanh mướt. Chấm cùng bát sốt tương bơ đậu phộng rang xay mịn béo ngậy.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/9faa2842-fdfd-4252-81cf-b932b06156b8/images/3cee5a62-152c-469e-b919-d8dc9e90ae43.jpg",
            Nutrition: (260.00m, 14.00m, 36.00m, 6.00m, 2.00m, 450.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Tôm sú tươi", 300.000m, "g", "Luộc chín, bóc vỏ chẻ đôi sống lưng"),
                ("Thịt ba chỉ heo", 300.000m, "g", "Luộc chín tới, thái lát mỏng"),
                ("Bánh tráng dẻo", 1.000m, "xấp", "Bánh tráng gạo dẻo"),
                ("Bún tươi", 300.000m, "g", "Để ráo"),
                ("Hẹ lá, xà lách, rau thơm các loại", 200.000m, "g", "Rửa sạch vẩy ráo"),
                ("Tương đen bơ đậu phộng", 1.000m, "chén", "Nấu tương ngọt bùi cùng bơ đậu phộng"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Luộc tôm và thịt ba chỉ", "Luộc thịt ba chỉ với chút muối và củ hành tím đập dập đến khi chín tới, ngâm nước lạnh rồi thái lát mỏng. Luộc tôm đỏ au, bóc vỏ và xẻ đôi dọc sống lưng.", 15),
                (2, "Chuẩn bị rau sống và bánh tráng", "Nhặt sạch các loại rau thơm, xà lách và lá hẹ. Thấm nhẹ chút nước làm ẩm bánh tráng cho mềm dẻo dễ cuốn.", 5),
                (3, "Cuộn gỏi cuốn", "Trải bánh tráng ra mặt phẳng, đặt xà lách, rau thơm, bún và thịt luộc. Gấp hai mép bánh tráng lại, xếp tôm đỏ úp mặt xuống cho đẹp mắt, đặt nhánh hẹ thò đầu ra ngoài rồi cuộn tròn chặt tay.", 8),
                (4, "Pha nước sốt tương đậu phộng", "Phi thơm tỏi băm, cho tương đen ngọt, bơ đậu phộng và chút nước dừa vào đun sánh mịn. Múc ra chén rắc đậu phộng rang đập dập và ớt băm, chấm cùng gỏi cuốn.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/9faa2842-fdfd-4252-81cf-b932b06156b8/images/3cee5a62-152c-469e-b919-d8dc9e90ae43.jpg", "http://localhost:9000/culinaryblog/recipes/9faa2842-fdfd-4252-81cf-b932b06156b8/images/3cee5a62-152c-469e-b919-d8dc9e90ae43_medium.jpg", "http://localhost:9000/culinaryblog/recipes/9faa2842-fdfd-4252-81cf-b932b06156b8/images/3cee5a62-152c-469e-b919-d8dc9e90ae43_thumb.jpg", "Gỏi Cuốn Tôm Thịt Thanh Mát", true),
            }
        ),
        new SeedDish(
            Title: "Chả Giò Hải Sản Giòn Tan",
            Slug: "cha-gio-hai-san-gion-tan",
            CategorySlug: "mon-chien-ran",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 20,
            Servings: 4,
            Difficulty: DifficultyLevel.Easy,
            Description: "Chả giò chiên vàng ươm giòn tan, nhân tôm tươi băm, mực tươi, khoai môn bùi bùi và mộc nhĩ giòn sần sật, ăn nóng kèm nước mắm chua ngọt và rau sống.",
            Content: "Chả giò là món ăn không thể thiếu trong các bữa tiệc và mâm cơm sum họp gia đình Việt. Lớp bánh tráng chiên giòn rụm bao bọc lấy phần nhân hải sản ngọt lịm từ tôm mực tươi kết hợp vị bùi thơm của khoai môn thái sợi và độ giòn sần sật của nấm mèo.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/97c16a7f-d584-4a34-bc68-f808adfa80aa/images/42a46840-5e02-445e-b41a-baf384f807ae.jpg",
            Nutrition: (340.00m, 15.00m, 28.00m, 19.00m, 2.00m, 580.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Tôm sú tươi băm nhỏ", 200.000m, "g", "Lột vỏ băm nhỏ vừa ăn"),
                ("Mực nang tươi", 150.000m, "g", "Thái hạt lựu nhỏ"),
                ("Thịt heo xay", 150.000m, "g", "Thịt nạc vai xay"),
                ("Khoai môn bào sợi", 100.000m, "g", "Bào sợi chiên sơ"),
                ("Nấm mèo (mộc nhĩ)", 30.000m, "g", "Ngâm nở thái chỉ"),
                ("Bánh tráng bò bía hoặc bánh tráng xốp", 1.000m, "gói", "Cuốn chả giò giòn"),
                ("Trứng gà", 1.000m, "quả", "Trộn vào nhân tạo độ kết dính"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Trộn nhân chả giò", "Trộn đều tôm, mực, thịt xay, khoai môn, mộc nhĩ, hành tím băm, tiêu xay và 1 quả trứng gà cho nhân quyện đều dẻo mịn.", 15),
                (2, "Cuốn chả giò", "Trải bánh tráng lên đĩa, múc một lượng nhân vừa phải đặt vào đầu bánh, gập hai bên mép và cuộn tròn đều tay vừa vặn để chả không bị bục khi chiên.", 15),
                (3, "Chiên chả giò giòn rụm", "Đun sôi dầu ngập chảo, thả từng cuốn chả giò vào chiên lửa vừa. Khi vỏ bánh se lại thì tăng lửa cho chả chín vàng đều giòn rụm rồi vớt ra giấy thấm dầu.", 15),
                (4, "Thưởng thức chả giò nóng hổi", "Xếp chả giò ra đĩa, ăn nóng cùng bún tươi, rau xà lách, rau thơm và chén nước mắm chua ngọt đậm đà vị tỏi ớt.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/97c16a7f-d584-4a34-bc68-f808adfa80aa/images/42a46840-5e02-445e-b41a-baf384f807ae.jpg", "http://localhost:9000/culinaryblog/recipes/97c16a7f-d584-4a34-bc68-f808adfa80aa/images/42a46840-5e02-445e-b41a-baf384f807ae_medium.jpg", "http://localhost:9000/culinaryblog/recipes/97c16a7f-d584-4a34-bc68-f808adfa80aa/images/42a46840-5e02-445e-b41a-baf384f807ae_thumb.jpg", "Chả Giò Hải Sản Giòn Tan", true),
            }
        ),
        new SeedDish(
            Title: "Thịt Kho Tàu Nước Dừa Xiêm",
            Slug: "thit-kho-tau-nuoc-dua-xiem",
            CategorySlug: "mon-kho-rim",
            PrepTimeMinutes: 20,
            CookTimeMinutes: 60,
            Servings: 4,
            Difficulty: DifficultyLevel.Easy,
            Description: "Thịt ba chỉ thái vuông bản to ninh mềm trong nước dừa xiêm ngọt thanh, trứng vịt thấm đẫm sốt kho màu cánh gián tự nhiên, đậm đà ngậy béo đưa cơm ngày mưa.",
            Content: "Thịt kho tàu nước dừa là món ăn mang đậm hương vị Tết phương Nam. Từng miếng thịt ba rọi cắt vuông vức ninh mềm rục, mỡ trong veo không ngấy, quyện cùng trứng vịt luộc đậm đà màu cánh gián vàng óng nhờ nước dừa tươi cô đọng lại.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/9e5929e6-1363-4343-8a03-903d5ea31200/images/b2173709-67ad-4a8e-beeb-3ec1d764434f.jpg",
            Nutrition: (580.00m, 30.00m, 12.00m, 46.00m, 0.00m, 940.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Thịt ba rọi rút sườn", 700.000m, "g", "Thái khối vuông 4-5cm"),
                ("Trứng vịt luộc", 6.000m, "quả", "Bóc vỏ luộc chín"),
                ("Nước dừa tươi xiêm", 600.000m, "ml", "Nước dừa ngọt tự nhiên"),
                ("Nước mắm ngon truyền thống", 4.000m, "muỗng canh", "Nêm vị đậm đà"),
                ("Hành tím, tỏi, ớt sừng", 50.000m, "g", "Băm nhuyễn vắt lấy nước ướp"),
                ("Đường phèn, nước màu dừa", 2.000m, "muỗng canh", "Tạo màu cánh gián tự nhiên"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Ướp thịt ba rọi", "Chần thịt qua nước sôi có gừng khử mùi, rửa sạch để ráo. Ướp thịt với nước mắm, đường phèn, nước màu dừa và nước cốt hành tỏi trong ít nhất 40 phút cho ngấm gia vị.", 40),
                (2, "Xào săn thịt", "Cho thịt vào nồi đảo lửa vừa cho miếng thịt săn lại và mỡ bắt đầu trong veo.", 10),
                (3, "Kho thịt với nước dừa xiêm", "Đổ nước dừa tươi ngập mặt thịt, đun sôi rồi hạ lửa nhỏ liu riu hớt bọt. Khi thịt bắt đầu mềm thì thả trứng vịt luộc vào kho cùng đến khi nước sốt keo lại màu cánh gián óng ánh.", 50),
                (4, "Hoàn thiện và thưởng thức", "Múc thịt kho và trứng ra đĩa sâu lòng, chan nước sốt sóng sánh thơm lừng. Ăn cùng cơm nóng dẻo và đĩa dưa giá hoặc cải chua muối giòn.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/9e5929e6-1363-4343-8a03-903d5ea31200/images/b2173709-67ad-4a8e-beeb-3ec1d764434f.jpg", "http://localhost:9000/culinaryblog/recipes/9e5929e6-1363-4343-8a03-903d5ea31200/images/b2173709-67ad-4a8e-beeb-3ec1d764434f_medium.jpg", "http://localhost:9000/culinaryblog/recipes/9e5929e6-1363-4343-8a03-903d5ea31200/images/b2173709-67ad-4a8e-beeb-3ec1d764434f_thumb.jpg", "Thịt Kho Tàu Nước Dừa Xiêm", true),
            }
        ),
        new SeedDish(
            Title: "Canh Chua Cá Lóc Nam Bộ",
            Slug: "canh-chua-ca-loc-nam-bo",
            CategorySlug: "mon-canh",
            PrepTimeMinutes: 20,
            CookTimeMinutes: 25,
            Servings: 4,
            Difficulty: DifficultyLevel.Easy,
            Description: "Nồi canh chua bốc khói nghi ngút, vị chua thanh từ nước cốt me chín, ngọt béo cá lóc đồng, giòn ngọt từ bạc hà, bắp chuối, đậu bắp, thơm nức ngò gai và rau ngổ.",
            Content: "Canh chua cá lóc là món canh quốc hồn quốc túy của vùng sông nước Tây Nam Bộ. Sự hòa quyện tuyệt diệu giữa vị chua dịu của me, vị ngọt đậm từ cá lóc đồng tươi roi rói cùng hương thơm ngào ngạt của ngò gai, rau ngổ và tỏi phi giòn rụm.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/315c2b65-f539-420f-a57c-7819b6506eb4/images/9c4c4d52-1413-4d63-89dd-0aafea76b7b2.jpg",
            Nutrition: (220.00m, 22.00m, 18.00m, 6.00m, 3.00m, 710.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Cá lóc đồng", 600.000m, "g", "Làm sạch cắt khúc dày"),
                ("Nước cốt me chín", 3.000m, "muỗng canh", "Lọc lấy nước chua thanh"),
                ("Dứa (thơm)", 0.250m, "quả", "Thái lát mỏng"),
                ("Cà chua chín", 2.000m, "quả", "Bổ múi cau"),
                ("Đậu bắp, bạc hà (dọc mùng)", 150.000m, "g", "Tước vỏ thái xéo"),
                ("Giá đỗ tươi", 100.000m, "g", "Rửa sạch"),
                ("Ngò gai, rau ngổ, tỏi phi thơm", 1.000m, "phần", "Thái nhỏ rắc lên canh"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Sơ chế cá lóc và nguyên liệu", "Cá lóc xát muối và chanh khử nhớt, rửa sạch cắt khúc vừa ăn. Dứa thái lát, cà chua bổ múi cau, bạc hà tước vỏ bóp muối rửa sạch vắt ráo.", 15),
                (2, "Nấu nước canh chua", "Đun sôi 1.2 lít nước, cho nước cốt me và dứa vào nấu sôi. Thả từng khúc cá lóc vào đun lửa vừa, hớt bọt để nước canh trong vắt.", 10),
                (3, "Thêm các loại rau quả", "Khi cá chín tới, cho cà chua, đậu bắp, bạc hà vào đun sôi thêm 3 phút. Nêm nước mắm ngon, muối, đường cho vị chua ngọt hài hòa thanh tao.", 5),
                (4, "Hoàn thiện bát canh", "Cho giá đỗ vào tô, múc canh chua nóng cùng cá lóc lên trên. Rắc ngò gai, rau ngổ và tỏi phi thơm lừng. Dùng kèm chén nước mắm ớt mặn nguyên chất.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/315c2b65-f539-420f-a57c-7819b6506eb4/images/9c4c4d52-1413-4d63-89dd-0aafea76b7b2.jpg", "http://localhost:9000/culinaryblog/recipes/315c2b65-f539-420f-a57c-7819b6506eb4/images/9c4c4d52-1413-4d63-89dd-0aafea76b7b2_medium.jpg", "http://localhost:9000/culinaryblog/recipes/315c2b65-f539-420f-a57c-7819b6506eb4/images/9c4c4d52-1413-4d63-89dd-0aafea76b7b2_thumb.jpg", "Canh Chua Cá Lóc Nam Bộ", true),
            }
        ),
        new SeedDish(
            Title: "Bò Kho Bánh Mì Đậm Đà",
            Slug: "bo-kho-banh-mi-dam-da",
            CategorySlug: "mon-chinh",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 90,
            Servings: 4,
            Difficulty: DifficultyLevel.Easy,
            Description: "Thịt nạm bò ninh nhừ mềm thơm quyện mùi sả, hoa hồi, quế chi, sốt cà ri và dầu màu điều sánh mịn, cà rốt ngọt mềm, ăn kèm bánh mì nóng giòn chấm muối tiêu chanh.",
            Content: "Bò kho là món ăn quen thuộc đậm đà của người miền Nam. Từng thớ thịt nạm và gân bò mềm rục ngấm đượm hương sả nồng nàn, hoa hồi, quế chi và sốt cà ri thơm lừng. Nước sốt sánh đậm màu cam rực rỡ, chấm cùng bánh mì nướng nóng giòn tan tạo nên trải nghiệm ẩm thực khó quên.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/73d93e1e-3f72-476d-8633-e037ac9dc544/images/4cd3cd78-1079-48a3-8d32-3fa7b314a18d.jpg",
            Nutrition: (480.00m, 36.00m, 32.00m, 22.00m, 4.00m, 880.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Nạm bò pha gân", 800.000m, "g", "Thái miếng vuông 3-4cm"),
                ("Cà rốt", 2.000m, "củ", "Cắt khúc hoặc tỉa hoa dày"),
                ("Sả cây", 6.000m, "nhánh", "Đập dập cắt khúc"),
                ("Gói gia vị bò kho (hoa hồi, quế, thảo quả)", 1.000m, "gói", "Tạo hương thơm"),
                ("Dầu màu điều, bột cà ri", 2.000m, "muỗng canh", "Tạo màu đỏ cam sánh đẹp"),
                ("Nước dừa tươi", 500.000m, "ml", "Tạo vị ngọt đậm đà"),
                ("Bánh mì giòn, húng quế, ngò gai", 1.000m, "phần", "Ăn kèm"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Ướp thịt nạm bò", "Thịt bò chần qua nước sôi gừng khử mùi, rửa sạch. Ướp bò với gia vị bò kho, dầu màu điều, bột cà ri, nước mắm, đường, hạt tiêu và tỏi sả băm trong 45 phút.", 45),
                (2, "Xào săn thịt bò", "Phi thơm tỏi và sả cây đập dập, trút thịt bò đã ướp vào đảo lửa lớn cho thịt săn chắc và dậy mùi thơm ngào ngạt.", 10),
                (3, "Hầm mềm thịt bò", "Đổ nước dừa tươi và nước lọc ngập mặt thịt. Đun sôi rồi hạ nhỏ lửa hầm liu riu trong 60 phút cho thịt bò chín mềm nhừ.", 60),
                (4, "Nấu cùng cà rốt và hoàn thiện", "Cho cà rốt vào hầm thêm 15 phút cho cà rốt vừa chín tới ngọt mềm. Nêm nếm lại gia vị vừa miệng, hòa chút bột năng tạo độ sánh nhẹ rồi múc ra tô rắc ngò gai, dùng nóng cùng bánh mì giòn rụm.", 15),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/73d93e1e-3f72-476d-8633-e037ac9dc544/images/4cd3cd78-1079-48a3-8d32-3fa7b314a18d.jpg", "http://localhost:9000/culinaryblog/recipes/73d93e1e-3f72-476d-8633-e037ac9dc544/images/4cd3cd78-1079-48a3-8d32-3fa7b314a18d_medium.jpg", "http://localhost:9000/culinaryblog/recipes/73d93e1e-3f72-476d-8633-e037ac9dc544/images/4cd3cd78-1079-48a3-8d32-3fa7b314a18d_thumb.jpg", "Bò Kho Bánh Mì Đậm Đà", true),
            }
        ),
        new SeedDish(
            Title: "Mì Quảng Tôm Thịt Đà Nẵng",
            Slug: "mi-quang-tom-thit-da-nang",
            CategorySlug: "bun-mi-pho",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 45,
            Servings: 3,
            Difficulty: DifficultyLevel.Easy,
            Description: "Sợi mì Quảng vàng dẻo óng ánh chan chút nước nhưn đậm đà nấu từ tôm đất và thịt heo, trứng cút bùi béo, rắc đậu phộng rang giã nhỏ, ăn cùng bánh tráng mè nướng giòn.",
            Content: "Mì Quảng là biểu tượng của tinh thần hào sảng xứ Quảng. Sợi mì gạo to bản nhuộm màu củ nghệ vàng óng ả, chan chút nước nhưn sánh đậm ngọt thơm từ tôm đất và thịt ba chỉ rim kẹo. Món ăn hòa quyện giữa vị bùi của đậu phộng rang, giòn tan của bánh tráng nướng mè và hương thơm nồng của rau búp chuối.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/157e8125-d704-4f4f-a6e4-f5dca1f99079/images/1d47b330-ce55-4524-9c5a-cf1202761df1.jpg",
            Nutrition: (460.00m, 25.00m, 58.00m, 15.00m, 3.00m, 820.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Sợi mì Quảng tươi", 600.000m, "g", "Mì sợi dẹt vàng nghệ"),
                ("Tôm đất tươi", 300.000m, "g", "Cắt đầu đuôi, ướp gia vị"),
                ("Thịt ba chỉ heo", 300.000m, "g", "Thái lát mỏng vừa ăn"),
                ("Trứng cút luộc", 10.000m, "quả", "Bóc vỏ"),
                ("Củ nén (hành tăm)", 50.000m, "g", "Đập dập tạo mùi thơm đặc trưng"),
                ("Dầu màu điều", 2.000m, "muỗng canh", "Tạo màu đỏ cam óng ả"),
                ("Đậu phộng rang, bánh tráng nướng mè", 100.000m, "g", "Bẻ nhỏ ăn kèm"),
                ("Rau sống búp chuối, cải non", 200.000m, "g", "Rửa sạch"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Ướp tôm thịt", "Ướp tôm và thịt ba chỉ với nước mắm ngon, hạt nêm, đường, tiêu xay và củ nén đập dập trong 20 phút cho thấm gia vị.", 20),
                (2, "Xào nước nhưn mì Quảng", "Đun nóng dầu màu điều, phi thơm củ nén rồi trút thịt và tôm vào xào săn trên lửa lớn cho dậy mùi thơm nức.", 10),
                (3, "Nấu nước nhưn sánh đậm", "Chế nước hầm xương vào xăm xắp mặt thịt, thêm trứng cút vào om lửa nhỏ 20 phút cho nước nhưn ngấm đượm sánh ngọt đậm đà.", 20),
                (4, "Trình bày bát mì Quảng", "Trần mì Quảng xếp vào tô, chan một vá nước nhưn cùng tôm thịt và trứng cút lên trên. Rắc đậu phộng rang, hành lá, bẻ miếng bánh tráng mè giòn rụm và thưởng thức cùng đĩa rau sống xanh mát.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/157e8125-d704-4f4f-a6e4-f5dca1f99079/images/1d47b330-ce55-4524-9c5a-cf1202761df1.jpg", "http://localhost:9000/culinaryblog/recipes/157e8125-d704-4f4f-a6e4-f5dca1f99079/images/1d47b330-ce55-4524-9c5a-cf1202761df1_medium.jpg", "http://localhost:9000/culinaryblog/recipes/157e8125-d704-4f4f-a6e4-f5dca1f99079/images/1d47b330-ce55-4524-9c5a-cf1202761df1_thumb.jpg", "Mì Quảng Tôm Thịt Đà Nẵng", true),
            }
        ),
        new SeedDish(
            Title: "Bánh Cuốn Nóng Thịt Băm Mộc Nhĩ",
            Slug: "banh-cuon-nong-thit-bam-moc-nhi",
            CategorySlug: "mon-hap-luoc",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 25,
            Servings: 3,
            Difficulty: DifficultyLevel.Easy,
            Description: "Bánh tráng mỏng tang trắng ngần, cuộn tròn nhân thịt nạc vai băm xào mộc nhĩ hành khô thơm lừng, rắc ruốc tôm và hành phi vàng giòn, chấm nước mắm chanh ớt ấm nóng.",
            Content: "Bánh cuốn nóng là thức quà sáng thanh nhã của người Hà Nội. Từng lá bánh tráng hơi mỏng tang, dẻo dai trắng muốt ôm ấp phần nhân thịt nạc băm xào mộc nhĩ thơm nức tiêu sọ. Rắc thêm nắm hành phi giòn rụm tự làm vàng ruộm và chấm vào bát nước mắm chua ngọt ấm nồng dậy mùi chả quế.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/dceed039-da43-41ad-94f8-11e19d28b8c4/images/5b4655c4-d03f-4d25-aae0-64ca99644b5b.jpg",
            Nutrition: (310.00m, 14.00m, 46.00m, 8.00m, 2.00m, 650.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Bột gạo tẻ pha chút bột năng", 300.000m, "g", "Khuấy mịn cùng nước lọc"),
                ("Thịt nạc vai heo băm nhỏ", 250.000m, "g", "Xào săn"),
                ("Mộc nhĩ, nấm hương", 40.000m, "g", "Ngâm nở băm nhỏ"),
                ("Hành khô phi vàng", 50.000m, "g", "Hành phi thơm giòn rụm"),
                ("Chả quế hoặc chả lụa", 150.000m, "g", "Thái lát ăn kèm"),
                ("Rau thơm: húng quế, mùi ta", 100.000m, "g", "Rửa sạch"),
                ("Nước mắm pha ấm chua ngọt", 1.000m, "bát", "Pha chanh tỏi ớt ấm nóng"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Xào nhân bánh cuốn", "Phi thơm hành tím băm, cho thịt nạc vai băm, mộc nhĩ, nấm hương vào xào chín săn, nêm nước mắm và tiêu xay thơm lừng rồi tắt bếp.", 10),
                (2, "Tráng bánh cuốn mỏng mướt", "Đun sôi nồi hơi nước tráng bánh hoặc chảo chống dính. Thoa nhẹ lớp dầu mỏng, múc vá bột láng thật mỏng đều mặt chảo, đậy nắp 45 giây cho bánh chín trong veo.", 5),
                (3, "Cuộn nhân bánh", "Nhẹ nhàng lấy lá bánh cuốn ra mâm sạch thoa dầu, rải một muỗng nhân thịt mộc nhĩ vào giữa rồi cuộn tròn thon dài đều tay.", 10),
                (4, "Hoàn thiện đĩa bánh cuốn", "Cắt bánh cuốn thành từng khúc vừa ăn xếp lên đĩa. Rắc nhiều hành phi vàng giòn và ruốc tôm lên mặt bánh, dùng kèm chả quế và bát nước mắm chua ngọt ấm nóng.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/dceed039-da43-41ad-94f8-11e19d28b8c4/images/5b4655c4-d03f-4d25-aae0-64ca99644b5b.jpg", "http://localhost:9000/culinaryblog/recipes/dceed039-da43-41ad-94f8-11e19d28b8c4/images/5b4655c4-d03f-4d25-aae0-64ca99644b5b_medium.jpg", "http://localhost:9000/culinaryblog/recipes/dceed039-da43-41ad-94f8-11e19d28b8c4/images/5b4655c4-d03f-4d25-aae0-64ca99644b5b_thumb.jpg", "Bánh Cuốn Nóng Thịt Băm Mộc Nhĩ", true),
            }
        ),
        new SeedDish(
            Title: "Bún Riêu Cua Đồng Chua Thanh",
            Slug: "bun-rieu-cua-dong-chua-thanh",
            CategorySlug: "bun-mi-pho",
            PrepTimeMinutes: 35,
            CookTimeMinutes: 40,
            Servings: 4,
            Difficulty: DifficultyLevel.Easy,
            Description: "Món bún thanh tao đất Bắc với nước riêu cua đồng thơm ngọt tự nhiên, gạch cua xào béo ngậy, vị chua thanh từ giấm bỗng và cà chua, đậu phụ rán phồng chấm mắm tôm ngon.",
            Content: "Bún riêu cua đồng mang hương vị đồng quê mộc mạc mà quyến rũ khôn nguôi. Từng tảng riêu cua đồng nổi bồng bềnh vàng óng trên nồi nước dùng đỏ au màu cà chua xào dầu điều, thoang thoảng vị chua thanh dịu dàng của giấm bỗng nếp và mùi mắm tôm nồng đượm đặc trưng.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/d2b5481d-7fcf-465b-a938-0aa568d4ef9a/images/14d5295f-2c32-422f-802a-fe594a5ca7b7.jpg",
            Nutrition: (420.00m, 22.00m, 54.00m, 13.00m, 3.00m, 910.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Cua đồng tươi", 600.000m, "g", "Giã nhuyễn lọc lấy nước thịt cua"),
                ("Đậu phụ", 3.000m, "bìa", "Cắt miếng nhỏ rán vàng giòn"),
                ("Cà chua chín", 4.000m, "quả", "Bổ múi cau xào sơ"),
                ("Giấm bỗng nếp ngon", 0.500m, "bát con", "Tạo vị chua thanh đặc trưng"),
                ("Bún tươi", 800.000m, "g", "Trần nóng"),
                ("Mắm tôm Bắc hảo hạng", 1.000m, "muỗng canh", "Nêm dậy mùi thơm"),
                ("Hành hoa, rau chuối, kinh giới", 300.000m, "g", "Ăn kèm"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Lọc và nấu riêu cua", "Cua đồng giã nhuyễn lọc với 2 lít nước, thêm chút muối khuấy đều rồi bắc lên bếp đun lửa vừa. Khuấy nhẹ tay đến khi tảng riêu cua kết tủa đóng bánh nổi lên mặt nước thì vớt ra đĩa riêng.", 20),
                (2, "Xào gạch cua và cà chua", "Khêu gạch cua trong mai, phi thơm hành khô với dầu màu điều rồi xào chín gạch cua thơm bùi. Cho cà chua bổ múi cau vào xào chín tới giữ nguyên miếng.", 10),
                (3, "Nấu nước dùng bún riêu", "Trút cà chua xào và gạch cua vào nồi nước cua, thêm đậu phụ rán giòn. Nêm giấm bỗng nếp, nước mắm, hạt nêm và thìa mắm tôm nhỏ cho dậy mùi thơm nồng nàn thanh dịu.", 15),
                (4, "Trình bày bát bún riêu", "Xếp bún tươi vào tô, múc miếng riêu cua, đậu rán, cà chua lên trên. Chan ngập nước dùng sôi sùng sục, rắc hành hoa thái nhỏ và thưởng thức cùng đĩa rau sống tươi mát.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/d2b5481d-7fcf-465b-a938-0aa568d4ef9a/images/14d5295f-2c32-422f-802a-fe594a5ca7b7.jpg", "http://localhost:9000/culinaryblog/recipes/d2b5481d-7fcf-465b-a938-0aa568d4ef9a/images/14d5295f-2c32-422f-802a-fe594a5ca7b7_medium.jpg", "http://localhost:9000/culinaryblog/recipes/d2b5481d-7fcf-465b-a938-0aa568d4ef9a/images/14d5295f-2c32-422f-802a-fe594a5ca7b7_thumb.jpg", "Bún Riêu Cua Đồng Chua Thanh", true),
            }
        ),
        new SeedDish(
            Title: "Bún Thịt Nướng Chả Giò Sài Gòn",
            Slug: "bun-thit-nuong-cha-gio-sai-gon",
            CategorySlug: "bun-mi-pho",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 25,
            Servings: 2,
            Difficulty: DifficultyLevel.Easy,
            Description: "Thịt nạc dăm ướp sả mè nướng xém cạnh thơm phức, cuốn chả giò giòn rụm trên bát bún tươi, phủ ngập rau thơm, dưa leo băm, mỡ hành, đậu phộng chan nước mắm tỏi ớt.",
            Content: "Bún thịt nướng là món ăn khoái khẩu của người phương Nam với sự phong phú màu sắc và hài hòa hương vị. Từng lát thịt nạc dăm ướp sả mè nướng trên than hoa đượm khói, cuốn chả giò giòn rụm, phủ đều mỡ hành óng ánh trên nền bún trắng ngần và rau ghém xanh mát.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/ee79990d-bd15-4bf0-8a22-d0d46bcfd7c6/images/49cb5d87-ba9b-4550-9997-a326114ca6e2.jpg",
            Nutrition: (510.00m, 24.00m, 62.00m, 19.00m, 4.00m, 820.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Thịt nạc dăm heo", 400.000m, "g", "Thái lát mỏng vừa ăn"),
                ("Chả giò chiên sẵn", 4.000m, "cuốn", "Chiên vàng giòn rụm"),
                ("Bún tươi", 500.000m, "g", "Tơi xốp"),
                ("Sả băm, hạt mè trắng, tỏi", 3.000m, "muỗng canh", "Ướp thịt nướng"),
                ("Mỡ hành lá", 1.000m, "chén nhỏ", "Hành lá phi dầu thơm"),
                ("Đậu phộng rang giã dập", 50.000m, "g", "Rắc lên mặt tô"),
                ("Nước mắm chua ngọt tỏi ớt", 1.000m, "bát", "Chan vào bún"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Ướp thịt nạc dăm", "Ướp thịt với sả băm, mè trắng rang, tỏi ớt, mật ong, nước mắm ngon và dầu hào trong 30 phút cho ngấm đượm.", 30),
                (2, "Nướng thịt xém cạnh", "Xếp thịt lên vỉ than hoa hoặc áp chảo lửa lớn cho thịt chín vàng óng ả, xém nhẹ các cạnh thơm nức mùi sả nướng.", 15),
                (3, "Chiên chả giò", "Thả chả giò vào chảo dầu nóng chiên vàng đều giòn rụm rồi vớt ra cắt đôi.", 10),
                (4, "Trình bày tô bún thịt nướng", "Cho xà lách, rau thơm, dưa leo băm vào tô rồi xếp bún tươi lên. Đặt thịt nướng thơm lừng và chả giò giòn rụm lên trên, rưới mỡ hành óng ả, rắc đậu phộng rang và chan ngập nước mắm chua ngọt.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/ee79990d-bd15-4bf0-8a22-d0d46bcfd7c6/images/49cb5d87-ba9b-4550-9997-a326114ca6e2.jpg", "http://localhost:9000/culinaryblog/recipes/ee79990d-bd15-4bf0-8a22-d0d46bcfd7c6/images/49cb5d87-ba9b-4550-9997-a326114ca6e2_medium.jpg", "http://localhost:9000/culinaryblog/recipes/ee79990d-bd15-4bf0-8a22-d0d46bcfd7c6/images/49cb5d87-ba9b-4550-9997-a326114ca6e2_thumb.jpg", "Bún Thịt Nướng Chả Giò Sài Gòn", true),
            }
        ),
        new SeedDish(
            Title: "Bánh Khọt Tôm Tươi Vũng Tàu",
            Slug: "banh-khot-tom-tuoi-vung-tau",
            CategorySlug: "mon-chien-ran",
            PrepTimeMinutes: 25,
            CookTimeMinutes: 25,
            Servings: 3,
            Difficulty: DifficultyLevel.Easy,
            Description: "Bánh khọt vàng giòn rụm nướng khuôn gang, nhân tôm tươi ngọt thịt, rắc bột tôm sấy và hành lá phi thơm, ăn cuốn xà lách rau cải chấm nước mắm pha đu đủ sợi.",
            Content: "Bánh khọt Vũng Tàu quyến rũ thực khách bởi lớp vỏ bột gạo giòn tan rụm bên ngoài nhưng bên trong vẫn giữ được độ mềm béo ngậy của nước cốt dừa. Con tôm tươi đỏ au nằm gọn trong lòng bánh, phủ lên lớp mỡ hành xanh mướt và bột tôm cháy vàng rực bắt mắt.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/5174f8c8-b834-499b-8bfa-99021cfc7e65/images/98e76316-db88-44c3-b937-fc4e805ce95a.jpg",
            Nutrition: (370.00m, 15.00m, 42.00m, 16.00m, 2.00m, 610.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Bột bánh khọt hoặc bột gạo", 300.000m, "g", "Pha nước cốt dừa và hành lá"),
                ("Tôm sú tươi lột vỏ", 200.000m, "g", "Để nguyên con xào sơ"),
                ("Bột tôm chấy đỏ cam", 50.000m, "g", "Rắc lên mặt bánh"),
                ("Mỡ hành thơm", 1.000m, "chén nhỏ", "Hành hoa phi dầu"),
                ("Rau xà lách, rau thơm các loại", 300.000m, "g", "Rửa sạch để ráo"),
                ("Đu đủ ngâm chua ngọt", 100.000m, "g", "Thả vào bát nước mắm chấm"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Pha bột bánh khọt", "Hòa tan bột gạo, bột chiên giòn, nước cốt dừa và hành lá cắt nhỏ. Để bột nghỉ 15 phút cho bột nở sánh mịn.", 15),
                (2, "Đổ bánh khọt vào khuôn", "Đặt khuôn bánh khọt lên bếp, quét một lớp dầu mỏng vào từng khuôn tròn. Khi khuôn nóng già, múc bột đổ vào lưng khuôn, đặt 1 con tôm tươi vào giữa rồi đậy nắp lại.", 5),
                (3, "Chiên bánh giòn rụm", "Mở nắp rưới thêm chút dầu ăn quanh rìa bánh, chiên đến khi vành bánh khọt chuyển sang màu vàng giòn rụm thì vớt bánh ra đĩa.", 8),
                (4, "Hoàn thiện và thưởng thức", "Quết mỡ hành óng ánh và rắc bột tôm cháy lên từng chiếc bánh khọt. Cuốn bánh cùng lá xà lách cải xanh, chấm vào chén nước mắm chua ngọt đu đủ giòn.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/5174f8c8-b834-499b-8bfa-99021cfc7e65/images/98e76316-db88-44c3-b937-fc4e805ce95a.jpg", "http://localhost:9000/culinaryblog/recipes/5174f8c8-b834-499b-8bfa-99021cfc7e65/images/98e76316-db88-44c3-b937-fc4e805ce95a_medium.jpg", "http://localhost:9000/culinaryblog/recipes/5174f8c8-b834-499b-8bfa-99021cfc7e65/images/98e76316-db88-44c3-b937-fc4e805ce95a_thumb.jpg", "Bánh Khọt Tôm Tươi Vũng Tàu", true),
            }
        ),
        new SeedDish(
            Title: "Chè Hạt Sen Long Nhãn Cung Đình",
            Slug: "che-hat-sen-long-nhan-cung-dinh",
            CategorySlug: "mon-trang-mieng",
            PrepTimeMinutes: 25,
            CookTimeMinutes: 30,
            Servings: 4,
            Difficulty: DifficultyLevel.Easy,
            Description: "Chè cung đình Huế thanh khiết với hạt sen tươi bùi bùi bọc trong lớp cùi nhãn ngọt mọng nước, nấu cùng đường phèn thanh tao và hoa nhài ướp lạnh dịu dàng.",
            Content: "Chè hạt sen long nhãn là món tráng miệng quý tộc của cung đình Huế. Hạt sen hồ Tịnh Tâm bở bùi, được luộc chín tới rồi khéo léo lồng vào trong từng múi long nhãn mọng nước trong suốt. Nước chè nấu bằng đường phèn ngọt thanh như sương sớm, thoang thoảng hương hoa nhài thanh khiết.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/3cdcd31b-72bb-43ca-a1fd-1ec4c97b5651/images/0998ed72-3358-482a-8209-f00b4e48fa3a.jpg",
            Nutrition: (180.00m, 4.00m, 40.00m, 1.00m, 3.00m, 20.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Hạt sen tươi Huế", 300.000m, "g", "Thông tâm sen, rửa sạch"),
                ("Nhãn lồng tươi mọng nước", 500.000m, "g", "Bóc vỏ tách hạt khéo léo"),
                ("Đường phèn kết tinh", 150.000m, "g", "Tạo vị ngọt thanh tao"),
                ("Hoa nhài tươi hoặc nước hoa bưởi", 5.000m, "bông", "Ướp hương thơm"),
                ("Nước lọc tinh khiết", 1.000m, "lít", "Nấu nước chè"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Nấu hạt sen chín bở", "Cho hạt sen vào nồi cùng nước luộc chín mềm nhưng không nát. Vớt hạt sen ra đảo nhẹ cùng chút đường phèn cho hạt sen ngấm vị ngọt bùi.", 20),
                (2, "Lồng hạt sen vào long nhãn", "Dùng dao tỉa tách hạt nhãn khéo léo giữ nguyên múi cùi nhãn mọng nước. Nhẹ nhàng nhét từng hạt sen bùi béo vào bên trong múi nhãn.", 15),
                (3, "Nấu nước đường phèn", "Đun sôi 1 lít nước với lượng đường phèn còn lại cho tan hoàn toàn, hớt sạch bọt cho nước đường trong vắt.", 10),
                (4, "Hoàn thiện bát chè hạt sen", "Thả long nhãn bọc hạt sen vào nồi nước đường đun sôi lăn tăn 1 phút rồi tắt bếp để nhãn giữ độ giòn sần sật. Thả vài cánh hoa nhài thơm ngát, múc ra chén ướp lạnh trước khi thưởng thức.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/3cdcd31b-72bb-43ca-a1fd-1ec4c97b5651/images/0998ed72-3358-482a-8209-f00b4e48fa3a.jpg", "http://localhost:9000/culinaryblog/recipes/3cdcd31b-72bb-43ca-a1fd-1ec4c97b5651/images/0998ed72-3358-482a-8209-f00b4e48fa3a_medium.jpg", "http://localhost:9000/culinaryblog/recipes/3cdcd31b-72bb-43ca-a1fd-1ec4c97b5651/images/0998ed72-3358-482a-8209-f00b4e48fa3a_thumb.jpg", "Chè Hạt Sen Long Nhãn Cung Đình", true),
            }
        ),
        new SeedDish(
            Title: "Canh Khổ Qua Nhồi Thịt Thanh Nhiệt",
            Slug: "canh-kho-qua-nhoi-thit-thanh-nhiet",
            CategorySlug: "mon-canh",
            PrepTimeMinutes: 25,
            CookTimeMinutes: 35,
            Servings: 4,
            Difficulty: DifficultyLevel.Easy,
            Description: "Trái khổ qua xanh mướt nhồi thịt nạc băm và mộc nhĩ giòn ngọt, ninh trong nước hầm xương thanh mát, vị đắng nhẹ dịu ngọt hậu giải nhiệt cơ thể.",
            Content: "Canh khổ qua nhồi thịt là món ăn mang ý nghĩa 'mọi khổ cực đều qua đi' trong mâm cơm người Việt. Vị đắng thanh mát ban đầu của mướp đắng nhanh chóng chuyển thành vị ngọt hậu đậm đà khi hòa cùng nhân thịt băm mộc nhĩ dẻo dai và nước canh hầm ngọt sâu.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/cbdaecf5-b608-414e-9e9a-cbcdebbfe38f/images/18eeadcc-a7a7-4c3d-b18b-da349cf51de9.jpg",
            Nutrition: (210.00m, 18.00m, 12.00m, 10.00m, 3.00m, 560.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Mướp đắng (khổ qua)", 3.000m, "trái lớn", "Cắt khúc khoét ruột sạch"),
                ("Thịt nạc vai heo xay", 300.000m, "g", "Thịt băm dẻo mịn"),
                ("Mộc nhĩ ngâm nở", 30.000m, "g", "Thái chỉ băm nhỏ"),
                ("Hành tím, hành lá, ngò rí", 50.000m, "g", "Thái nhỏ"),
                ("Nước dùng xương hầm", 1.200m, "lít", "Nấu canh ngọt thanh"),
                ("Nước mắm, muối, tiêu sọ", 1.000m, "phần", "Nêm nhân và nước canh"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Trộn nhân thịt", "Trộn thịt băm cùng mộc nhĩ, hành tím băm, tiêu xay, chút nước mắm và hạt nêm. Dùng thìa quết đều tay cho nhân dẻo quánh.", 15),
                (2, "Nhồi thịt vào khổ qua", "Khổ qua cắt khúc 4-5cm khoét sạch ruột trắng bên trong. Dùng thìa nhồi chặt nhân thịt vào từng khúc khổ qua, miết phẳng hai đầu.", 10),
                (3, "Nấu canh chín mềm", "Đun sôi nước dùng xương, thả nhẹ từng khúc khổ qua nhồi thịt vào nồi đun sôi. Hạ nhỏ lửa hớt bọt, ninh trong 25 phút cho khổ qua chín mềm và giảm vị đắng gắt.", 25),
                (4, "Hoàn thiện bát canh", "Nêm chút nước mắm ngon và hạt nêm cho nước canh vừa vị ngọt dịu. Múc khổ qua ra tô, rắc hành lá ngò rí và tiêu sọ thơm nồng, ăn nóng cùng cơm.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/cbdaecf5-b608-414e-9e9a-cbcdebbfe38f/images/18eeadcc-a7a7-4c3d-b18b-da349cf51de9.jpg", "http://localhost:9000/culinaryblog/recipes/cbdaecf5-b608-414e-9e9a-cbcdebbfe38f/images/18eeadcc-a7a7-4c3d-b18b-da349cf51de9_medium.jpg", "http://localhost:9000/culinaryblog/recipes/cbdaecf5-b608-414e-9e9a-cbcdebbfe38f/images/18eeadcc-a7a7-4c3d-b18b-da349cf51de9_thumb.jpg", "Canh Khổ Qua Nhồi Thịt Thanh Nhiệt", true),
            }
        ),
        new SeedDish(
            Title: "Lẩu Bò Sa Tế Nước Dùng Đậm Đà",
            Slug: "lau-bo-sa-te-nuoc-dung-dam-da",
            CategorySlug: "mon-lau-nuoc-dung",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 60,
            Servings: 4,
            Difficulty: DifficultyLevel.Easy,
            Description: "Nồi lẩu bò nghi ngút khói với nước dùng ngọt sâu từ xương ống bò, thơm nức quế hồi và sa tế cay nồng, nhúng ngập bắp bò tươi, nạm bò và các loại nấm tươi mát.",
            Content: "Lẩu bò là món ăn tuyệt vời cho những buổi liên hoan sum họp ấm cúng. Nước dùng lẩu được ninh kỳ công từ xương bò cùng các vị thảo mộc thiên nhiên, màu đỏ óng ánh hấp dẫn của ớt sa tế cay tê đầu lưỡi. Khi ăn nhúng từng lát bắp bò tươi ngọt, đậu hũ non và rau cải xanh giòn mát.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/ef1ba9ed-c321-42ff-abb5-b49ccd63ef64/images/fc2caebf-978d-4298-899b-18d8b876a0e2.jpg",
            Nutrition: (540.00m, 42.00m, 28.00m, 26.00m, 4.00m, 980.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Xương ống bò hầm nước dùng", 1.000m, "kg", "Ninh lấy 2.5 lít nước ngọt"),
                ("Bắp bò hoa tươi thái mỏng", 400.000m, "g", "Thịt bò tươi nhúng lẩu"),
                ("Nạm bò luộc mềm", 300.000m, "g", "Cắt miếng vừa ăn"),
                ("Đậu phụ non", 2.000m, "hộp", "Cắt khối vuông"),
                ("Ớt sa tế tôm cay nồng", 3.000m, "muỗng canh", "Tạo vị cay thơm"),
                ("Nấm kim châm, nấm đùi gà", 300.000m, "g", "Rửa sạch cắt chân"),
                ("Rau mồng tơi, cải bẹ xanh, mì trứng", 400.000m, "g", "Nhúng lẩu"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Nấu nước cốt lẩu bò", "Ninh xương bò với hành tây nướng và củ sả đập dập trong 2 giờ cho ra hết chất ngọt tủy xương bò.", 90),
                (2, "Xào sa tế và tạo hương lẩu", "Phi thơm tỏi ớt băm và sả băm với dầu màu điều, cho sa tế cay vào đảo đều rồi trút vào nồi nước dùng xương bò.", 10),
                (3, "Nêm nếm gia vị nước lẩu", "Nêm hạt nêm, đường phèn, chút nước mắm ngon và tương đen cho nước lẩu có vị đậm đà cay nồng nàn quyến rũ.", 10),
                (4, "Thưởng thức lẩu bò", "Đặt nồi lẩu lên bếp gas mini đun sôi sùng sục. Thả nạm bò, đậu hũ non vào trước, nhúng từng lát bắp bò tươi cùng nấm và rau xanh, chấm chao sa tế béo cay.", 10),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/ef1ba9ed-c321-42ff-abb5-b49ccd63ef64/images/fc2caebf-978d-4298-899b-18d8b876a0e2.jpg", "http://localhost:9000/culinaryblog/recipes/ef1ba9ed-c321-42ff-abb5-b49ccd63ef64/images/fc2caebf-978d-4298-899b-18d8b876a0e2_medium.jpg", "http://localhost:9000/culinaryblog/recipes/ef1ba9ed-c321-42ff-abb5-b49ccd63ef64/images/fc2caebf-978d-4298-899b-18d8b876a0e2_thumb.jpg", "Lẩu Bò Sa Tế Nước Dùng Đậm Đà", true),
            }
        ),
        new SeedDish(
            Title: "Cơm Chiên Dương Châu Giòn Ngon",
            Slug: "com-chien-duong-chau-gion-ngon",
            CategorySlug: "mon-xao",
            PrepTimeMinutes: 20,
            CookTimeMinutes: 15,
            Servings: 3,
            Difficulty: DifficultyLevel.Easy,
            Description: "Từng hạt cơm vàng ươm tơi xốp xào lửa lớn cùng tôm sú tươi ngọt, lạp xưởng Mai Quế Lộ béo ngậy, đậu Hà Lan giòn ngọt và trứng gà thơm phức.",
            Content: "Cơm chiên Dương Châu là món cơm xào quen thuộc trong mọi mâm tiệc Việt. Hạt cơm nguội để ráo xào trên chảo gang lửa lớn cho săn giòn vàng óng, quyện cùng sắc đỏ của lạp xưởng, xanh mát của đậu Hà Lan, cam tươi của cà rốt và vàng thơm của trứng gà đánh bông.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/ff91c3a3-c872-42b7-b9c8-90d506dc7f2c/images/b03a7495-8a2b-4c2d-8d53-f5e1ab01e046.jpg",
            Nutrition: (490.00m, 18.00m, 68.00m, 16.00m, 3.00m, 720.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Cơm nguội tơi xốp", 4.000m, "chén", "Trộn đều lòng đỏ trứng gà"),
                ("Lạp xưởng Mai Quế Lộ", 2.000m, "cây", "Luộc sơ thái hạt lựu"),
                ("Tôm sú tươi lột vỏ", 150.000m, "g", "Thái hạt lựu xào chín"),
                ("Đậu Hà Lan, cà rốt", 100.000m, "g", "Thái hạt lựu chần sơ"),
                ("Trứng gà tươi", 2.000m, "quả", "Đánh tan tráng mỏng"),
                ("Hành lá, tỏi băm, tiêu xay", 1.000m, "phần", "Gia vị xào cơm"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Chuẩn bị hạt cơm", "Bóp tơi cơm nguội cùng 1 lòng đỏ trứng gà và thìa dầu ăn cho từng hạt cơm áo một lớp vàng óng ả.", 5),
                (2, "Xào nhân lạp xưởng tôm", "Phi thơm tỏi băm, trút lạp xưởng và tôm vào xào săn chín tới, tiếp tục cho cà rốt và đậu Hà Lan vào đảo đều rồi trút ra đĩa riêng.", 5),
                (3, "Chiên cơm tơi giòn", "Làm nóng chảo gang với chút dầu ăn, cho cơm vào chiên trên lửa lớn, đảo đều tay liên tục cho hạt cơm săn lại và nổ lách tách giòn tan.", 8),
                (4, "Trộn nhân và hoàn thiện", "Trút toàn bộ phần nhân lạp xưởng, tôm, rau củ vào chảo cơm, nêm chút hạt nêm, tiêu xay và nước tương ngon đảo đều 2 phút rồi rắc hành lá thái nhỏ tắt bếp.", 3),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/ff91c3a3-c872-42b7-b9c8-90d506dc7f2c/images/b03a7495-8a2b-4c2d-8d53-f5e1ab01e046.jpg", "http://localhost:9000/culinaryblog/recipes/ff91c3a3-c872-42b7-b9c8-90d506dc7f2c/images/b03a7495-8a2b-4c2d-8d53-f5e1ab01e046_medium.jpg", "http://localhost:9000/culinaryblog/recipes/ff91c3a3-c872-42b7-b9c8-90d506dc7f2c/images/b03a7495-8a2b-4c2d-8d53-f5e1ab01e046_thumb.jpg", "Cơm Chiên Dương Châu Giòn Ngon", true),
            }
        ),
        new SeedDish(
            Title: "Cơm Cháy Kho Quẹt Miền Tây",
            Slug: "com-chay-kho-quet-mien-tay",
            CategorySlug: "an-vat",
            PrepTimeMinutes: 20,
            CookTimeMinutes: 30,
            Servings: 3,
            Difficulty: DifficultyLevel.Easy,
            Description: "Cơm cháy đáy chảo vàng giòn rụm chấm cùng niêu kho quẹt tôm khô thịt ba chỉ tóp mỡ đậm đà, quyện tiêu xanh cay nồng và đĩa rau củ luộc thanh mát.",
            Content: "Cơm cháy kho quẹt là món ăn bình dị mà mê hoặc lòng người đất phương Nam. Miếng cơm cháy mỏng tang vàng ươm giòn rụm được bẻ rôm rốp, chấm ngập vào tộ đất kho quẹt keo quánh đậm đà vị nước mắm ngon, tôm khô dai ngọt và tóp mỡ giòn tan đượm cay tiêu xanh.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/faa8cce3-2847-40af-bdaf-ab38d9b7ba7e/images/b6531979-b1d0-444d-8aa3-e87d576d4ed2.jpg",
            Nutrition: (420.00m, 14.00m, 58.00m, 16.00m, 3.00m, 850.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Cơm trắng dẻo", 3.000m, "chén", "Ép mỏng đáy chảo làm cơm cháy"),
                ("Thịt ba chỉ rút sườn", 200.000m, "g", "Thái hạt lựu thắng lấy tóp mỡ"),
                ("Tôm khô ngon", 60.000m, "g", "Ngâm nước ấm mềm"),
                ("Nước mắm ngon truyền thống", 4.000m, "muỗng canh", "Nấu sốt kho quẹt"),
                ("Đường thốt nốt hoặc đường cát", 3.000m, "muỗng canh", "Tạo vị ngọt keo sệt"),
                ("Tiêu xanh, ớt hiểm, hành tím", 30.000m, "g", "Tạo vị cay thơm nồng"),
                ("Rau củ luộc: bầu, đậu bắp, cà rốt", 300.000m, "g", "Chấm kho quẹt"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Làm tóp mỡ giòn", "Cho thịt ba chỉ thái hạt lựu vào chảo thắng lửa nhỏ cho ra hết mỡ, vớt tóp mỡ vàng giòn rụm ra để riêng.", 10),
                (2, "Nấu niêu kho quẹt", "Phi thơm hành tím với 2 muỗng mỡ lợn, cho tôm khô vào xào săn. Trút hỗn hợp nước mắm, đường và tiêu xanh vào tộ đất đun liu riu cho nước sốt keo quánh lại thì trút tóp mỡ và ớt hiểm vào đun thêm 2 phút.", 15),
                (3, "Làm cơm cháy giòn rụm", "Cho cơm trắng vào chảo chống dính, dùng muôi miết thật mỏng đều khắp lòng chảo, bật lửa nhỏ xoay đều chảo cho lớp đáy cơm vàng giòn rụm rồi lấy ra bẻ miếng vừa ăn.", 15),
                (4, "Thưởng thức cơm cháy kho quẹt", "Bày đĩa cơm cháy vàng ươm cùng niêu kho quẹt sôi sùng sục và đĩa rau củ luộc xanh mướt. Chấm miếng cơm cháy giòn rụm vào sốt kho quẹt đậm đà khó cưỡng.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/faa8cce3-2847-40af-bdaf-ab38d9b7ba7e/images/b6531979-b1d0-444d-8aa3-e87d576d4ed2.jpg", "http://localhost:9000/culinaryblog/recipes/faa8cce3-2847-40af-bdaf-ab38d9b7ba7e/images/b6531979-b1d0-444d-8aa3-e87d576d4ed2_medium.jpg", "http://localhost:9000/culinaryblog/recipes/faa8cce3-2847-40af-bdaf-ab38d9b7ba7e/images/b6531979-b1d0-444d-8aa3-e87d576d4ed2_thumb.jpg", "Cơm Cháy Kho Quẹt Miền Tây", true),
            }
        ),
        new SeedDish(
            Title: "Bò Nướng Tảng Đá Hoa Cương",
            Slug: "bo-nuong-tang-da-hoa-cuong",
            CategorySlug: "mon-nuong-bbq",
            PrepTimeMinutes: 20,
            CookTimeMinutes: 15,
            Servings: 3,
            Difficulty: DifficultyLevel.Easy,
            Description: "Từng tảng thịt thăn bò tươi mềm mọng nước nướng trực tiếp trên phiến đá nham thạch rực nóng, giữ trọn vị ngọt tự nhiên nguyên bản chấm sốt tiêu xanh đậm đà.",
            Content: "Bò nướng đá là phương pháp chế biến độc đáo giúp giữ nguyên vẹn độ mọng nước và vị ngọt tự nhiên của thăn bò tươi. Miếng thịt bò dày dặn xèo xèo trên phiến đá nóng rực, không ám khói dầu mỡ, thơm lừng bơ tỏi và thảo mộc thiên nhiên.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/746d4fb2-a9ce-45df-94db-bad6f813bbfa/images/a0479978-164c-4ecc-b598-e9b8c9348139.jpg",
            Nutrition: (450.00m, 38.00m, 8.00m, 28.00m, 1.00m, 620.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Thịt thăn bò tươi hảo hạng", 600.000m, "g", "Thái tảng dày 2cm"),
                ("Tiêu xanh tươi đập dập", 30.000m, "g", "Ướp và làm sốt chấm"),
                ("Bơ lạt nguyên chất", 50.000m, "g", "Thoa lên mặt đá nướng"),
                ("Tỏi đập dập, lá hương thảo", 20.000m, "g", "Tạo hương thơm"),
                ("Rau củ nướng: măng tây, bắp ngọt", 200.000m, "g", "Nướng kèm"),
                ("Muối biển hạt thô", 1.000m, "muỗng cà phê", "Rắc lên thịt"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Sơ chế thịt bò tảng", "Thấm khô miếng thịt bò bằng khăn sạch. Ướp nhẹ cùng tiêu xanh đập dập, muối biển và chút dầu ô liu trong 15 phút.", 15),
                (2, "Làm nóng đá nướng", "Nung phiến đá nướng trên bếp than hoặc bếp gas trong 15 phút cho đá tích nhiệt cực cao.", 15),
                (3, "Nướng thịt trên đá nóng", "Quết một lớp bơ lạt lên mặt đá rực nóng, đặt tảng thịt bò lên trên cùng tỏi và lá hương thảo. Nướng mỗi mặt 3 phút cho thịt xém cạnh bên ngoài mà bên trong vẫn mềm mọng hồng hào.", 8),
                (4, "Thưởng thức bò nướng đá", "Cắt miếng bò nướng thành từng lát vừa ăn, chấm muối tiêu chanh hoặc sốt tiêu xanh thơm nồng, ăn kèm măng tây giòn ngọt.", 4),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/746d4fb2-a9ce-45df-94db-bad6f813bbfa/images/a0479978-164c-4ecc-b598-e9b8c9348139.jpg", "http://localhost:9000/culinaryblog/recipes/746d4fb2-a9ce-45df-94db-bad6f813bbfa/images/a0479978-164c-4ecc-b598-e9b8c9348139_medium.jpg", "http://localhost:9000/culinaryblog/recipes/746d4fb2-a9ce-45df-94db-bad6f813bbfa/images/a0479978-164c-4ecc-b598-e9b8c9348139_thumb.jpg", "Bò Nướng Tảng Đá Hoa Cương", true),
            }
        ),
        new SeedDish(
            Title: "Hàu Nướng Mỡ Hành Đậu Phộng",
            Slug: "hau-nuong-mo-hanh-dau-phong",
            CategorySlug: "mon-nuong-bbq",
            PrepTimeMinutes: 20,
            CookTimeMinutes: 10,
            Servings: 3,
            Difficulty: DifficultyLevel.Easy,
            Description: "Những con hàu sữa tươi béo múp nướng than hồng thơm nức mùi mỡ hành xanh mướt, rắc đậu phộng rang giòn bùi và chút hành phi giòn rụm chấm muối tiêu chanh.",
            Content: "Hàu nướng mỡ hành là món hải sản nướng khoái khẩu trên khắp các bãi biển Việt Nam. Hàu sữa béo múp ngậy sữa, nướng trên vỉ than hoa sôi xèo xèo trong lớp mỡ hành thơm nức mũi, hòa quyện với vị bùi giòn của đậu phộng rang và hành phi vàng ruộm.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/de89453f-7db0-418c-85e6-52e54c446af0/images/68556009-3fa2-4644-ae19-0cbb015ca962.jpg",
            Nutrition: (310.00m, 16.00m, 14.00m, 22.00m, 1.00m, 480.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Hàu sữa tươi nguyên con", 1.500m, "kg", "Tách vỏ làm sạch cát"),
                ("Hành lá tươi", 100.000m, "g", "Thái nhỏ làm mỡ hành"),
                ("Đậu phộng rang giòn", 60.000m, "g", "Giã dập vừa ăn"),
                ("Hành phi vàng giòn", 30.000m, "g", "Rắc lên mặt hàu"),
                ("Dầu ăn hảo hạng", 80.000m, "ml", "Làm mỡ hành"),
                ("Muối tiêu chanh tươi ớt băm", 1.000m, "chén", "Chấm kèm"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Sơ chế hàu sữa", "Dùng bàn chải chà sạch vỏ hàu dưới vòi nước chảy. Dùng dao tách vỏ hàu, rửa nhẹ thịt hàu để ráo nước xếp lên khay.", 15),
                (2, "Làm mỡ hành thơm phức", "Đun sôi 80ml dầu ăn, nêm chút hạt nêm rồi rót vào bát hành lá thái nhỏ đảo đều cho hành chín tái giữ màu xanh tươi.", 5),
                (3, "Nướng hàu trên than hồng", "Đặt từng con hàu lên vỉ nướng trên than hoa đỏ rực. Khi thịt hàu bắt đầu săn lại và sôi lăn tăn, rưới muỗng mỡ hành óng ánh lên từng con hàu nướng thêm 2 phút.", 6),
                (4, "Hoàn thiện đĩa hàu nướng", "Gắp hàu ra đĩa, rắc đậu phộng rang giòn bùi và hành phi giòn tan lên mặt. Vắt chút chanh tươi lên thịt hàu và thưởng thức ngay khi còn nóng hổi.", 3),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/de89453f-7db0-418c-85e6-52e54c446af0/images/68556009-3fa2-4644-ae19-0cbb015ca962.jpg", "http://localhost:9000/culinaryblog/recipes/de89453f-7db0-418c-85e6-52e54c446af0/images/68556009-3fa2-4644-ae19-0cbb015ca962_medium.jpg", "http://localhost:9000/culinaryblog/recipes/de89453f-7db0-418c-85e6-52e54c446af0/images/68556009-3fa2-4644-ae19-0cbb015ca962_thumb.jpg", "Hàu Nướng Mỡ Hành Đậu Phộng", true),
            }
        ),
        new SeedDish(
            Title: "Bánh Bông Lan Trứng Muối Chà Bông",
            Slug: "banh-bong-lan-trung-muoi-cha-bong",
            CategorySlug: "mon-trang-mieng",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 35,
            Servings: 6,
            Difficulty: DifficultyLevel.Easy,
            Description: "Cốt bánh bông lan xốp mềm thơm mùi bơ sữa, phủ ngập sốt phô mai béo ngậy, trứng muối nướng bùi bùi và chà bông heo tơi xốp mằn mặn hấp dẫn.",
            Content: "Bánh bông lan trứng muối là món bánh ngọt được giới trẻ Việt Nam đặc biệt yêu thích. Cốt bánh chiffon mềm ẩm như mây tan ngay đầu lưỡi, kết hợp hoàn hảo cùng vị mặn mòi bùi béo của lòng đỏ trứng muối nướng rượu mai quế lộ, sốt phô mai kem béo ngậy và chà bông thơm lừng.",
            CoverImageUrl: "http://localhost:9000/culinaryblog/recipes/3868783a-08c4-498c-badc-a4ca964a7c63/images/7f111db7-707e-47d8-81c6-af1d6437bce8.jpg",
            Nutrition: (360.00m, 12.00m, 42.00m, 16.00m, 1.00m, 450.00m),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Bột mì số 8 chuyên làm bánh", 100.000m, "g", "Rây mịn"),
                ("Trứng gà tươi", 4.000m, "quả", "Tách riêng lòng đỏ và lòng trắng"),
                ("Lòng đỏ trứng muối", 6.000m, "quả", "Rửa rượu nướng thơm"),
                ("Chà bông (ruốc thịt heo)", 50.000m, "g", "Tơi xốp sợi nhỏ"),
                ("Sốt phô mai bơ sữa", 100.000m, "g", "Nấu kem béo mịn"),
                ("Đường cát trắng", 80.000m, "g", "Đánh bông lòng trắng"),
                ("Dầu ăn và sữa tươi không đường", 40.000m, "ml", "Hòa cùng bột"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Nướng lòng đỏ trứng muối", "Rửa lòng đỏ trứng muối với rượu trắng khử tanh, thoa chút dầu mè rồi nướng ở 160 độ C trong 7 phút cho trứng chín dẻo bùi thơm.", 10),
                (2, "Đánh bông cốt bánh bông lan", "Đánh bông lòng trắng trứng với đường cát đến chóp mềm dẻo. Trộn nhẹ nhàng lòng đỏ, sữa tươi, dầu ăn và bột mì rây mịn vào theo kỹ thuật fold để không vỡ bọt khí.", 15),
                (3, "Nướng cốt bánh xốp mềm", "Đổ bột vào khuôn lót giấy nến, nướng ở 155 độ C trong 35 phút đến khi cốt bánh nở phồng vàng ươm, thử tăm khô ráo là bánh chín hoàn hảo.", 35),
                (4, "Trang trí bánh và thưởng thức", "Để bánh nguội bớt, phủ lớp sốt phô mai kem béo ngậy lên mặt bánh, xếp từng nửa lòng đỏ trứng muối bùi béo và rắc ngập chà bông heo tơi xốp.", 10),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/3868783a-08c4-498c-badc-a4ca964a7c63/images/7f111db7-707e-47d8-81c6-af1d6437bce8.jpg", "http://localhost:9000/culinaryblog/recipes/3868783a-08c4-498c-badc-a4ca964a7c63/images/7f111db7-707e-47d8-81c6-af1d6437bce8_medium.jpg", "http://localhost:9000/culinaryblog/recipes/3868783a-08c4-498c-badc-a4ca964a7c63/images/7f111db7-707e-47d8-81c6-af1d6437bce8_thumb.jpg", "Bánh Bông Lan Trứng Muối Chà Bông", true),
            }
        ),
        new SeedDish(
            Title: "Hủ Tiếu Nam Vang Thập Cẩm Sài Gòn",
            Slug: "hu-tieu-nam-vang-sai-gon",
            CategorySlug: "bun-mi-pho",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 120,
            Servings: 4,
            Difficulty: DifficultyLevel.Medium,
            Description: "Hủ tiếu Nam Vang chuẩn vị với nước lèo trong veo ngọt lịm từ xương ống và mực nướng, tôm tươi giòn ngọt, thịt băm, gan cật heo và tỏi phi thơm nức.",
            Content: "Hủ tiếu Nam Vang là món ăn quen thuộc và được yêu thích hàng đầu tại Sài Gòn. Món ăn thu hút thực khách bởi nước dùng ninh từ xương tủy heo, khô mực nướng và tôm khô thanh ngọt tự nhiên, không ngấy. Tô hủ tiếu thập cẩm hội tụ đủ tôm sú tươi luộc đỏ au, thịt nạc dăm băm nhuyễn xào thơm, gan và cật heo luộc giòn sần sật, trứng cút bùi béo và lớp tỏi phi vàng ươm thơm lừng. Có thể ăn theo kiểu nước hoặc kiểu khô trộn sốt chua ngọt đặc trưng.",
            CoverImageUrl: null,
            Nutrition: (460.00m, 28.00m, 58.00m, 13.00m, 2.50m, null),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Hủ tiếu dai Sa Đéc", 500.000m, "g", "Trụng qua nước sôi xóc dầu tỏi"),
                ("Xương ống heo tươi", 1000.000m, "g", "Chặt khúc ninh lấy nước dùng"),
                ("Mực khô nhỏ và tôm khô", 50.000m, "g", "Nướng thơm thả vào nước dùng"),
                ("Thịt nạc heo băm nhuyễn", 200.000m, "g", "Xào chín với hành tỏi thơm"),
                ("Tôm sú tươi sống", 300.000m, "g", "Luộc chín bóc vỏ chừa đuôi"),
                ("Gan heo và cật heo tươi", 200.000m, "g", "Khử mùi rượu gừng, luộc giòn"),
                ("Trứng cút luộc", 8.000m, "quả", "Luộc chín bóc sạch vỏ"),
                ("Cần tàu, hẹ lá, giá đỗ, tỏi phi", 150.000m, "g", "Rửa sạch thái khúc vừa ăn"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Ninh nước lèo thanh ngọt", "Xương ống heo chần nước sôi rửa sạch, ninh cùng 3.5 lít nước, mực khô nướng thơm và tôm khô trong 2 giờ với lửa nhỏ liu riu, hớt bọt thường xuyên để nước dùng trong vắt.", 120),
                (2, "Sơ chế tôm, thịt băm và phủ tạng", "Tôm sú luộc chín bóc vỏ giữ đuôi. Gan và cật heo ngâm rượu gừng khử mùi, luộc chín tới rồi ngâm nước đá cho giòn. Thịt heo băm xào thơm với hành tỏi băm và chút tiêu.", 20),
                (3, "Phi tỏi vàng thơm giòn rụm", "Băm nhỏ 1 củ tỏi lớn, phi ngập dầu với lửa vừa đến khi tỏi chuyển sang màu vàng tươi giòn rụm thì vớt ra ngay để tỏi không bị đắng.", 10),
                (4, "Hoàn thiện tô hủ tiếu", "Trụng hủ tiếu qua nước sôi cho vào tô, trộn 1 thìa dầu tỏi phi. Xếp tôm, thịt băm, gan cật, trứng cút lên trên cùng hẹ và cần tàu. Chan nước lèo sôi sùng sục ngập mặt hủ tiếu, rắc tỏi phi và dùng nóng.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/45587299-7d26-4b3d-b4d8-97f1eaa36cb2/images/efcd7072-0fe8-4683-bf87-af624bf5cfcc.jpg", "http://localhost:9000/culinaryblog/recipes/45587299-7d26-4b3d-b4d8-97f1eaa36cb2/images/efcd7072-0fe8-4683-bf87-af624bf5cfcc_medium.jpg", "http://localhost:9000/culinaryblog/recipes/45587299-7d26-4b3d-b4d8-97f1eaa36cb2/images/efcd7072-0fe8-4683-bf87-af624bf5cfcc_thumb.jpg", "Hủ Tiếu Nam Vang Thập Cẩm Sài Gòn", true),
            }
        ),
        new SeedDish(
            Title: "Bún Đậu Mắm Tôm Thập Cẩm Hà Nội",
            Slug: "bun-dau-mam-tom-thap-cam-ha-noi",
            CategorySlug: "bun-mi-pho",
            PrepTimeMinutes: 25,
            CookTimeMinutes: 35,
            Servings: 3,
            Difficulty: DifficultyLevel.Easy,
            Description: "Mẹt bún đậu mắm tôm chuẩn Hà Thành với đậu phụ Mơ rán vàng ngoài giòn trong mềm béo, chả cốm giòn dẻo, nem rán, thịt chân giò luộc và chén mắm tôm đánh sủi bọt dậy mùi.",
            Content: "Bún đậu mắm tôm là món ăn dân dã đặc sắc bậc nhất của Hà Nội. Linh hồn của món ăn nằm ở những miếng đậu phụ làng Mơ béo ngậy được rán lướt ván hoặc giòn xốp vàng ươm, ăn kèm bún lá cắt miếng vừa miệng. Mẹt bún đầy đặn kết hợp thêm chả cốm dẻo thơm hạt cốm sữa, thịt bắp chân giò luộc thái mỏng giòn sần sật, nem rán giòn tan. Chén mắm tôm Thanh Hóa được pha khéo léo với đường, nước cốt quất chua thanh, ớt tươi cay nồng và chút mỡ sôi đánh sủi bọt mịn màng thơm nức mũi.",
            CoverImageUrl: null,
            Nutrition: (520.00m, 25.00m, 54.00m, 22.00m, 3.00m, null),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Bún lá tươi (bún vắt miếng)", 600.000m, "g", "Cắt miếng vuông vừa ăn"),
                ("Đậu phụ trắng làng Mơ", 4.000m, "bìa", "Cắt miếng vuông chiên giòn"),
                ("Chả cốm tươi Hà Nội", 200.000m, "g", "Chiên vàng giòn cắt lát xéo"),
                ("Thịt bắp chân giò heo", 350.000m, "g", "Cuộn chỉ luộc chín ngâm đá"),
                ("Mắm tôm ngon nguyên chất", 3.000m, "muỗng canh", "Mắm tôm truyền thống Thanh Hóa"),
                ("Quất tươi và ớt chỉ thiên", 4.000m, "quả", "Vắt lấy nước cốt, ớt băm"),
                ("Rượu trắng ngon", 1.000m, "muỗng cà phê", "Khử tanh và tạo bọt cho mắm"),
                ("Rau kinh giới, tía tô, dưa leo", 200.000m, "g", "Rửa sạch ngâm nước muối loãng"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Luộc thịt bắp giò giòn ngọt", "Thịt chân giò cuộn tròn chỉ sạch, luộc cùng chút muối và hành tím đập dập trong 25 phút cho chín tới. Vớt ra ngâm nước đá lạnh rồi thái lát mỏng vừa ăn.", 25),
                (2, "Rán đậu phụ Mơ và chả cốm", "Đậu phụ cắt miếng vuông vừa ăn, thả vào chảo dầu nóng chiên vàng đều các mặt cho vỏ ngoài giòn rụm, ruột bên trong vẫn mềm mịn béo ngậy. Chả cốm chiên vàng giòn rồi cắt lát xéo.", 15),
                (3, "Pha chén mắm tôm sủi bọt", "Cho mắm tôm vào chén cùng đường, rượu trắng, nước cốt quất rồi dùng đũa đánh thật nhanh tay cho mắm tôm bông xốp sủi bọt mịn màng. Rưới thêm 1 muỗng mỡ vừa rán đậu đang sôi và thêm ớt tươi thái lát.", 5),
                (4, "Bày mẹt bún đậu và thưởng thức", "Xếp lá chuối lên mẹt, đặt bún lá cắt miếng, đậu rán nóng hổi, chả cốm, thịt chân giò luộc, dưa leo cùng rau kinh giới, tía tô xung quanh chén mắm tôm thơm lừng.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/a614dea0-b245-4010-8603-d94f9ecc9c30/images/8b91b535-eb78-4f6a-bc82-8f06c2712ae2.jpg", "http://localhost:9000/culinaryblog/recipes/a614dea0-b245-4010-8603-d94f9ecc9c30/images/8b91b535-eb78-4f6a-bc82-8f06c2712ae2_medium.jpg", "http://localhost:9000/culinaryblog/recipes/a614dea0-b245-4010-8603-d94f9ecc9c30/images/8b91b535-eb78-4f6a-bc82-8f06c2712ae2_thumb.jpg", "Bún Đậu Mắm Tôm Thập Cẩm Hà Nội", true),
            }
        ),
        new SeedDish(
            Title: "Cá Lóc Kho Tộ Đậm Đà Miền Tây",
            Slug: "ca-kho-to-mien-tay",
            CategorySlug: "mon-kho-rim",
            PrepTimeMinutes: 20,
            CookTimeMinutes: 45,
            Servings: 4,
            Difficulty: DifficultyLevel.Medium,
            Description: "Cá lóc đồng kho trong tộ đất với nước màu dừa Bến Tre, thịt ba rọi béo ngậy, tiêu sọ cay nồng và ớt hiểm đỏ au, nước kho sánh sệt đậm đà chuẩn cơm mẹ nấu.",
            Content: "Cá kho tộ là món ăn kinh điển trong bữa cơm gia đình người Việt, đặc biệt là vùng đồng bằng sông Cửu Long. Cá lóc đồng tươi sống thịt chắc ngọt được cắt khúc dày, kho trong tộ đất nung giữ nhiệt. Sự kết hợp giữa thịt cá tươi ngon, thịt ba rọi béo ngậy tươm mỡ, nước mắm cá cơm thượng hạng và nước màu dừa thơm ngọt tạo nên hương vị đậm đà khó cưỡng. Nồi cá kho tộ sôi lục bục trên bếp lửa liu riu, rắc tiêu sọ đập dập và hành lá xanh mướt, ăn cùng cơm trắng nóng hổi và đĩa rau luộc thì không gì sánh bằng.",
            CoverImageUrl: null,
            Nutrition: (380.00m, 32.00m, 12.00m, 22.00m, 0.80m, null),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Cá lóc đồng tươi", 600.000m, "g", "Làm sạch vảy, cắt khúc dày 3cm"),
                ("Thịt ba chỉ heo rút sườn", 150.000m, "g", "Cắt con chì vừa ăn"),
                ("Nước mắm cá cơm nguyên chất", 4.000m, "muỗng canh", "Nước mắm truyền thống 40 độ đạm"),
                ("Nước màu dừa Bến Tre", 1.000m, "muỗng canh", "Tạo sắc nâu cánh gián óng ả"),
                ("Đường thốt nốt nguyên chất", 2.000m, "muỗng canh", "Tạo vị ngọt đậm tự nhiên"),
                ("Hành tím và tỏi băm", 2.000m, "muỗng canh", "Băm nhỏ phi thơm"),
                ("Tiêu đen và ớt hiểm đỏ", 1.000m, "muỗng canh", "Tiêu sọ đập dập nguyên hạt"),
                ("Hành lá tươi và đầu hành", 4.000m, "nhánh", "Đầu hành đập dập, lá thái nhỏ"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Sơ chế cá lóc và thịt ba chỉ", "Cá lóc làm sạch vảy, xát muối hột và chanh để sạch nhớt rồi rửa sạch, cắt khúc dày 3cm. Thịt ba chỉ rửa sạch cắt con chì vừa ăn.", 15),
                (2, "Ướp cá và thịt ngấm gia vị", "Cho cá và thịt vào tộ đất, ướp cùng nước mắm ngon, nước màu dừa, đường, hạt nêm, hành tỏi băm và chút tiêu sọ trong 25 phút để gia vị thấm sâu vào từng thớ thịt.", 25),
                (3, "Đun săn và lên màu cá", "Đặt tộ cá lên bếp, đun lửa vừa cho nước ướp sôi bùng và thịt cá săn lại. Lật nhẹ mặt cá để thấm đều màu nâu cánh gián óng ả.", 10),
                (4, "Kho liu riu sánh sệt nước dùng", "Thêm 1/2 chén nước sôi (hoặc nước dừa tươi) vào tộ, hạ lửa nhỏ liu riu kho trong 35 phút cho nước kho cạn dần sánh sệt. Thả đầu hành, ớt hiểm và rắc tiêu sọ rồi tắt bếp, dùng nóng với cơm trắng.", 35),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/86025620-b8cd-4780-982d-2f2c0133ef5d/images/d21eee71-9ca7-4dc2-a461-0850bd39cbfc.jpg", "http://localhost:9000/culinaryblog/recipes/86025620-b8cd-4780-982d-2f2c0133ef5d/images/d21eee71-9ca7-4dc2-a461-0850bd39cbfc_medium.jpg", "http://localhost:9000/culinaryblog/recipes/86025620-b8cd-4780-982d-2f2c0133ef5d/images/d21eee71-9ca7-4dc2-a461-0850bd39cbfc_thumb.jpg", "Cá Lóc Kho Tộ Đậm Đà Miền Tây", true),
            }
        ),
        new SeedDish(
            Title: "Cháo Gà Ta Đậu Xanh Bổ Dưỡng",
            Slug: "chao-ga-ta-dau-xanh",
            CategorySlug: "mon-chinh",
            PrepTimeMinutes: 25,
            CookTimeMinutes: 60,
            Servings: 5,
            Difficulty: DifficultyLevel.Easy,
            Description: "Cháo gà ta hạt gạo nở búp sánh mịn, đậu xanh bùi bùi béo ngậy, thịt gà ta dai ngọt xé phay trộn gỏi bắp cải rau răm chua ngọt chuẩn vị ấm lòng.",
            Content: "Cháo gà ta đậu xanh là món ăn thanh bổ, bồi bổ sức khỏe tuyệt vời trong ẩm thực Việt. Gà ta thả vườn thịt chắc, ngọt thơm được luộc chín tới, nước luộc gà béo ngậy dùng để ninh cháo cùng gạo tẻ pha chút gạo nếp và đậu xanh cà vỏ. Từng hạt gạo nở bung mềm mại, hòa quyện với vị bùi béo của đậu xanh và hương thơm thoang thoảng của gừng tươi. Thịt gà sau khi nguội được xé miếng vừa ăn, lòng mề xào thơm, dùng kèm chén muối tiêu chanh ớt hiểm và đĩa gỏi bắp cải bóp chua ngọt giòn sần sật.",
            CoverImageUrl: null,
            Nutrition: (410.00m, 29.00m, 48.00m, 11.00m, 3.50m, null),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Gà ta thả vườn nguyên con", 1300.000m, "g", "Làm sạch, xát muối khử mùi"),
                ("Gạo tẻ thơm pha gạo nếp", 150.000m, "g", "Vo sạch rang sơ thơm hạt"),
                ("Đậu xanh cà vỏ", 80.000m, "g", "Ngâm nước ấm 30 phút đãi sạch"),
                ("Gừng tươi đập dập", 1.000m, "củ lớn", "Tạo hương thơm thanh ấm"),
                ("Hành tím nướng thơm", 3.000m, "củ", "Nướng xém vỏ cạo sạch"),
                ("Hành lá, ngò rí, tía tô", 100.000m, "g", "Rửa sạch thái nhỏ mịn"),
                ("Nước mắm ngon và tiêu xay", 2.000m, "muỗng canh", "Nêm cháo và chấm thịt"),
                ("Hành phi vàng giòn", 3.000m, "muỗng canh", "Rắc lên mặt cháo khi ăn"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Luộc gà ta da giòn thịt ngọt", "Cho gà vào nồi cùng gừng đập dập, hành tím nướng và 1 thìa muối. Đổ ngập 3 lít nước luộc lửa vừa trong 30 phút cho gà chín tới, vớt ra ngâm nước đá lạnh để da gà giòn dai rồi xé miếng hoặc chặt vừa ăn.", 30),
                (2, "Rang gạo và ngâm đậu xanh", "Gạo tẻ và nếp đem vo sạch, rang sơ trên chảo lửa nhỏ cho hạt gạo trắng đục dậy mùi thơm. Đậu xanh ngâm nước ấm 30 phút cho nở mềm rồi đãi sạch.", 15),
                (3, "Ninh cháo gà đậu xanh", "Cho gạo rang và đậu xanh vào nồi nước luộc gà đang sôi, hạ lửa nhỏ ninh liu riu trong 45 phút, thỉnh thoảng khuấy đều đáy nồi để cháo không bị khê, ninh đến khi hạt gạo nở búp sánh mịn.", 45),
                (4, "Hoàn thiện và thưởng thức", "Nêm nước mắm ngon và hạt nêm cho vừa miệng. Múc cháo nóng hổi ra tô, xếp thịt gà xé phay lên trên, rắc hành ngò thái nhỏ, hành phi thơm giòn và rắc tiêu sọ xay thơm nức. Dùng kèm muối tiêu chanh ớt.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/e100889c-b024-4842-a346-6c17d06499e3/images/6e2f1f2e-fef6-4489-81f8-16a6df7890e7.jpg", "http://localhost:9000/culinaryblog/recipes/e100889c-b024-4842-a346-6c17d06499e3/images/6e2f1f2e-fef6-4489-81f8-16a6df7890e7_medium.jpg", "http://localhost:9000/culinaryblog/recipes/e100889c-b024-4842-a346-6c17d06499e3/images/6e2f1f2e-fef6-4489-81f8-16a6df7890e7_thumb.jpg", "Cháo Gà Ta Đậu Xanh Bổ Dưỡng", true),
            }
        ),
        new SeedDish(
            Title: "Bánh Bèo Chén Tôm Chấy Xứ Huế",
            Slug: "banh-beo-chen-mien-trung",
            CategorySlug: "an-vat",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 30,
            Servings: 4,
            Difficulty: DifficultyLevel.Medium,
            Description: "Bánh bèo chén bột gạo mềm mướt xoáy tròn lòng chén, phủ ngập tôm chấy đỏ cam thơm ngọt, tóp mỡ da heo giòn rụm, mỡ hành óng ả và chan nước mắm ớt cay ngọt ấm nồng.",
            Content: "Bánh bèo chén là món bánh dân gian đặc sắc của xứ Huế và miền Trung. Chiếc bánh nhỏ xinh vừa vặn trong lòng chén đất nung nhỏ, khi hấp chín tạo thành vết xoáy lõm duyên dáng ở giữa bánh. Lớp bột bánh đúc từ gạo quê dẻo thơm, mềm mượt nhưng vẫn có độ dai nhẹ tự nhiên. Mặt bánh được phủ đều lớp tôm chấy giã nhuyễn xao khô vàng cam đẹp mắt, rưới mỡ hành lá xanh mướt và điểm vài miếng tóp mỡ chiên giòn tan. Khi ăn, dùng thìa chan chút nước mắm nhĩ pha loãng nấu cùng nước luộc tôm với vị ngọt thanh, cay xè ớt chỉ thiên xanh.",
            CoverImageUrl: null,
            Nutrition: (320.00m, 14.00m, 46.00m, 9.00m, 1.20m, null),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Bột gạo tẻ ngon", 200.000m, "g", "Pha nước lạnh ủ 30 phút"),
                ("Bột năng tạo độ dai mướt", 30.000m, "g", "Pha cùng bột gạo"),
                ("Tôm đất tươi làm tôm chấy", 250.000m, "g", "Luộc bóc vỏ, giã nhuyễn xao khô"),
                ("Da heo chiên giòn", 50.000m, "g", "Chiên vàng phồng xốp"),
                ("Hành lá tươi làm mỡ hành", 1.000m, "cụm", "Xối dầu sôi giữ màu xanh"),
                ("Nước mắm cá cơm nhĩ", 3.000m, "muỗng canh", "Nấu cùng nước luộc tôm và đường"),
                ("Ớt chỉ thiên xanh cay nồng", 3.000m, "trái", "Cắt khoanh thả vào mắm"),
                ("Dầu ăn và đường cát", 2.000m, "muỗng canh", "Nêm nước chấm và mỡ hành"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Pha và ủ bột bánh bèo", "Hòa tan bột gạo, bột năng cùng 300ml nước lạnh và 1/2 thìa cà phê muối, để bột nghỉ 30 phút. Trước khi đổ bánh, cho thêm 250ml nước ấm khuấy đều để bánh tạo xoáy đẹp khi hấp.", 30),
                (2, "Làm tôm chấy đỏ cam thơm ngọt", "Tôm đất luộc chín bóc vỏ, giã nhuyễn bằng cối. Cho tôm vào chảo không dính xao trên lửa nhỏ cùng chút muối, tiêu và nước màu gạch tôm đến khi thịt tôm tơi xốp, khô ráo và dậy mùi thơm.", 20),
                (3, "Làm mỡ hành và tóp mỡ", "Đun nóng 3 muỗng dầu ăn rồi đổ trực tiếp vào chén hành lá thái nhỏ để giữ màu xanh tươi. Da heo chiên ngập dầu cho giòn phồng xốp.", 10),
                (4, "Đổ bánh bèo và thưởng thức", "Thoa dầu vào các chén nhỏ xếp vào xửng hấp nóng 2 phút. Khuấy đều bột rồi múc vào từng chén (khoảng 2/3 chén), đậy nắp hấp lửa lớn trong 7-8 phút cho bánh chín trong và xoáy tròn. Lấy bánh ra, rắc tôm chấy, mỡ hành, tóp mỡ và chan nước mắm ớt ăn ngay khi còn ấm.", 15),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/00e15dd3-a858-43d0-8f5e-3549a75a3833/images/bb1741c6-b1af-423e-913e-4a1fc45ba5dd.jpg", "http://localhost:9000/culinaryblog/recipes/00e15dd3-a858-43d0-8f5e-3549a75a3833/images/bb1741c6-b1af-423e-913e-4a1fc45ba5dd_medium.jpg", "http://localhost:9000/culinaryblog/recipes/00e15dd3-a858-43d0-8f5e-3549a75a3833/images/bb1741c6-b1af-423e-913e-4a1fc45ba5dd_thumb.jpg", "Bánh Bèo Chén Tôm Chấy Xứ Huế", true),
            }
        ),
        new SeedDish(
            Title: "Cao Lầu Thịt Xíu Hội An",
            Slug: "cao-lau-hoi-an",
            CategorySlug: "bun-mi-pho",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 45,
            Servings: 4,
            Difficulty: DifficultyLevel.Medium,
            Description: "Món mì trứ danh phố cổ Hội An với sợi cao lầu vàng óng dai giòn ngâm tro củi cù lao, thịt xá xíu đậm đà, tóp mỡ chiên giòn rụm và rau đắng Trà Quế.",
            Content: "Cao lầu là món ăn biểu tượng của ẩm thực phố cổ Hội An. Sợi mì cao lầu được chế biến công phu từ gạo ngâm tro củi Cù Lao Chàm và nước giếng Bá Lễ, tạo nên màu vàng tự nhiên và độ dai sần sật đặc trưng. Thịt xá xíu được ướp ngũ vị hương, tỏi, nước tương ngon rồi rim nhỏ lửa đến khi săn chắc, đậm đà, óng ánh sắc nâu cánh gián. Khi thưởng thức, xếp sợi mì vào tô, phủ thịt xíu thái mỏng, tóp mỡ da heo chiên giòn, chan chút nước sốt xíu sền sệt đậm vị và ăn kèm rau sống Trà Quế tươi non.",
            CoverImageUrl: null,
            Nutrition: (480.00m, 26.00m, 62.00m, 14.00m, 3.00m, null),
            Ingredients: new (string Name, decimal? Qty, string? Unit, string? Notes)[]
            {
                ("Sợi mì cao lầu tươi", 600.000m, "g", "Trụng qua nước sôi trước khi dùng"),
                ("Thịt nạc vai heo tươi", 500.000m, "g", "Cắt miếng dày bản to"),
                ("Da heo làm tóp mỡ", 150.000m, "g", "Cắt hạt lựu chiên giòn"),
                ("Tỏi và hành tím băm", 2.000m, "củ", "Băm thật nhuyễn"),
                ("Ngũ vị hương và tiêu sọ", 1.000m, "gói", "Dùng ướp thịt xá xíu"),
                ("Nước tương ngon hảo hạng", 4.000m, "muỗng canh", "Tạo màu và vị đậm đà"),
                ("Đường thốt nốt nguyên chất", 1.000m, "muỗng canh", "Tạo độ ngọt thanh"),
                ("Rau sống làng Trà Quế", 200.000m, "g", "Rau đắng, giá đỗ, húng lủi"),
            },
            Steps: new (int Step, string Title, string Desc, int? Timer)[]
            {
                (1, "Sơ chế và ướp thịt xá xíu", "Thịt heo rửa sạch với nước muối loãng, cắt miếng dày cỡ 3 ngón tay. Ướp thịt với ngũ vị hương, tỏi hành băm, nước tương, đường thốt nốt và tiêu trong ít nhất 30 phút cho ngấm sâu.", 30),
                (2, "Rim thịt xá xíu đậm đà", "Cho 2 muỗng dầu ăn vào chảo, cho thịt vào chiên săn vàng đều các mặt. Đổ toàn bộ phần nước ướp cùng 1 chén nước dừa tươi vào, đun sôi rồi hạ lửa liu riu rim trong 40 phút cho thịt chín mềm, nước sốt sánh lại.", 40),
                (3, "Chiên tóp mỡ giòn rụm", "Da heo cạo sạch lông, luộc sơ rồi cắt hạt lựu, phơi se mặt rồi đem chiên ngập dầu cho phồng giòn rụm màu vàng ruộm, vớt ra để ráo dầu.", 15),
                (4, "Trình bày tô cao lầu và thưởng thức", "Chần sợi mì cao lầu qua nước sôi xếp vào tô, đặt vài lát thịt xíu thái mỏng, tóp mỡ giòn, rắc hành ngò và chan 3-4 muỗng nước sốt xíu nóng hổi. Dùng kèm rau đắng, giá chần và ớt sa tế Hội An.", 5),
            },
            Images: new (string Orig, string? Med, string? Thumb, string? Alt, bool Primary)[]
            {
                ("http://localhost:9000/culinaryblog/recipes/beddccf5-4292-4c2c-a8a3-90b925302712/images/5cf00aa6-7483-4c99-8847-d108912c7a09.jpg", "http://localhost:9000/culinaryblog/recipes/beddccf5-4292-4c2c-a8a3-90b925302712/images/5cf00aa6-7483-4c99-8847-d108912c7a09_medium.jpg", "http://localhost:9000/culinaryblog/recipes/beddccf5-4292-4c2c-a8a3-90b925302712/images/5cf00aa6-7483-4c99-8847-d108912c7a09_thumb.jpg", "Cao Lầu Thịt Xíu Hội An", true),
            }
        ),
    };

}
