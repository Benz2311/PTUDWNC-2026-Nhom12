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
    // =========================================================
    // 1. KIỂM TRA KẾT NỐI POSTGRESQL
    // =========================================================

    Console.WriteLine(
        "Đang kiểm tra kết nối PostgreSQL...");

    if (!await db.Database.CanConnectAsync())
    {
        Console.WriteLine(
            "Không thể kết nối PostgreSQL.");

        return;
    }

    Console.WriteLine(
        "Kết nối PostgreSQL thành công.");

    Console.WriteLine();

    // =========================================================
    // 2. SEED CATEGORY + RECIPE BẰNG BOGUS
    // =========================================================

    Console.WriteLine(
        "Đang sinh dữ liệu Category và Recipe bằng Bogus...");

    await CategoryDataSeeder.SeedAsync(
        db,
        targetCount: 20,
        minRecipesPerCategory: 3);

    Console.WriteLine(
        "Hoàn tất seed Category và Recipe.");

    Console.WriteLine();

    // =========================================================
    // 3. LẤY DANH SÁCH RECIPE SAU KHI ĐÃ SEED
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
    // 4. SINH RECIPE STEP
    // =========================================================

    Console.WriteLine(
        "Đang sinh dữ liệu RecipeStep...");

    var recipeIdsHavingSteps =
        await db.RecipeSteps
            .Select(x => x.RecipeId)
            .Distinct()
            .ToListAsync();

    var recipesWithoutSteps =
        recipes
            .Where(
                recipe =>
                    !recipeIdsHavingSteps
                        .Contains(recipe.Id))
            .ToList();

    if (recipesWithoutSteps.Count > 0)
    {
        var steps =
            RecipeStepSeeder.Generate(
                recipesWithoutSteps);

        await db.RecipeSteps
            .AddRangeAsync(steps);

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
    // 5. SINH RECIPE IMAGE
    // =========================================================

    Console.WriteLine(
        "Đang sinh dữ liệu RecipeImage...");

    var recipeIdsHavingImages =
        await db.RecipeImages
            .Select(x => x.RecipeId)
            .Distinct()
            .ToListAsync();

    var recipesWithoutImages =
        recipes
            .Where(
                recipe =>
                    !recipeIdsHavingImages
                        .Contains(recipe.Id))
            .ToList();

    if (recipesWithoutImages.Count > 0)
    {
        var images =
            RecipeImageSeeder.Generate(
                recipesWithoutImages);

        await db.RecipeImages
            .AddRangeAsync(images);

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
    // 6. KIỂM TRA KẾT QUẢ SAU KHI SEED
    // =========================================================

    var categoryCount =
        await db.Categories.CountAsync();

    var recipeCount =
        await db.Recipes.CountAsync();

    var stepCount =
        await db.RecipeSteps.CountAsync();

    var imageCount =
        await db.RecipeImages.CountAsync();

    // =========================================================
    // THỐNG KÊ STEP THEO RECIPE
    // =========================================================

    var stepStatistics =
        await db.RecipeSteps
            .GroupBy(x => x.RecipeId)
            .Select(
                group => new
                {
                    RecipeId = group.Key,
                    StepCount = group.Count()
                })
            .ToListAsync();

    var recipeIdsWithSteps =
        stepStatistics
            .Select(x => x.RecipeId)
            .ToHashSet();

    var recipesWithoutAnyStep =
        recipeCount == 0
            ? 0
            : recipes.Count(
                recipe =>
                    !recipeIdsWithSteps
                        .Contains(recipe.Id));

    var recipesUnderFiveSteps =
        stepStatistics.Count(
            x => x.StepCount < 5);

    var minimumSteps =
        stepStatistics.Count > 0
            ? stepStatistics.Min(
                x => x.StepCount)
            : 0;

    // =========================================================
    // THỐNG KÊ IMAGE THEO RECIPE
    // =========================================================

    var imageStatistics =
        await db.RecipeImages
            .GroupBy(x => x.RecipeId)
            .Select(
                group => new
                {
                    RecipeId = group.Key,
                    ImageCount = group.Count()
                })
            .ToListAsync();

    var recipeIdsWithImages =
        imageStatistics
            .Select(x => x.RecipeId)
            .ToHashSet();

    var recipesWithoutAnyImage =
        recipeCount == 0
            ? 0
            : recipes.Count(
                recipe =>
                    !recipeIdsWithImages
                        .Contains(recipe.Id));

    var minimumImages =
        imageStatistics.Count > 0
            ? imageStatistics.Min(
                x => x.ImageCount)
            : 0;

    // =========================================================
    // 7. IN KẾT QUẢ
    // =========================================================

    Console.WriteLine(
        "======================================");

    Console.WriteLine(
        "           KẾT QUẢ SEED DATA");

    Console.WriteLine(
        "======================================");

    Console.WriteLine(
        $"Categories   : {categoryCount}");

    Console.WriteLine(
        $"Recipes      : {recipeCount}");

    Console.WriteLine(
        $"RecipeSteps  : {stepCount}");

    Console.WriteLine(
        $"RecipeImages : {imageCount}");

    Console.WriteLine();

    Console.WriteLine(
        $"Ít nhất Step/Recipe : {minimumSteps}");

    Console.WriteLine(
        $"Recipe không có Step: {recipesWithoutAnyStep}");

    Console.WriteLine(
        $"Recipe có dưới 5 Step: {recipesUnderFiveSteps}");

    Console.WriteLine();

    Console.WriteLine(
        $"Ít nhất Image/Recipe: {minimumImages}");

    Console.WriteLine(
        $"Recipe không có Image: {recipesWithoutAnyImage}");

    Console.WriteLine();

    // =========================================================
    // 8. KIỂM TRA YÊU CẦU
    // =========================================================

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

    if (
        recipeCount > 0 &&
        recipesWithoutAnyImage == 0)
    {
        Console.WriteLine(
            "ĐẠT: Tất cả Recipe đều có ít nhất 1 Image.");
    }
    else
    {
        Console.WriteLine(
            "CHƯA ĐẠT: Vẫn còn Recipe chưa có Image.");
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

    Console.WriteLine(
        ex.Message);

    if (ex.InnerException is not null)
    {
        Console.WriteLine();

        Console.WriteLine(
            "Inner exception:");

        Console.WriteLine(
            ex.InnerException.Message);
    }
}