using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("======================================");
Console.WriteLine("     CULINARY BLOG DATABASE SEEDER");
Console.WriteLine("======================================");
Console.WriteLine();

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
                Email = "demo@culinary.local",
                PasswordHash = "demo"
            };
            db.Users.Add(demoUser);
        }
        else
        {
            demoUserId = demoUser.Id;
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

                Title = $"Món ăn mẫu {i}",
                Slug = $"mon-an-mau-{i}",

                Description =
                    $"Recipe mẫu số {i} dùng để kiểm tra dữ liệu Step và Image.",

                Content =
                    $"Nội dung Recipe mẫu số {i}.",

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