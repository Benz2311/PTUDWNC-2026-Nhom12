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

        var demoUser = new ApplicationUser
        {
            Id = demoUserId,
            UserName = "demo_user",
            Email = "demo@culinary.local",
            PasswordHash = "demo"
        };

        db.Users.Add(demoUser);

        var demoCategory = Category.Create(
            name: "Món ăn mẫu",
            slug: "mon-an-mau",
            description: "Danh mục mẫu để kiểm tra Step và Image",
            imageUrl: null,
            orderIndex: 1
        );

        db.Categories.Add(demoCategory);

        await db.SaveChangesAsync();

        for (var i = 1; i <= 10; i++)
        {
            var recipe = new Recipe
            {
                Id = Guid.NewGuid(),

                // Recipe.AuthorId hiện đang là string
                AuthorId = demoUserId.ToString(),

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

            // ===============================================
            // TEMP FIX
            // Recipe.AuthorId = string
            // ApplicationUser.Id = Guid
            //
            // EF Core đang sinh shadow FK tên AuthorId1.
            // Gán giá trị để Recipe mẫu insert được.
            // Sau khi nhóm thống nhất kiểu FK thì bỏ đoạn này.
            // ===============================================

            db.Entry(recipe)
                .Property<Guid>("AuthorId1")
                .CurrentValue = demoUserId;
        }

        await db.SaveChangesAsync();

        Console.WriteLine("Đã tạo 10 Recipe mẫu.");
        Console.WriteLine();
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