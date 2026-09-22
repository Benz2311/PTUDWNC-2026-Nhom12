using Bogus;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class CategoryDataSeeder
{
    private const int SEED = 42;

    // Danh sách tên danh mục ẩm thực phong phú và thực tế
    private static readonly string[] PredefinedCategoryPool =
    [
        "Món Khai Vị",
        "Món Nướng & BBQ",
        "Món Kho & Rim",
        "Món Xào",
        "Món Chiên & Rán",
        "Món Hấp & Luộc",
        "Món Lẩu & Nước Dùng",
        "Bún, Mì & Phở",
        "Gỏi & Salad",
        "Bánh Truyền Thống",
        "Nước Chấm & Sốt",
        "Món Ăn Sáng",
        "Món Chay Thanh Đạm",
        "Eat Clean & Dinh Dưỡng",
        "Món Nhậu & Lai Rai",
        "Đồ Ngâm & Dưa Muối",
        "Ẩm Thực Miền Bắc",
        "Ẩm Thực Miền Trung",
        "Ẩm Thực Miền Nam",
        "Món Nhanh Dễ Làm",
        "Món Hầm & Tiềm Bổ Dưỡng",
        "Hải Sản Tươi Sống",
        "Bánh Ngọt & Bakery",
        "Thực Đơn Gia Đình"
    ];

    public static async Task SeedAsync(ApplicationDbContext context, int targetCount = 20)
    {
        // Kiểm tra số lượng danh mục hiện có trong cơ sở dữ liệu
        var existingCategories = await context.Categories
            .IgnoreQueryFilters()
            .ToListAsync();

        var existingNames = new HashSet<string>(
            existingCategories.Select(c => c.Name.Trim().ToLowerInvariant()));

        var existingSlugs = new HashSet<string>(
            existingCategories.Select(c => c.Slug.Trim().ToLowerInvariant()));

        var currentCount = existingCategories.Count;
        if (currentCount >= targetCount)
        {
            return;
        }

        var faker = new Faker("vi");
        faker.Random = new Randomizer(SEED);

        var neededCount = targetCount - currentCount;
        var categoriesToAdd = new List<Category>();

        var orderIndex = existingCategories.Any()
            ? existingCategories.Max(c => c.OrderIndex) + 1
            : 1;

        // 1. Lấy từ pool danh mục thực tế trước
        foreach (var categoryName in PredefinedCategoryPool)
        {
            if (categoriesToAdd.Count >= neededCount)
            {
                break;
            }

            var nameLower = categoryName.Trim().ToLowerInvariant();
            if (existingNames.Contains(nameLower))
            {
                continue;
            }

            var slug = SlugHelper.Generate(categoryName);
            var uniqueSlug = slug;
            var suffix = 1;
            while (existingSlugs.Contains(uniqueSlug))
            {
                uniqueSlug = $"{slug}-{suffix++}";
            }

            var description = $"Khám phá tuyển tập các công thức chế biến {categoryName.ToLower()} thơm ngon, chuẩn vị gia đình.";
            var imageUrl = $"https://picsum.photos/seed/{uniqueSlug}/600/400";

            var category = Category.Create(
                name: categoryName,
                slug: uniqueSlug,
                description: description,
                imageUrl: imageUrl,
                orderIndex: orderIndex++);

            categoriesToAdd.Add(category);
            existingNames.Add(nameLower);
            existingSlugs.Add(uniqueSlug);
        }

        // 2. Nếu vẫn chưa đủ targetCount, dùng Bogus sinh thêm danh mục ngẫu nhiên
        while (categoriesToAdd.Count < neededCount)
        {
            var randomNoun = faker.Commerce.ProductAdjective();
            var randomCategoryName = $"Món ngon {randomNoun} {categoriesToAdd.Count + 1}";
            var nameLower = randomCategoryName.ToLowerInvariant();

            if (existingNames.Contains(nameLower))
            {
                continue;
            }

            var slug = SlugHelper.Generate(randomCategoryName);
            var uniqueSlug = slug;
            var suffix = 1;
            while (existingSlugs.Contains(uniqueSlug))
            {
                uniqueSlug = $"{slug}-{suffix++}";
            }

            var description = faker.Lorem.Sentence(8);
            var imageUrl = $"https://picsum.photos/seed/{uniqueSlug}/600/400";

            var category = Category.Create(
                name: randomCategoryName,
                slug: uniqueSlug,
                description: description,
                imageUrl: imageUrl,
                orderIndex: orderIndex++);

            categoriesToAdd.Add(category);
            existingNames.Add(nameLower);
            existingSlugs.Add(uniqueSlug);
        }

        if (categoriesToAdd.Any())
        {
            await context.Categories.AddRangeAsync(categoriesToAdd);
            await context.SaveChangesAsync();
        }

        // 3. Nếu chưa có Recipe nào trong database, tạo một số Recipe mẫu phân bổ vào các danh mục
        // để API thống kê (Category & Statistics) hiển thị số liệu thực tế
        if (!await context.Recipes.IgnoreQueryFilters().AnyAsync())
        {
            await SeedSampleRecipesAsync(context);
        }
    }

    private static async Task SeedSampleRecipesAsync(ApplicationDbContext context)
    {
        var allCategories = await context.Categories
            .IgnoreQueryFilters()
            .ToListAsync();

        if (!allCategories.Any())
        {
            return;
        }

        var sampleRecipesData = new (string Title, string Slug, string Description, int Prep, int Cook, int Servings, DifficultyLevel Difficulty, RecipeStatus Status)[]
        {
            ("Phở Bò Truyền Thống Hà Nội", "pho-bo-truyen-thong-ha-noi", "Hương vị phở bò nước dùng trong veo, đậm đà hoa hồi quế chi", 30, 180, 4, DifficultyLevel.Hard, RecipeStatus.Published),
            ("Bún Chả Hà Nội Nướng Than Hoa", "bun-cha-ha-noi-nuong-than-hoa", "Chả viên và chả miếng nướng xém cạnh ăn kèm nước mắm chua ngọt", 45, 30, 4, DifficultyLevel.Medium, RecipeStatus.Published),
            ("Cơm Tấm Sườn Bì Chả Sài Gòn", "com-tam-suon-bi-cha-sai-gon", "Hương vị đặc trưng cơm tấm sườn nướng mật ong thơm lừng", 40, 40, 3, DifficultyLevel.Medium, RecipeStatus.Published),
            ("Canh Chua Cá Lóc Nam Bộ", "canh-chua-ca-loc-nam-bo", "Vị chua thanh của me, giòn ngọt của bạc hà và thơm", 20, 25, 4, DifficultyLevel.Easy, RecipeStatus.Published),
            ("Gỏi Cuốn Tôm Thịt Chấm Tương Bơ", "goi-cuon-tom-thit-tuong-bo", "Món ăn thanh mát nhiều rau xanh ăn kèm tương đen pha đậu phộng", 25, 15, 4, DifficultyLevel.Easy, RecipeStatus.Published),
            ("Bánh Xèo Miền Tây Giòn Rụm", "banh-xeo-mien-tay-gion-rum", "Vỏ bánh vàng ươm nghệ tây, nhân tôm thịt giá đỗ đượm vị", 30, 30, 5, DifficultyLevel.Medium, RecipeStatus.Published),
            ("Chè Bưởi An Giang Giòn Sần Sật", "che-buoi-an-giang", "Cùi bưởi khử đắng khéo léo kết hợp đậu xanh và nước cốt dừa", 60, 45, 6, DifficultyLevel.Hard, RecipeStatus.Published),
            ("Trà Đào Cam Sả Tươi Mát", "tra-dao-cam-sa-tuoi-mat", "Thức uống giải nhiệt mùa hè thơm nồng hương sả và cam tươi", 15, 10, 2, DifficultyLevel.Easy, RecipeStatus.Published),
            ("Nem Rán Hà Nội Giòn Tan", "nem-ran-ha-noi-gion-tan", "Vỏ đa nem giòn rụm nhân thịt băm, nấm hương, mộc nhĩ", 35, 25, 6, DifficultyLevel.Medium, RecipeStatus.Draft),
            ("Thịt Kho Tàu Nước Dừa", "thit-kho-tau-nuoc-dua", "Thịt ba chỉ mềm rục ngấm nước dừa xiêm cùng trứng vịt bùi ngậy", 20, 90, 5, DifficultyLevel.Medium, RecipeStatus.Published)
        };

        var recipesToAdd = new List<Recipe>();
        for (int i = 0; i < sampleRecipesData.Length; i++)
        {
            var data = sampleRecipesData[i];
            var category = allCategories[i % allCategories.Count];

            var recipe = new Recipe(
                title: data.Title,
                slug: data.Slug,
                description: data.Description,
                prepTimeMinutes: data.Prep,
                cookTimeMinutes: data.Cook,
                servings: data.Servings,
                difficulty: data.Difficulty,
                status: data.Status,
                categoryId: category.Id
            );

            recipesToAdd.Add(recipe);
        }

        await context.Recipes.AddRangeAsync(recipesToAdd);
        await context.SaveChangesAsync();
    }
}
