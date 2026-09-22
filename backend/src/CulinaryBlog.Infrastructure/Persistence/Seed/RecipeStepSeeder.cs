using Bogus;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class RecipeStepSeeder
{
    public static List<RecipeStep> Generate(IEnumerable<Recipe> recipes)
    {
        var result = new List<RecipeStep>();
        var faker = new Faker("vi");

        foreach (var recipe in recipes)
        {
            var ingredients = recipe.Ingredients
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ToList();

            var ingredientNames = ingredients
                .Select(x => x.Name.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var steps = BuildSteps(
                recipe.Title,
                ingredientNames,
                faker);

            for (var index = 0; index < steps.Count; index++)
            {
                result.Add(new RecipeStep
                {
                    Id = faker.Random.Guid(),
                    RecipeId = recipe.Id,
                    StepNumber = index + 1,
                    Title = steps[index].Title,
                    Description = steps[index].Description,
                    CreatedAt = faker.Date
                        .Recent(30)
                        .ToUniversalTime(),
                    UpdatedAt = null,
                    IsDeleted = false,
                    RowVersion = new byte[8]
                });
            }
        }

        return result;
    }

    private static List<(string Title, string Description)> BuildSteps(
        string recipeTitle,
        List<string> ingredientNames,
        Faker faker)
    {
        var steps = new List<(string Title, string Description)>();

        var title = recipeTitle.ToLowerInvariant();

        var mainIngredients = ingredientNames
            .Take(4)
            .ToList();

        var ingredientText = FormatIngredients(mainIngredients);

        // =====================================================
        // BƯỚC 1 - CHUẨN BỊ
        // =====================================================

        steps.Add((
            "Chuẩn bị nguyên liệu",
            ingredientNames.Count > 0
                ? $"Chuẩn bị {ingredientText}. Kiểm tra và sơ chế nguyên liệu trước khi bắt đầu chế biến."
                : $"Chuẩn bị đầy đủ nguyên liệu cần thiết để thực hiện món {recipeTitle}."
        ));

        // =====================================================
        // BƯỚC 2 - SƠ CHẾ
        // =====================================================

        steps.Add((
            "Sơ chế nguyên liệu",
            BuildPreparationDescription(
                ingredientNames,
                faker)
        ));

        // =====================================================
        // BƯỚC 3 - XỬ LÝ THEO LOẠI MÓN
        // =====================================================

        if (ContainsAny(
                title,
                "mì",
                "bún",
                "phở",
                "miến",
                "hủ tiếu",
                "pasta"))
        {
            steps.Add((
                "Chuẩn bị phần sợi",
                $"Đun nước sôi, cho phần sợi của món {recipeTitle} vào nấu khoảng " +
                $"{faker.Random.Int(2, 6)} phút. Vớt ra và để ráo."
            ));
        }
        else if (ContainsAny(
                     title,
                     "cơm",
                     "cháo"))
        {
            steps.Add((
                "Chuẩn bị phần gạo",
                $"Vo sạch gạo, thêm lượng nước phù hợp và nấu đến khi đạt độ chín cần thiết cho món {recipeTitle}."
            ));
        }
        else if (ContainsAny(
                     title,
                     "canh",
                     "súp",
                     "lẩu"))
        {
            steps.Add((
                "Chuẩn bị nước dùng",
                $"Đun lượng nước vừa đủ đến khi sôi, sau đó chuẩn bị cho các nguyên liệu chính vào nấu."
            ));
        }
        else if (ContainsAny(
                     title,
                     "nướng"))
        {
            steps.Add((
                "Làm nóng dụng cụ nướng",
                $"Làm nóng lò hoặc bếp nướng ở nhiệt độ khoảng {faker.Random.Int(170, 210)}°C trước khi cho nguyên liệu vào."
            ));
        }
        else if (ContainsAny(
                     title,
                     "chiên",
                     "rán"))
        {
            steps.Add((
                "Làm nóng dầu",
                "Cho dầu vào chảo và làm nóng ở lửa vừa trước khi cho nguyên liệu vào chiên."
            ));
        }
        else
        {
            steps.Add((
                "Chuẩn bị chế biến",
                $"Làm nóng chảo hoặc nồi ở lửa vừa, sau đó chuẩn bị cho nguyên liệu chính vào chế biến."
            ));
        }

        // =====================================================
        // BƯỚC 4 - GIA VỊ
        // =====================================================

        steps.Add((
            "Nêm và ướp gia vị",
            BuildSeasoningDescription(
                ingredientNames,
                faker)
        ));

        // =====================================================
        // BƯỚC 5 - CHẾ BIẾN CHÍNH
        // =====================================================

        steps.Add((
            "Chế biến món ăn",
            BuildCookingDescription(
                recipeTitle,
                ingredientNames,
                faker)
        ));

        // =====================================================
        // RANDOM THÊM 0 - 2 BƯỚC
        // =====================================================

        var extraStepCount = faker.Random.Int(0, 2);

        if (extraStepCount >= 1)
        {
            steps.Add((
                "Tiếp tục làm chín",
                $"Tiếp tục chế biến ở lửa vừa trong khoảng {faker.Random.Int(3, 10)} phút. " +
                "Đảo hoặc khuấy nhẹ để nguyên liệu chín đều và không bị cháy."
            ));
        }

        if (extraStepCount >= 2)
        {
            steps.Add((
                "Điều chỉnh hương vị",
                "Nếm thử món ăn và điều chỉnh lượng gia vị cho phù hợp trước khi hoàn thiện."
            ));
        }

        // =====================================================
        // BƯỚC CUỐI
        // =====================================================

        steps.Add((
            "Hoàn thiện món ăn",
            $"Kiểm tra độ chín của món {recipeTitle}, tắt bếp và trình bày ra đĩa hoặc tô. " +
            "Trang trí tùy thích và dùng khi còn nóng."
        ));

        return steps;
    }

    private static string BuildPreparationDescription(
        List<string> ingredientNames,
        Faker faker)
    {
        if (ingredientNames.Count == 0)
        {
            return "Rửa sạch và sơ chế các nguyên liệu trước khi chế biến.";
        }

        var selected = ingredientNames
            .Take(faker.Random.Int(
                1,
                Math.Min(3, ingredientNames.Count)))
            .ToList();

        return $"Rửa sạch {FormatIngredients(selected)}. " +
               "Cắt hoặc chia thành phần vừa ăn, sau đó để ráo nước.";
    }

    private static string BuildSeasoningDescription(
        List<string> ingredientNames,
        Faker faker)
    {
        var seasoningKeywords = new[]
        {
            "muối",
            "đường",
            "tiêu",
            "nước mắm",
            "nước tương",
            "xì dầu",
            "hạt nêm",
            "dầu hào",
            "tỏi",
            "hành"
        };

        var seasonings = ingredientNames
            .Where(x =>
                seasoningKeywords.Any(keyword =>
                    x.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase)))
            .Take(4)
            .ToList();

        if (seasonings.Count > 0)
        {
            return $"Cho {FormatIngredients(seasonings)} vào nguyên liệu và trộn đều. " +
                   $"Để thấm gia vị khoảng {faker.Random.Int(5, 20)} phút.";
        }

        return $"Nêm một lượng gia vị vừa ăn và trộn đều. " +
               $"Để nguyên liệu thấm vị khoảng {faker.Random.Int(5, 15)} phút.";
    }

    private static string BuildCookingDescription(
        string recipeTitle,
        List<string> ingredientNames,
        Faker faker)
    {
        var title = recipeTitle.ToLowerInvariant();

        var mainIngredient = ingredientNames
            .FirstOrDefault()
            ?? "nguyên liệu";

        if (ContainsAny(title, "xào"))
        {
            return $"Cho {mainIngredient} và các nguyên liệu còn lại vào chảo. " +
                   $"Xào ở lửa vừa đến lớn khoảng {faker.Random.Int(5, 10)} phút, đảo đều tay.";
        }

        if (ContainsAny(title, "chiên", "rán"))
        {
            return $"Cho {mainIngredient} vào dầu nóng và chiên đến khi vàng đều hai mặt. " +
                   "Vớt ra để ráo dầu.";
        }

        if (ContainsAny(title, "nướng"))
        {
            return $"Cho {mainIngredient} vào lò hoặc bếp nướng và nướng đến khi chín đều, " +
                   "bề mặt chuyển màu vàng đẹp.";
        }

        if (ContainsAny(title, "luộc"))
        {
            return $"Cho {mainIngredient} vào nước đang sôi và luộc đến khi chín. " +
                   "Vớt ra và để ráo.";
        }

        if (ContainsAny(title, "hấp"))
        {
            return $"Cho {mainIngredient} vào xửng hấp và hấp khoảng {faker.Random.Int(10, 25)} phút " +
                   "hoặc đến khi nguyên liệu chín hoàn toàn.";
        }

        if (ContainsAny(title, "kho"))
        {
            return $"Cho {mainIngredient} vào nồi, thêm phần gia vị và lượng nước vừa đủ. " +
                   $"Kho ở lửa nhỏ khoảng {faker.Random.Int(15, 30)} phút cho thấm vị.";
        }

        if (ContainsAny(title, "canh", "súp"))
        {
            return $"Cho {mainIngredient} cùng các nguyên liệu còn lại vào nước dùng đang sôi. " +
                   $"Nấu thêm khoảng {faker.Random.Int(8, 15)} phút cho nguyên liệu chín.";
        }

        if (ContainsAny(title, "lẩu"))
        {
            return $"Cho các nguyên liệu tạo vị vào nồi nước dùng và đun sôi. " +
                   "Khi dùng, lần lượt cho các nguyên liệu còn lại vào nấu chín.";
        }

        if (ContainsAny(title, "mì", "bún", "phở", "miến"))
        {
            return $"Kết hợp phần sợi đã chuẩn bị với {mainIngredient} và các nguyên liệu còn lại. " +
                   "Trộn hoặc nấu thêm vài phút để các nguyên liệu hòa quyện.";
        }

        return $"Cho {mainIngredient} cùng các nguyên liệu còn lại vào nồi hoặc chảo. " +
               $"Chế biến ở lửa vừa khoảng {faker.Random.Int(8, 15)} phút cho đến khi chín đều.";
    }

    private static bool ContainsAny(
        string value,
        params string[] keywords)
    {
        return keywords.Any(keyword =>
            value.Contains(
                keyword,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatIngredients(
        List<string> ingredients)
    {
        if (ingredients.Count == 0)
        {
            return "các nguyên liệu";
        }

        if (ingredients.Count == 1)
        {
            return ingredients[0];
        }

        if (ingredients.Count == 2)
        {
            return $"{ingredients[0]} và {ingredients[1]}";
        }

        return string.Join(
                   ", ",
                   ingredients.Take(ingredients.Count - 1))
               + $" và {ingredients[^1]}";
    }
}