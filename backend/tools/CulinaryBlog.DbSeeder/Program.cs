using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("======================================");
Console.WriteLine("     CULINARY BLOG DATABASE SEEDER");
Console.WriteLine("======================================");
Console.WriteLine();

var vietnameseRecipeNames = new[]
{
    "Phở bò Hà Nội", "Bún chả Hà Nội", "Cơm rang thập cẩm",
    "Gỏi cuốn tôm thịt", "Bánh xèo miền Tây", "Canh chua cá lóc",
    "Cá kho tộ", "Thịt kho trứng", "Bún bò Huế",
    "Gà nướng sả ớt", "Mì xào hải sản", "Bò xào rau củ",
    "Cá hồi áp chảo", "Súp bí đỏ", "Đậu hũ sốt cà",
    "Nấm xào tỏi", "Rau củ nướng", "Cháo gà đậu xanh",
    "Chè hạt sen", "Bánh chuối hấp", "Pudding sữa dừa",
    "Sinh tố xoài", "Kem dừa", "Bánh flan caramel",
    "Nem rán giòn", "Lẩu thái chua cay", "Cơm gà Hội An",
    "Bún riêu cua", "Mì quảng tôm thịt", "Chè bưởi",
    "Cơm chiên hải sản", "Bún thịt nướng", "Gà kho gừng",
    "Bò lúc lắc", "Tôm rang me", "Mực xào sa tế"
};

var factory = new ApplicationDbContextFactory();

await using var db =
    factory.CreateDbContext(Array.Empty<string>());

