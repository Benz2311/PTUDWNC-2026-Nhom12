using Bogus;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class CategoryDataSeeder
{
    private const int SEED = 42;

    // User mẫu dùng làm Author nếu database chưa có user.
    private static readonly Guid DemoUserId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ============================================================
    // DANH SÁCH CATEGORY
    // ============================================================

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

    // ============================================================
    // DỮ LIỆU ĐỂ BOGUS SINH TÊN RECIPE
    // ============================================================

    private static readonly string[] CookingMethods =
    [
        "Kho",
        "Xào",
        "Nướng",
        "Chiên giòn",
        "Hấp",
        "Lẩu",
        "Canh",
        "Gỏi",
        "Om",
        "Rim",
        "Hầm",
        "Rang",
        "Áp chảo",
        "Chao"
    ];

    private static readonly string[] MainIngredients =
    [
        "Gà Ta",
        "Thịt Bò",
        "Ba Chỉ Heo",
        "Tôm Sú",
        "Cá Hồi",
        "Cá Lóc",
        "Mực Ống",
        "Sườn Non",
        "Đậu Hũ Non",
        "Nấm Đùi Gà",
        "Bạch Tuộc",
        "Cá Bống",
        "Vịt Cỏ",
        "Cua Biển",
        "Lươn Đồng",
        "Ốc Bươu",
        "Bò Tơ",
        "Cá Chẽm"
    ];

    private static readonly string[] FlavorsAndSpices =
    [
        "Sả Ớt",
        "Chua Ngọt",
        "Tiêu Xanh",
        "Ngũ Vị Hương",
        "Nước Dừa Xiêm",
        "Gừng Hành",
        "Sa Tế Cay Nồng",
        "Mật Ong Rừng",
        "Tỏi Ớt Đậm Đà",
        "Lá É",
        "Lá Giang",
        "Sốt Me Chua Cay",
        "Phô Mai Béo Ngậy",
        "Trứng Muối Thơm Bùi",
        "Mỡ Hành Đậu Phộng",
        "Cốt Dừa Béo Ngậy"
    ];

    private static readonly string[] RegionsAndStyles =
    [
        "Chuẩn Vị Hà Nội",
        "Đặc Sản Miền Tây",
        "Phong Cách Nam Bộ",
        "Chuẩn Vị Xứ Huế",
        "Hương Vị Tây Bắc",
        "Gia Truyền",
        "Thanh Đạm",
        "Đậm Đà Chuẩn Cơm Mẹ Nấu"
    ];

    // ============================================================
    // MÓN ĂN ƯU TIÊN THEO CATEGORY
    // ============================================================

    private static readonly Dictionary<string, string[]>
        CategorySpecificDishes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Món Canh"] =
                [
                    "Canh Chua Cá Lóc Nam Bộ",
                    "Canh Cua Rau Đay Mồng Tơi",
                    "Canh Sườn Hầm Rau Củ Quả",
                    "Canh Khổ Qua Nhồi Thịt",
                    "Canh Rong Biển Đậu Hũ Non",
                    "Canh Ngao Nấu Chua Thì Là"
                ],

                ["Đồ Uống"] =
                [
                    "Trà Đào Cam Sả Tươi Mát",
                    "Trà Sữa Trân Châu Đường Đen",
                    "Sinh Tố Bơ Sáp Sầu Riêng",
                    "Nước Ép Dưa Hấu Bạc Hà",
                    "Cà Phê Muối Xứ Huế",
                    "Trà Hoa Cúc Mật Ong Hạt Chia"
                ],

                ["Bún, Mì & Phở"] =
                [
                    "Phở Bò Truyền Thống Hà Nội",
                    "Bún Bò Huế Chuẩn Vị Cố Đô",
                    "Mì Quảng Tôm Thịt Đậm Đà",
                    "Bún Chả Hà Nội Nướng Than Hoa",
                    "Hủ Tiếu Nam Vang Sườn Tôm",
                    "Bún Riêu Cua Đồng Bắp Bò"
                ],

                ["Món Tráng Miệng"] =
                [
                    "Chè Bưởi An Giang Giòn Sần Sật",
                    "Bánh Flan Caramel Cốt Dừa",
                    "Chè Khúc Bạch Hạnh Nhân Hạt É",
                    "Bánh Pía Sóc Trăng Trứng Muối",
                    "Chè Dưỡng Nhan Tuyết Yến Hạt Sen",
                    "Panna Cotta Sốt Xoài Dẻo"
                ],

                ["Ăn Vặt"] =
                [
                    "Bánh Tráng Trộn Long An",
                    "Chân Gà Sả Tắc Giòn Sần Sật",
                    "Bắp Xào Bơ Tép Sấy",
                    "Khoai Lang Kén Chiên Giòn",
                    "Bánh Tráng Nướng Mỡ Hành Đà Lạt",
                    "Nem Chua Rán Hà Nội"
                ],

                ["Món Chay Thanh Đạm"] =
                [
                    "Đậu Hũ Non Sốt Nấm Đông Cô",
                    "Canh Nấm Hạt Sen Thanh Tịnh",
                    "Rau Củ Luộc Kho Quẹt Chay",
                    "Gỏi Cuốn Nấm Đậu Hũ Chay",
                    "Nấm Đùi Gà Kho Tiêu Xanh Chay",
                    "Cà Tím Nướng Mỡ Hành Chay"
                ],

                ["Món Nướng & BBQ"] =
                [
                    "Sườn Cây Nướng Sốt BBQ",
                    "Bò Cuộn Nấm Kim Châm Nướng Than",
                    "Gà Nướng Muối Ớt Tây Bắc",
                    "Bạch Tuộc Nướng Sa Tế Cay",
                    "Hàu Nướng Mỡ Hành Đậu Phộng",
                    "Ba Chỉ Heo Nướng Riềng Mẻ"
                ],

                ["Món Lẩu & Nước Dùng"] =
                [
                    "Lẩu Cá Kèo Lá Giang Miền Tây",
                    "Lẩu Bò Nhúng Dấm Chua Thanh",
                    "Lẩu Thái Hải Sản Chua Cay",
                    "Lẩu Gà Lá É Phú Yên",
                    "Lẩu Nấm Chim Câu Bổ Dưỡng",
                    "Lẩu Riêu Cua Bắp Bò Sườn Sụn"
                ],

                ["Gỏi & Salad"] =
                [
                    "Gỏi Ngó Sen Tôm Thịt",
                    "Gỏi Bò Bóp Thấu Hành Tây",
                    "Salad Ức Gà Sốt Mè Rang",
                    "Gỏi Gà Xé Phay Bắp Cải",
                    "Gỏi Xoài Xanh Cá Sặc Giòn",
                    "Salad Bơ Trứng Dầu Giấm"
                ],

                ["Bánh Truyền Thống"] =
                [
                    "Bánh Xèo Miền Tây Giòn Rụm",
                    "Bánh Bèo Chén Tôm Chấy Xứ Huế",
                    "Bánh Bột Lọc Gói Lá Chuối",
                    "Bánh Cuốn Nóng Thịt Băm Mộc Nhĩ",
                    "Bánh Chưng Tranh Khúc",
                    "Bánh Đúc Nóng Thịt Băm"
                ],

                ["Nước Chấm & Sốt"] =
                [
                    "Nước Mắm Chua Ngọt Tỏi Ớt",
                    "Muối Ớt Xanh Chấm Hải Sản Nha Trang",
                    "Kho Quẹt Tôm Khô Thịt Ba Chỉ",
                    "Sốt Me Chua Ngọt Rang Tôm",
                    "Sốt Thái Trộn Chân Gà Cay",
                    "Mắm Nêm Pha Dứa Chấm Bò Cuốn"
                ],

                ["Món Ăn Sáng"] =
                [
                    "Cơm Tấm Sườn Bì Chả Sài Gòn",
                    "Bánh Mì Thịt Nướng Bơ Thơm",
                    "Xôi Xéo Gà Xé Hành Phi Vàng",
                    "Cháo Sườn Quẩy Giòn Hà Nội",
                    "Bánh Cuốn Thanh Trì Chả Lụa",
                    "Mì Hoành Thánh Xá Xíu"
                ],

                ["Eat Clean & Dinh Dưỡng"] =
                [
                    "Ức Gà Áp Chảo Măng Tây",
                    "Cá Hồi Nướng Bơ Chanh",
                    "Salad Tôm Bơ Hạt Diêm Mạch",
                    "Thịt Bò Áp Chảo Bông Cải Xanh",
                    "Khoai Lang Nướng Hạt Chia Yến Mạch",
                    "Trứng Cuộn Rau Củ Eat Clean"
                ],

                ["Món Nhậu & Lai Rai"] =
                [
                    "Bò Bóp Thấu Thính Gạo Thơm",
                    "Mực Trứng Chiên Nước Mắm",
                    "Gà Hấp Lá Chanh Hoa Tiêu",
                    "Lòng Xào Dưa Chua Giòn Sần Sật",
                    "Cánh Gà Chiên Bơ Tỏi",
                    "Gân Bò Trộn Cóc Non Sốt Thái"
                ]
            };

    // ============================================================
    // HÀM SEED CHÍNH
    // ============================================================

    public static async Task SeedAsync(
        ApplicationDbContext context,
        int targetCount = 20,
        int minRecipesPerCategory = 3)
    {
        var faker = new Faker("vi");
        faker.Random = new Randomizer(SEED);

        // ========================================================
        // 0. ĐẢM BẢO CÓ USER ĐỂ LÀM AUTHOR CHO RECIPE
        // ========================================================

        var author = await context.Users
            .FirstOrDefaultAsync();

        if (author is null)
        {
            author = new ApplicationUser
            {
                Id = DemoUserId,
                UserName = "culinary-demo",
                Email = "demo@culinaryblog.local",
                PasswordHash = "seeded-demo-password"
            };

            context.Users.Add(author);

            await context.SaveChangesAsync();
        }

        // ========================================================
        // 1. SEED CATEGORY
        // Đảm bảo database có ít nhất targetCount category.
        // ========================================================

        var existingCategories = await context.Categories
            .IgnoreQueryFilters()
            .ToListAsync();

        var existingCategoryNames =
            new HashSet<string>(
                existingCategories.Select(
                    c => c.Name.Trim().ToLowerInvariant()));

        var existingCategorySlugs =
            new HashSet<string>(
                existingCategories.Select(
                    c => c.Slug.Trim().ToLowerInvariant()));

        var categoriesToAdd = new List<Category>();

        var orderIndex = existingCategories.Any()
            ? existingCategories.Max(c => c.OrderIndex) + 1
            : 1;

        if (existingCategories.Count < targetCount)
        {
            var neededCategories =
                targetCount - existingCategories.Count;

            // ====================================================
            // 1.1. Ưu tiên danh sách category định nghĩa sẵn
            // ====================================================

            foreach (var categoryName in PredefinedCategoryPool)
            {
                if (categoriesToAdd.Count >= neededCategories)
                {
                    break;
                }

                var nameLower =
                    categoryName.Trim().ToLowerInvariant();

                if (existingCategoryNames.Contains(nameLower))
                {
                    continue;
                }

                var slug = GenerateSlug(categoryName);

                var uniqueSlug = MakeUniqueSlug(
                    slug,
                    existingCategorySlugs);

                var description =
                    $"Khám phá tuyển tập các công thức chế biến " +
                    $"{categoryName.ToLowerInvariant()} " +
                    $"thơm ngon, chuẩn vị gia đình.";

                var imageUrl =
                    $"https://picsum.photos/seed/{uniqueSlug}/600/400";

                var category = Category.Create(
                    name: categoryName,
                    slug: uniqueSlug,
                    description: description,
                    imageUrl: imageUrl,
                    orderIndex: orderIndex++);

                categoriesToAdd.Add(category);

                existingCategoryNames.Add(nameLower);
                existingCategorySlugs.Add(uniqueSlug);
            }

            // ====================================================
            // 1.2. Nếu vẫn thiếu category thì dùng Bogus tạo thêm
            // ====================================================

            while (categoriesToAdd.Count < neededCategories)
            {
                var randomAdj =
                    faker.Commerce.ProductAdjective();

                var randomName =
                    $"Món ngon {randomAdj} " +
                    $"{categoriesToAdd.Count + 1}";

                var nameLower =
                    randomName.ToLowerInvariant();

                if (existingCategoryNames.Contains(nameLower))
                {
                    continue;
                }

                var slug = GenerateSlug(randomName);

                var uniqueSlug = MakeUniqueSlug(
                    slug,
                    existingCategorySlugs);

                var description =
                    faker.Lorem.Sentence(8);

                var imageUrl =
                    $"https://picsum.photos/seed/{uniqueSlug}/600/400";

                var category = Category.Create(
                    name: randomName,
                    slug: uniqueSlug,
                    description: description,
                    imageUrl: imageUrl,
                    orderIndex: orderIndex++);

                categoriesToAdd.Add(category);

                existingCategoryNames.Add(nameLower);
                existingCategorySlugs.Add(uniqueSlug);
            }

            if (categoriesToAdd.Any())
            {
                await context.Categories
                    .AddRangeAsync(categoriesToAdd);

                await context.SaveChangesAsync();
            }
        }

        // ========================================================
        // 2. SEED RECIPE
        // Đảm bảo mỗi Category có minRecipesPerCategory Recipe.
        // ========================================================

        var allCategories = await context.Categories
            .IgnoreQueryFilters()
            .ToListAsync();

        var existingRecipes = await context.Recipes
            .IgnoreQueryFilters()
            .ToListAsync();

        var existingRecipeSlugs =
            new HashSet<string>(
                existingRecipes.Select(
                    r => r.Slug.Trim().ToLowerInvariant()));

        var recipesToAdd = new List<Recipe>();

        foreach (var category in allCategories)
        {
            var categoryRecipeCount =
                existingRecipes.Count(
                    r => r.CategoryId == category.Id)
                +
                recipesToAdd.Count(
                    r => r.CategoryId == category.Id);

            var recipesNeeded =
                minRecipesPerCategory - categoryRecipeCount;

            if (recipesNeeded <= 0)
            {
                continue;
            }

            // Nếu Category có danh sách món riêng thì ưu tiên dùng.
            CategorySpecificDishes.TryGetValue(
                category.Name,
                out var specificDishes);

            var dishIndex = 0;

            for (var i = 0; i < recipesNeeded; i++)
            {
                string title;

                if (specificDishes is not null
                    && dishIndex < specificDishes.Length)
                {
                    title = specificDishes[dishIndex++];
                }
                else
                {
                    var method =
                        faker.PickRandom(CookingMethods);

                    var ingredient =
                        faker.PickRandom(MainIngredients);

                    var flavor =
                        faker.PickRandom(FlavorsAndSpices);

                    title =
                        $"{method} {ingredient} {flavor}";

                    if (faker.Random.Bool(0.35f))
                    {
                        title +=
                            $" {faker.PickRandom(RegionsAndStyles)}";
                    }
                }

                // =================================================
                // SLUG RECIPE
                // =================================================

                var slug = GenerateSlug(title);

                var uniqueSlug = MakeUniqueSlug(
                    slug,
                    existingRecipeSlugs);

                // =================================================
                // DỮ LIỆU NGẪU NHIÊN
                // =================================================

                var description =
                    $"Hướng dẫn từng bước cách làm món {title} " +
                    $"chuẩn vị, màu sắc bắt mắt và đậm đà " +
                    $"hương thơm truyền thống.";

                var prepTime =
                    faker.Random.Int(10, 45);

                var cookTime =
                    faker.Random.Int(15, 120);

                var servings =
                    faker.Random.Int(2, 6);

                var difficulty =
                    faker.PickRandom<DifficultyLevel>();

                var status =
                    faker.Random.WeightedRandom(
                        [
                            RecipeStatus.Published,
                            RecipeStatus.Draft
                        ],
                        [
                            0.85f,
                            0.15f
                        ]);

                // =================================================
                // TẠO RECIPE
                // Recipe hiện tại không có constructor custom,
                // nên dùng Object Initializer.
                // =================================================

                var recipe = new Recipe
                {
                    AuthorId = author.Id,

                    CategoryId = category.Id,

                    Title = title,

                    Slug = uniqueSlug,

                    Description = description,

                    Content =
                        $"Chuẩn bị đầy đủ nguyên liệu cho món {title}. " +
                        $"Sơ chế nguyên liệu, thực hiện lần lượt " +
                        $"các bước chế biến và nêm nếm theo khẩu vị.",

                    PrepTimeMinutes = prepTime,

                    CookTimeMinutes = cookTime,

                    Servings = servings,

                    Difficulty = difficulty,

                    Status = status,

                    PublishedAt =
                        status == RecipeStatus.Published
                            ? faker.Date
                                .Recent(180)
                                .ToUniversalTime()
                            : null,

                    Nutrition = new RecipeNutrition(),

                    IsDeleted = false,

                    RowVersion = new byte[8]
                };

                recipesToAdd.Add(recipe);

                existingRecipeSlugs.Add(uniqueSlug);
            }
        }

        if (recipesToAdd.Any())
        {
            await context.Recipes
                .AddRangeAsync(recipesToAdd);

            await context.SaveChangesAsync();
        }
    }

    // ============================================================
    // HELPER: TẠO SLUG
    // Ví dụ:
    // "Bún Bò Huế" -> "bun-bo-hue"
    // ============================================================

    private static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Guid.NewGuid()
                .ToString("N")
                .ToLowerInvariant();
        }

        // Xử lý riêng ký tự đ/Đ vì Normalize không bỏ được.
        text = text
            .Replace('đ', 'd')
            .Replace('Đ', 'D');

        var normalized =
            text.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(character);

            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();

        // Mọi ký tự không phải chữ/số -> dấu -
        slug = Regex.Replace(
            slug,
            @"[^a-z0-9]+",
            "-");

        // Xóa dấu - đầu/cuối
        slug = slug.Trim('-');

        // Tránh slug rỗng.
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = Guid.NewGuid()
                .ToString("N")
                .ToLowerInvariant();
        }

        return slug;
    }

    // ============================================================
    // HELPER: ĐẢM BẢO SLUG KHÔNG TRÙNG
    //
    // Ví dụ:
    // pho-bo
    // pho-bo-1
    // pho-bo-2
    // ============================================================

    private static string MakeUniqueSlug(
        string baseSlug,
        HashSet<string> existingSlugs)
    {
        var uniqueSlug = baseSlug;
        var suffix = 1;

        while (existingSlugs.Contains(
                   uniqueSlug.ToLowerInvariant()))
        {
            uniqueSlug =
                $"{baseSlug}-{suffix++}";
        }

        return uniqueSlug;
    }
}