try
{
    Console.WriteLine("Đang kiểm tra kết nối PostgreSQL...");

    if (!await db.Database.CanConnectAsync())
    {
        Console.WriteLine("Không thể kết nối PostgreSQL.");
        return;
    }

    Console.WriteLine("Kết nối PostgreSQL thành công.");
    Console.WriteLine();

    // =========================================================
    // 1. TẠO RECIPE MẪU NẾU DATABASE CHƯA CÓ RECIPE
    // =========================================================

    if (!await db.Recipes.AnyAsync())
    {
        Console.WriteLine(
            "Chưa có Recipe. Đang tạo dữ liệu Recipe mẫu...");

        var demoUserId = Guid.NewGuid();
        var demoUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == "demo_user");
        if (demoUser == null)
        {
            demoUser = new ApplicationUser
            {
                Id = demoUserId,
                UserName = "demo_user",
                DisplayName = "Bếp Trưởng Hà Nội",
                Email = "demo@culinary.local",
                PasswordHash = "demo"
            };
            db.Users.Add(demoUser);
        }
        else
        {
            demoUserId = demoUser.Id;
            if (string.IsNullOrWhiteSpace(demoUser.DisplayName))
            {
                demoUser.DisplayName = "Bếp Trưởng Hà Nội";
            }
        }

        var demoCategory = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "mon-an-mau");
        if (demoCategory == null)
        {
            demoCategory = Category.Create(
                name: "Món ăn mẫu",
                slug: "mon-an-mau",
                description: "Danh mục mẫu để kiểm tra Step và Image",
                imageUrl: null,
                orderIndex: 1
            );
            db.Categories.Add(demoCategory);
        }

        var predefinedCategories = new[]
        {
            Category.Create("Món Việt", "cat-001", "Công thức đậm đà hương vị Việt.", null, 2),
            Category.Create("Món Á", "cat-002", "Những món ăn châu Á dễ thực hiện.", null, 3),
            Category.Create("Món Âu", "cat-003", "Công thức phương Tây cho căn bếp gia đình.", null, 4),
            Category.Create("Món Chay", "cat-004", "Món chay cân bằng và giàu dinh dưỡng.", null, 5),
            Category.Create("Tráng Miệng", "cat-005", "Các món ngọt cho ngày thêm vui.", null, 6)
        };
        foreach (var cat in predefinedCategories)
        {
            if (!await db.Categories.AnyAsync(c => c.Slug == cat.Slug))
            {
                db.Categories.Add(cat);
            }
        }

        await db.SaveChangesAsync();

        for (var i = 1; i <= 10; i++)
        {
            var recipe = new Recipe
            {
                Id = Guid.NewGuid(),

                // Recipe.AuthorId là Guid
                AuthorId = demoUserId,

                CategoryId = demoCategory.Id,

                Title = vietnameseRecipeNames[(i - 1) % vietnameseRecipeNames.Length],
                Slug = $"mon-an-mau-{i}",

                Description =
                    "Công thức mẫu dùng để kiểm tra dữ liệu bước chế biến và hình ảnh.",

                Content =
                    "Chuẩn bị nguyên liệu sạch, thực hiện từng bước và điều chỉnh gia vị theo khẩu vị.",

                PrepTimeMinutes = 10,
                CookTimeMinutes = 20,
                Servings = 2,

                Difficulty =
                    CulinaryBlog.Domain.Enums.DifficultyLevel.Easy,

                Status =
                    CulinaryBlog.Domain.Enums.RecipeStatus.Published,

                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            db.Recipes.Add(recipe);

        }

        await db.SaveChangesAsync();

        Console.WriteLine("Đã tạo 10 Recipe mẫu.");
        Console.WriteLine();
    }

    // =========================================================
    // 1B. TẠO RECIPE DEMO "Phở bò Hà Nội" NẾU CHƯA TỒN TẠI (IDEMPOTENT)
    // =========================================================

    const string phoBoSlug = "pho-bo-ha-noi";
    var existingPhoBo = await db.Recipes
        .Include(r => r.Steps)
        .Include(r => r.Images)
        .FirstOrDefaultAsync(r => r.Slug == phoBoSlug);

    if (existingPhoBo == null)
    {
        Console.WriteLine("Đang tạo Recipe demo: Phở bò Hà Nội...");

        var author = await db.Users.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Không tìm thấy User nào trong database.");

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "cat-001" || c.Name.Contains("Việt"))
            ?? await db.Categories.FirstAsync();

        var phoBoId = Guid.NewGuid();
        var phoBoRecipe = new Recipe
        {
            Id = phoBoId,
            AuthorId = author.Id,
            CategoryId = category.Id,
            Title = "Phở bò Hà Nội",
            Slug = phoBoSlug,
            Description = "Phở bò truyền thống Hà Nội với nước dùng thơm và thịt bò.",
            Content = "Phở bò là món ăn truyền thống nổi tiếng của Hà Nội với nước dùng trong veo, thơm mùi quế, hồi, thảo quả và vị ngọt thanh từ xương bò ninh nhừ.",
            PrepTimeMinutes = 30,
            CookTimeMinutes = 180,
            Servings = 4,
            Difficulty = CulinaryBlog.Domain.Enums.DifficultyLevel.Medium,
            Status = CulinaryBlog.Domain.Enums.RecipeStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        db.Recipes.Add(phoBoRecipe);

        // Thêm 5 bước nấu chuẩn cho Phở bò
        var phoSteps = new List<RecipeStep>
        {
            new RecipeStep
            {
                Id = Guid.NewGuid(),
                RecipeId = phoBoId,
                StepNumber = 1,
                Title = "Sơ chế xương và thịt bò",
                Description = "Rửa sạch xương ống và nạm bò với nước muối loãng, chần qua nước sôi 5 phút để khử bọt và mùi hôi rồi rửa lại bằng nước lạnh.",
                TimerMinutes = 15,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeStep
            {
                Id = Guid.NewGuid(),
                RecipeId = phoBoId,
                StepNumber = 2,
                Title = "Nướng gia vị thơm",
                Description = "Nướng hành tây, hành tím, gừng, hoa hồi, quế, thảo quả trên lửa đến khi dậy mùi thơm nồng, cạo sạch muội đen và rửa sơ.",
                TimerMinutes = 10,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeStep
            {
                Id = Guid.NewGuid(),
                RecipeId = phoBoId,
                StepNumber = 3,
                Title = "Ninh nước dùng phở",
                Description = "Cho xương bò cùng hành gừng nướng và túi gia vị vào nồi, ninh nhỏ lửa trong 3 tiếng, vớt bọt liên tục để nước dùng trong vắt, nêm muối và nước mắm vừa ăn.",
                TimerMinutes = 180,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeStep
            {
                Id = Guid.NewGuid(),
                RecipeId = phoBoId,
                StepNumber = 4,
                Title = "Chần bánh phở và xếp thịt",
                Description = "Chần bánh phở qua nước sôi rồi chia đều vào các tô. Thái mỏng thịt bò tái và nạm bò chín, xếp đẹp mắt lên trên mặt bánh phở kèm hành lá, rau mùi.",
                TimerMinutes = 5,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeStep
            {
                Id = Guid.NewGuid(),
                RecipeId = phoBoId,
                StepNumber = 5,
                Title = "Chan nước dùng và thưởng thức",
                Description = "Đun nước dùng sôi sùng sục rồi chan ngập bánh phở và thịt bò. Dùng ngay khi còn nóng hổi kèm chanh tươi, ớt lát và quẩy giòn.",
                TimerMinutes = 2,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

        // Thêm 2 ảnh chuẩn cho Phở bò (OrderIndex 0 là Primary)
        var phoImages = new List<RecipeImage>
        {
            new RecipeImage
            {
                Id = Guid.NewGuid(),
                RecipeId = phoBoId,
                OriginalUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=800",
                AltText = "Tô Phở bò Hà Nội thơm ngon nóng hổi",
                IsPrimary = true,
                OrderIndex = 0,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeImage
            {
                Id = Guid.NewGuid(),
                RecipeId = phoBoId,
                OriginalUrl = "https://images.unsplash.com/photo-1503764654157-724e030b1447?w=800",
                AltText = "Nước dùng phở bò trong vắt chuẩn vị",
                IsPrimary = false,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

        db.RecipeSteps.AddRange(phoSteps);
        db.RecipeImages.AddRange(phoImages);

        await db.SaveChangesAsync();
        Console.WriteLine("Đã thêm thành công Recipe 'Phở bò Hà Nội' kèm 5 Steps và 2 Images (Primary = true).");
    }
    else
    {
        Console.WriteLine("Recipe 'Phở bò Hà Nội' đã tồn tại, bỏ qua tạo mới (Idempotent).");
    }

    // =========================================================
    // 1C. TẠO 100 RECIPE MẪU CHO ngocphuoc255@gmail.com
    // =========================================================

    const string sampleOwnerEmail = "ngocphuoc255@gmail.com";
    var sampleOwner = await db.Users.FirstOrDefaultAsync(
        user => user.Email.ToLower() == sampleOwnerEmail);

    if (sampleOwner == null)
    {
        sampleOwner = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "ngocphuoc255",
            NormalizedUserName = "NGOCPHUOC255",
            Email = sampleOwnerEmail,
            NormalizedEmail = sampleOwnerEmail.ToUpperInvariant(),
            FullName = "Ngoc Phuoc",
            DisplayName = "Ngoc Phuoc",
            Bio = "Tác giả thử nghiệm với bộ công thức ẩm thực đa dạng.",
            AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=400",
            EmailConfirmed = true,
            Roles = ["Author"],
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        sampleOwner.PasswordHash =
            new PasswordHasher<ApplicationUser>()
                .HashPassword(sampleOwner, "NgocPhuoc@123");
        db.Users.Add(sampleOwner);
        await db.SaveChangesAsync();
        Console.WriteLine($"Đã tạo tài khoản Author: {sampleOwnerEmail}");
    }
    else if (string.IsNullOrWhiteSpace(sampleOwner.PasswordHash))
    {
        sampleOwner.FullName = "Ngoc Phuoc";
        sampleOwner.DisplayName = "Ngoc Phuoc";
        sampleOwner.Bio ??= "Tác giả thử nghiệm với bộ công thức ẩm thực đa dạng.";
        sampleOwner.AvatarUrl ??= "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=400";
        sampleOwner.EmailConfirmed = true;
        sampleOwner.Roles = ["Author"];
        sampleOwner.IsActive = true;
        sampleOwner.PasswordHash =
            new PasswordHasher<ApplicationUser>()
                .HashPassword(sampleOwner, "NgocPhuoc@123");
        await db.SaveChangesAsync();
    }

    var sampleCategory = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "mon-an-mau")
        ?? await db.Categories.FirstAsync();

    var existingSampleSlugs = await db.Recipes
        .Where(recipe => recipe.Slug.StartsWith("ngocphuoc-mau-"))
        .Select(recipe => recipe.Slug)
        .ToHashSetAsync();

    var sampleRecipes = new List<Recipe>();
    var sampleSteps = new List<RecipeStep>();
    var sampleIngredients = new List<RecipeIngredient>();
    var sampleImages = new List<RecipeImage>();

    for (var i = 1; i <= 100; i++)
    {
        var slug = $"ngocphuoc-mau-{i:D3}";
        if (existingSampleSlugs.Contains(slug))
        {
            continue;
        }

        var recipeId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = recipeId,
            AuthorId = sampleOwner.Id,
            CategoryId = sampleCategory.Id,
            Title = vietnameseRecipeNames[(i - 1) % vietnameseRecipeNames.Length],
            Slug = slug,
            Description = $"Công thức {vietnameseRecipeNames[(i - 1) % vietnameseRecipeNames.Length].ToLowerInvariant()} dành cho thực hành và kiểm thử dữ liệu.",
            Content = $"Chuẩn bị nguyên liệu đầy đủ, thực hiện lần lượt các bước và điều chỉnh gia vị cho phù hợp khẩu vị của món {vietnameseRecipeNames[(i - 1) % vietnameseRecipeNames.Length].ToLowerInvariant()}.",
            PrepTimeMinutes = 10 + (i % 20),
            CookTimeMinutes = 15 + (i % 45),
            Servings = 2 + (i % 6),
            CoverImageUrl = $"https://images.unsplash.com/photo-1547592180-85f173990554?w=800&seed={i}",
            Difficulty = (i % 3) switch
            {
                0 => CulinaryBlog.Domain.Enums.DifficultyLevel.Easy,
                1 => CulinaryBlog.Domain.Enums.DifficultyLevel.Medium,
                _ => CulinaryBlog.Domain.Enums.DifficultyLevel.Hard
            },
            Status = CulinaryBlog.Domain.Enums.RecipeStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-((i - 1) % 30)),
            CreatedAt = DateTime.UtcNow.AddMinutes(-i),
            IsDeleted = false,
            Nutrition = new RecipeNutrition
            {
                Source = CulinaryBlog.Domain.Enums.NutritionSource.Manual,
                Calories = 250 + i,
                Protein = 12 + (i % 20),
                Carbohydrates = 30 + (i % 25),
                Fat = 8 + (i % 15),
                Fiber = 4 + (i % 8),
                Sodium = 300 + (i % 300)
            }
        };
        sampleRecipes.Add(recipe);

        sampleIngredients.AddRange(
        [
            new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = "Nguyên liệu chính",
                Quantity = 300 + (i % 300),
                Unit = "g",
                Notes = "Rửa sạch và sơ chế trước khi dùng.",
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = "Gia vị tổng hợp",
                Quantity = 2,
                Unit = "muỗng",
                Notes = "Nêm vừa miệng.",
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = "Hành tây",
                Quantity = 1,
                Unit = "củ",
                Notes = "Bóc vỏ, thái lát.",
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = "Tỏi",
                Quantity = 3,
                Unit = "củ",
                Notes = "Băm nhỏ.",
                SortOrder = 3,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = "Cà chua",
                Quantity = 2,
                Unit = "quả",
                Notes = "Bổ múi cau hoặc cắt khối.",
                SortOrder = 4,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = "Nước tương",
                Quantity = 2,
                Unit = "muỗng canh",
                Notes = "Có thể thay bằng nước mắm.",
                SortOrder = 5,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = "Dầu ăn",
                Quantity = 2,
                Unit = "muỗng canh",
                Notes = "Dùng để phi thơm gia vị.",
                SortOrder = 6,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = "Hành lá và rau mùi",
                Quantity = 1,
                Unit = "bó",
                Notes = "Rửa sạch và thái nhỏ để trang trí.",
                SortOrder = 7,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        ]);

        for (var stepNumber = 1; stepNumber <= 5; stepNumber++)
        {
            sampleSteps.Add(new RecipeStep
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                StepNumber = stepNumber,
                Title = stepNumber switch
                {
                    1 => "Chuẩn bị nguyên liệu",
                    2 => "Sơ chế và ướp",
                    3 => "Phi thơm gia vị",
                    4 => "Chế biến món ăn",
                    _ => "Hoàn thiện và thưởng thức"
                },
                Description = stepNumber switch
                {
                    1 => "Sơ chế nguyên liệu và bố trí dụng cụ trước khi bắt đầu.",
                    2 => "Rửa sạch nguyên liệu, thái vừa miệng và ướp khoảng 10 phút với gia vị cơ bản.",
                    3 => "Làm nóng dầu, phi hành tỏi và hành tây đến khi dậy mùi thơm.",
                    4 => $"Cho nguyên liệu chính vào chảo, đảo đều khoảng {10 + (i % 8)} phút và nêm lại cho vừa khẩu vị.",
                    _ => "Bày ra đĩa, rắc hành lá và rau mùi, trang trí đẹp mắt và thưởng thức khi còn nóng."
                },
                TimerMinutes = stepNumber switch
                {
                    1 => 10,
                    2 => 10,
                    3 => 5,
                    4 => 10 + (i % 8),
                    _ => 5
                },
                ImageUrl = stepNumber == 4
                    ? $"https://images.unsplash.com/photo-1547592180-85f173990554?w=800&seed={i}"
                    : null,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        sampleImages.AddRange(
            new RecipeImage
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                OriginalUrl = $"https://images.unsplash.com/photo-1547592180-85f173990554?w=800&seed={i}",
                AltText = $"Hình ảnh {vietnameseRecipeNames[(i - 1) % vietnameseRecipeNames.Length]}",
                IsPrimary = true,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new RecipeImage
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                OriginalUrl = $"https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=800&seed={i}",
                AltText = $"Món ăn được trình bày đẹp mắt",
                IsPrimary = false,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
    }

    if (sampleRecipes.Count > 0)
    {
        db.Recipes.AddRange(sampleRecipes);
        db.RecipeIngredients.AddRange(sampleIngredients);
        db.RecipeSteps.AddRange(sampleSteps);
        db.RecipeImages.AddRange(sampleImages);
        await db.SaveChangesAsync();
        Console.WriteLine($"Đã thêm {sampleRecipes.Count} Recipe mẫu cho {sampleOwnerEmail}.");
    }
    else
    {
        Console.WriteLine($"100 Recipe mẫu cho {sampleOwnerEmail} đã tồn tại, bỏ qua tạo mới (Idempotent).");
    }

    var demoRecipeExamples = await db.Recipes
        .Where(recipe => recipe.Slug.StartsWith("mon-an-mau-"))
        .ToListAsync();

    foreach (var recipe in demoRecipeExamples)
    {
        var suffix = recipe.Slug.Replace("mon-an-mau-", string.Empty);
        var index = int.TryParse(suffix, out var parsedIndex) ? parsedIndex : 1;
        var recipeName = vietnameseRecipeNames[(index - 1) % vietnameseRecipeNames.Length];
        recipe.Title = recipeName;
        recipe.Description = "Công thức mẫu dùng để kiểm tra dữ liệu bước chế biến và hình ảnh.";
        recipe.Content = "Chuẩn bị nguyên liệu sạch, thực hiện từng bước và điều chỉnh gia vị theo khẩu vị.";
    }

    var sampleRecipesToComplete = await db.Recipes
        .Include(recipe => recipe.Ingredients)
        .Include(recipe => recipe.Steps)
        .Include(recipe => recipe.Images)
        .Where(recipe =>
            recipe.AuthorId == sampleOwner.Id &&
            recipe.Slug.StartsWith("ngocphuoc-mau-"))
        .ToListAsync();

    foreach (var recipe in sampleRecipesToComplete)
    {
        var recipeNumber = recipe.Slug.Split('-').LastOrDefault() ?? "001";
        var hasIndex = int.TryParse(recipeNumber, out var recipeIndex);
        if (!hasIndex)
        {
            recipeIndex = 1;
        }
        var recipeName = vietnameseRecipeNames[(recipeIndex - 1) % vietnameseRecipeNames.Length];
        recipe.Title = recipeName;
        recipe.Description = $"Công thức {recipeName.ToLowerInvariant()} dành cho thực hành và kiểm thử dữ liệu.";
        recipe.Content = $"Chuẩn bị nguyên liệu đầy đủ, thực hiện từng bước và điều chỉnh gia vị cho phù hợp khẩu vị của món {recipeName.ToLowerInvariant()}.";

        foreach (var ingredient in recipe.Ingredients
                     .Where(item => item.Name.StartsWith("Nguyên liệu chính ", StringComparison.OrdinalIgnoreCase)))
        {
            ingredient.Name = "Nguyên liệu chính";
        }

        foreach (var image in recipe.Images.Where(item =>
                     item.AltText != null &&
                     (item.AltText.Contains("mẫu", StringComparison.OrdinalIgnoreCase) ||
                      item.AltText.Contains("số", StringComparison.OrdinalIgnoreCase))))
        {
            image.AltText = $"Hình ảnh {recipeName}";
        }

        if (recipe.Ingredients.Count < 8)
        {
            var nextSortOrder = recipe.Ingredients.Count;
            db.RecipeIngredients.AddRange(
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(), RecipeId = recipe.Id, Name = "Hành tây",
                    Quantity = 1, Unit = "củ", Notes = "Bóc vỏ, thái lát.",
                    SortOrder = nextSortOrder++, CreatedAt = DateTime.UtcNow, IsDeleted = false
                },
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(), RecipeId = recipe.Id, Name = "Tỏi",
                    Quantity = 3, Unit = "củ", Notes = "Băm nhỏ.",
                    SortOrder = nextSortOrder++, CreatedAt = DateTime.UtcNow, IsDeleted = false
                },
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(), RecipeId = recipe.Id, Name = "Cà chua",
                    Quantity = 2, Unit = "quả", Notes = "Cắt khối.",
                    SortOrder = nextSortOrder++, CreatedAt = DateTime.UtcNow, IsDeleted = false
                },
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(), RecipeId = recipe.Id, Name = "Hành lá và rau mùi",
                    Quantity = 1, Unit = "bó", Notes = "Thái nhỏ để trang trí.",
                    SortOrder = nextSortOrder++, CreatedAt = DateTime.UtcNow, IsDeleted = false
                },
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(), RecipeId = recipe.Id, Name = "Muối",
                    Quantity = 1, Unit = "muỗng cà phê", Notes = "Điều chỉnh theo khẩu vị.",
                    SortOrder = nextSortOrder++, CreatedAt = DateTime.UtcNow, IsDeleted = false
                },
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(), RecipeId = recipe.Id, Name = "Tiêu",
                    Quantity = 1, Unit = "muỗng cà phê", Notes = "Tạo hương vị nhẹ.",
                    SortOrder = nextSortOrder++, CreatedAt = DateTime.UtcNow, IsDeleted = false
                });
        }

        if (recipe.Steps.Count < 5)
        {
            for (var stepNumber = recipe.Steps.Count + 1; stepNumber <= 5; stepNumber++)
            {
                db.RecipeSteps.Add(new RecipeStep
                {
                    Id = Guid.NewGuid(),
                    RecipeId = recipe.Id,
                    StepNumber = stepNumber,
                    Title = stepNumber switch
                    {
                        3 => "Phi thơm gia vị",
                        4 => "Chế biến món ăn",
                        _ => "Hoàn thiện và thưởng thức"
                    },
                    Description = stepNumber switch
                    {
                        3 => "Làm nóng dầu, phi hành tỏi và hành tây đến khi dậy mùi thơm.",
                        4 => "Cho nguyên liệu chính vào chảo, đảo đều và nêm lại cho vừa khẩu vị.",
                        _ => "Bày ra đĩa, trang trí đẹp mắt và thưởng thức khi còn nóng."
                    },
                    TimerMinutes = stepNumber == 4 ? 15 : 5,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }
        }

        if (recipe.Images.Count == 0)
        {
            db.RecipeImages.Add(new RecipeImage
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                OriginalUrl = $"https://images.unsplash.com/photo-1547592180-85f173990554?w=800&seed={recipeNumber}",
                AltText = $"Hình ảnh {recipe.Title}",
                IsPrimary = true,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }
    }

    await db.SaveChangesAsync();

    // =========================================================
    // 2. LẤY DANH SÁCH RECIPE
    // =========================================================

    var recipes = await db.Recipes
        .AsNoTracking()
        .ToListAsync();

    Console.WriteLine(
        $"Recipe hiện có: {recipes.Count}");

    if (recipes.Count == 0)
    {
        Console.WriteLine(
            "Không có Recipe để tạo Step và Image.");

        return;
    }

    Console.WriteLine();

    // =========================================================
    // 3. SINH RECIPE STEP BẰNG BOGUS
    // =========================================================

    Console.WriteLine(
        "Đang sinh dữ liệu RecipeStep...");

    var recipeIdsHavingSteps = await db.RecipeSteps
        .Select(x => x.RecipeId)
        .Distinct()
        .ToListAsync();

    var recipesWithoutSteps = recipes
        .Where(recipe =>
            !recipeIdsHavingSteps.Contains(recipe.Id))
        .ToList();

    if (recipesWithoutSteps.Count > 0)
    {
        var steps =
            RecipeStepSeeder.Generate(recipesWithoutSteps);

        await db.RecipeSteps.AddRangeAsync(steps);

        await db.SaveChangesAsync();

        Console.WriteLine(
            $"Đã sinh {steps.Count} RecipeStep cho " +
            $"{recipesWithoutSteps.Count} Recipe.");
    }
    else
    {
        Console.WriteLine(
            "Tất cả Recipe đã có Step.");
    }

    var stepCounts = await db.RecipeSteps
        .GroupBy(step => step.RecipeId)
        .Select(group => new { RecipeId = group.Key, Count = group.Count() })
        .ToDictionaryAsync(item => item.RecipeId, item => item.Count);

    var addedMissingStepCount = 0;
    foreach (var recipe in recipes)
    {
        stepCounts.TryGetValue(recipe.Id, out var currentStepCount);
        for (var stepNumber = currentStepCount + 1; stepNumber <= 5; stepNumber++)
        {
            db.RecipeSteps.Add(new RecipeStep
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                StepNumber = stepNumber,
                Title = stepNumber switch
                {
                    4 => "Hoàn thiện hương vị",
                    _ => "Kiểm tra và hoàn thiện món ăn"
                },
                Description = stepNumber == 4
                    ? "Nêm nếm lại, điều chỉnh gia vị và tiếp tục chế biến cho nguyên liệu chín đều."
                    : "Tắt bếp, trình bày đẹp mắt, trang trí và dùng khi còn nóng.",
                TimerMinutes = 5,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
            addedMissingStepCount++;
        }
        if (currentStepCount < 5)
        {
            stepCounts[recipe.Id] = 5;
        }
    }

    if (addedMissingStepCount > 0)
    {
        await db.SaveChangesAsync();
        Console.WriteLine($"Đã bổ sung {addedMissingStepCount} bước cho Recipe có dưới 5 Step.");
    }

    Console.WriteLine();

    // =========================================================
    // 4. SINH RECIPE IMAGE BẰNG BOGUS
    // =========================================================

    Console.WriteLine(
        "Đang sinh dữ liệu RecipeImage...");

    var recipeIdsHavingImages = await db.RecipeImages
        .Select(x => x.RecipeId)
        .Distinct()
        .ToListAsync();

    var recipesWithoutImages = recipes
        .Where(recipe =>
            !recipeIdsHavingImages.Contains(recipe.Id))
        .ToList();

    if (recipesWithoutImages.Count > 0)
    {
        var images =
            RecipeImageSeeder.Generate(recipesWithoutImages);

        await db.RecipeImages.AddRangeAsync(images);

        await db.SaveChangesAsync();

        Console.WriteLine(
            $"Đã sinh {images.Count} RecipeImage cho " +
            $"{recipesWithoutImages.Count} Recipe.");
    }
    else
    {
        Console.WriteLine(
            "Tất cả Recipe đã có Image.");
    }

    Console.WriteLine();

    // =========================================================
    // 5. KIỂM TRA KẾT QUẢ
    // =========================================================

    var recipeCount =
        await db.Recipes.CountAsync();

    var stepCount =
        await db.RecipeSteps.CountAsync();

    var imageCount =
        await db.RecipeImages.CountAsync();

    var stepStatistics = await db.RecipeSteps
        .GroupBy(x => x.RecipeId)
        .Select(group => new
        {
            RecipeId = group.Key,
            StepCount = group.Count()
        })
        .ToListAsync();

    var recipeIdsWithSteps = stepStatistics
        .Select(x => x.RecipeId)
        .ToHashSet();

    var recipesWithoutAnyStep =
        recipes.Count(recipe =>
            !recipeIdsWithSteps.Contains(recipe.Id));

    var recipesUnderFiveSteps =
        stepStatistics.Count(x => x.StepCount < 5);

    var minimumSteps =
        stepStatistics.Count > 0
            ? stepStatistics.Min(x => x.StepCount)
            : 0;

    Console.WriteLine("======================================");
    Console.WriteLine("           KẾT QUẢ SEED DATA");
    Console.WriteLine("======================================");

    Console.WriteLine(
        $"Recipes      : {recipeCount}");

    Console.WriteLine(
        $"RecipeSteps  : {stepCount}");

    Console.WriteLine(
        $"RecipeImages : {imageCount}");

    Console.WriteLine(
        $"Ít nhất Step/Recipe: {minimumSteps}");

    Console.WriteLine(
        $"Recipe không có Step: {recipesWithoutAnyStep}");

    Console.WriteLine(
        $"Recipe có dưới 5 Step: {recipesUnderFiveSteps}");

    Console.WriteLine();

    if (
        recipeCount > 0 &&
        recipesWithoutAnyStep == 0 &&
        recipesUnderFiveSteps == 0)
    {
        Console.WriteLine(
            "ĐẠT: Tất cả Recipe đều có ít nhất 5 bước chế biến.");
    }
    else
    {
        Console.WriteLine(
            "CHƯA ĐẠT: Vẫn còn Recipe chưa đủ 5 bước.");
    }

    Console.WriteLine();
    Console.WriteLine(
        "Seed database hoàn tất.");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine(
        "Seed database thất bại.");

    Console.WriteLine();
    Console.WriteLine(ex.Message);

    if (ex.InnerException is not null)
    {
        Console.WriteLine();
        Console.WriteLine(
            "Inner exception:");

        Console.WriteLine(
            ex.InnerException.Message);
    }
}