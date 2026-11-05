using System.Security.Claims;
using System.Text;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints;

public static class PlanningEndpoints
{
    private static readonly string[] MealTypes = ["Breakfast", "Lunch", "Dinner", "Snack"];
    private static readonly Dictionary<string, (decimal Factor, string Unit)> UnitConversions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["g"] = (1m, "g"), ["gram"] = (1m, "g"), ["gam"] = (1m, "g"),
            ["kg"] = (1000m, "g"), ["kilogram"] = (1000m, "g"),
            ["ml"] = (1m, "ml"), ["milliliter"] = (1m, "ml"),
            ["l"] = (1000m, "ml"), ["liter"] = (1000m, "ml")
        };

    public static void MapPlanningEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/me").RequireAuthorization().WithTags("Personal Planning");

        group.MapPost("/shopping-lists", CreateShoppingList);
        group.MapGet("/shopping-lists", GetShoppingLists);
        group.MapGet("/shopping-lists/{id:guid}", GetShoppingList);
        group.MapPost("/shopping-lists/{id:guid}/items", AddShoppingItem);
        group.MapPatch("/shopping-lists/{id:guid}/items/{itemId:guid}/toggle", ToggleShoppingItem);
        group.MapDelete("/shopping-lists/{id:guid}/items/{itemId:guid}", DeleteShoppingItem);
        group.MapDelete("/shopping-lists/{id:guid}/items/checked", DeleteCheckedShoppingItems);
        group.MapDelete("/shopping-lists/{id:guid}", DeleteShoppingList);

        group.MapGet("/meal-plans", GetMealPlans);
        group.MapGet("/meal-plans/current", GetCurrentMealPlan);
        group.MapPost("/meal-plans", CreateMealPlan);
        group.MapPost("/meal-plans/{id:guid}/items", AddMealPlanItem);
        group.MapPut("/meal-plans/{id:guid}/items/{itemId:guid}", UpdateMealPlanItem);
        group.MapDelete("/meal-plans/{id:guid}/items/{itemId:guid}", DeleteMealPlanItem);
        group.MapPost("/meal-plans/{id:guid}/generate-shopping-list", GenerateMealPlanShoppingList);
    }

    private static Guid UserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }

    private sealed record CreateShoppingListRequest(
        string Name,
        IReadOnlyList<Guid>? RecipeIds,
        int TargetServings = 1);

    private sealed record AddShoppingItemRequest(
        string IngredientName,
        decimal Quantity,
        string Unit,
        string? Category);

    private sealed record ToggleShoppingItemRequest(bool IsChecked);

    private sealed record CreateMealPlanRequest(string Title, DateOnly StartDate, DateOnly EndDate);

    private sealed record MealPlanItemRequest(
        Guid RecipeId,
        DateOnly PlanDate,
        string MealType,
        int Servings = 1,
        string? Note = null);

    private static async Task<IResult> CreateShoppingList(
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CreateShoppingListRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Tên danh sách không hợp lệ."] });
        if (request.TargetServings < 1 || request.TargetServings > 100)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["targetServings"] = ["Số suất phải từ 1 đến 100."] });

        var userId = UserId(principal);
        var list = new ShoppingList { UserId = userId, Name = request.Name.Trim() };
        db.ShoppingLists.Add(list);

        if (request.RecipeIds is { Count: > 0 })
        {
            var recipes = await db.Recipes
                .Include(x => x.Ingredients)
                .Where(x => request.RecipeIds.Contains(x.Id) && !x.IsDeleted && x.Status == Domain.Enums.RecipeStatus.Published)
                .ToListAsync(cancellationToken);
            if (recipes.Count != request.RecipeIds.Distinct().Count())
                return Results.NotFound(new { title = "Một hoặc nhiều công thức không tồn tại." });

            AddAggregatedItems(list, recipes.Select(recipe =>
                (recipe, request.TargetServings / (decimal)Math.Max(1, recipe.Servings))));
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/me/shopping-lists/{list.Id}", ToListResponse(list));
    }

    private static async Task<IResult> GetShoppingLists(
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = UserId(principal);
        var query = db.ShoppingLists.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var lists = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.Name, x.CreatedAt, itemCount = x.Items.Count, checkedCount = x.Items.Count(i => i.IsChecked) })
            .ToListAsync(cancellationToken);
        return Results.Ok(new { data = lists, meta = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) } });
    }

    private static async Task<IResult> GetShoppingList(
        ClaimsPrincipal principal, ApplicationDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var list = await db.ShoppingLists.AsNoTracking().Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), cancellationToken);
        return list is null ? Results.NotFound() : Results.Ok(new { data = ToListResponse(list) });
    }

    private static async Task<IResult> AddShoppingItem(
        ClaimsPrincipal principal, ApplicationDbContext db, Guid id, AddShoppingItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IngredientName) || request.IngredientName.Length > 200 || request.Quantity < 0)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["item"] = ["Nguyên liệu hoặc số lượng không hợp lệ."] });
        var list = await db.ShoppingLists.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), cancellationToken);
        if (list is null) return Results.NotFound();
        var item = new ShoppingListItem
        {
            ShoppingListId = id,
            UserId = list.UserId,
            Name = request.IngredientName.Trim(),
            Quantity = request.Quantity,
            Unit = request.Unit.Trim(),
            Category = request.Category?.Trim(),
            SortOrder = await db.ShoppingListItems.CountAsync(x => x.ShoppingListId == id, cancellationToken)
        };
        db.ShoppingListItems.Add(item);
        list.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/me/shopping-lists/{id}", ToItemResponse(item));
    }

    private static async Task<IResult> ToggleShoppingItem(
        ClaimsPrincipal principal, ApplicationDbContext db, Guid id, Guid itemId,
        ToggleShoppingItemRequest request, CancellationToken cancellationToken)
    {
        var item = await db.ShoppingListItems.SingleOrDefaultAsync(
            x => x.Id == itemId && x.ShoppingListId == id && x.UserId == UserId(principal), cancellationToken);
        if (item is null) return Results.NotFound();
        item.IsChecked = request.IsChecked;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { data = ToItemResponse(item) });
    }

    private static async Task<IResult> DeleteShoppingItem(
        ClaimsPrincipal principal, ApplicationDbContext db, Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        var item = await db.ShoppingListItems.SingleOrDefaultAsync(
            x => x.Id == itemId && x.ShoppingListId == id && x.UserId == UserId(principal), cancellationToken);
        if (item is null) return Results.NotFound();
        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteCheckedShoppingItems(
        ClaimsPrincipal principal, ApplicationDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var items = await db.ShoppingListItems.Where(
            x => x.ShoppingListId == id && x.UserId == UserId(principal) && x.IsChecked).ToListAsync(cancellationToken);
        db.ShoppingListItems.RemoveRange(items);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteShoppingList(
        ClaimsPrincipal principal, ApplicationDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var list = await db.ShoppingLists.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), cancellationToken);
        if (list is null) return Results.NotFound();
        db.ShoppingLists.Remove(list);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetMealPlans(
        ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var plans = await db.MealPlans.AsNoTracking().Where(x => x.UserId == UserId(principal))
            .OrderByDescending(x => x.StartDate)
            .Select(x => new { x.Id, x.Title, x.StartDate, x.EndDate, itemCount = x.Items.Count })
            .ToListAsync(cancellationToken);
        return Results.Ok(new { data = plans });
    }

    private static async Task<IResult> GetCurrentMealPlan(
        ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = await db.MealPlans.AsNoTracking().Include(x => x.Items).ThenInclude(x => x.Recipe)
            .SingleOrDefaultAsync(x => x.UserId == UserId(principal) && x.StartDate <= today && x.EndDate >= today, cancellationToken);
        return plan is null ? Results.NotFound() : Results.Ok(new { data = ToMealPlanResponse(plan) });
    }

    private static async Task<IResult> CreateMealPlan(
        ClaimsPrincipal principal, ApplicationDbContext db, CreateMealPlanRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200 || request.EndDate < request.StartDate)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["plan"] = ["Thông tin thực đơn không hợp lệ."] });
        var plan = new MealPlan { UserId = UserId(principal), Title = request.Title.Trim(), StartDate = request.StartDate, EndDate = request.EndDate };
        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/me/meal-plans/{plan.Id}", ToMealPlanResponse(plan));
    }

    private static async Task<IResult> AddMealPlanItem(
        ClaimsPrincipal principal, ApplicationDbContext db, Guid id, MealPlanItemRequest request, CancellationToken cancellationToken)
    {
        var plan = await db.MealPlans.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), cancellationToken);
        if (plan is null) return Results.NotFound();
        if (!MealTypes.Contains(request.MealType) || request.Servings < 1 || request.PlanDate < plan.StartDate || request.PlanDate > plan.EndDate)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["item"] = ["Ngày, bữa ăn hoặc số suất không hợp lệ."] });
        if (!await db.Recipes.AnyAsync(x => x.Id == request.RecipeId && !x.IsDeleted, cancellationToken))
            return Results.NotFound(new { title = "Không tìm thấy công thức." });
        var item = await db.MealPlanItems.SingleOrDefaultAsync(x => x.MealPlanId == id && x.PlanDate == request.PlanDate && x.MealType == request.MealType, cancellationToken);
        if (item is null)
        {
            item = new MealPlanItem { MealPlanId = id, RecipeId = request.RecipeId, PlanDate = request.PlanDate, MealType = request.MealType, Servings = request.Servings, Note = request.Note };
            db.MealPlanItems.Add(item);
        }
        else
        {
            item.RecipeId = request.RecipeId; item.Servings = request.Servings; item.Note = request.Note;
        }
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { data = new { item.Id, item.RecipeId, item.PlanDate, item.MealType, item.Servings, item.Note } });
    }

    private static async Task<IResult> UpdateMealPlanItem(
        ClaimsPrincipal principal, ApplicationDbContext db, Guid id, Guid itemId, MealPlanItemRequest request, CancellationToken cancellationToken)
    {
        var item = await db.MealPlanItems.Include(x => x.MealPlan).SingleOrDefaultAsync(
            x => x.Id == itemId && x.MealPlanId == id && x.MealPlan.UserId == UserId(principal), cancellationToken);
        if (item is null) return Results.NotFound();
        if (!MealTypes.Contains(request.MealType) || request.Servings < 1 || request.PlanDate < item.MealPlan.StartDate || request.PlanDate > item.MealPlan.EndDate)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["item"] = ["Ngày, bữa ăn hoặc số suất không hợp lệ."] });
        item.RecipeId = request.RecipeId; item.PlanDate = request.PlanDate; item.MealType = request.MealType; item.Servings = request.Servings; item.Note = request.Note;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { data = new { item.Id, item.RecipeId, item.PlanDate, item.MealType, item.Servings, item.Note } });
    }

    private static async Task<IResult> DeleteMealPlanItem(
        ClaimsPrincipal principal, ApplicationDbContext db, Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        var item = await db.MealPlanItems.Include(x => x.MealPlan).SingleOrDefaultAsync(
            x => x.Id == itemId && x.MealPlanId == id && x.MealPlan.UserId == UserId(principal), cancellationToken);
        if (item is null) return Results.NotFound();
        db.MealPlanItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GenerateMealPlanShoppingList(
        ClaimsPrincipal principal, ApplicationDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var plan = await db.MealPlans.Include(x => x.Items).ThenInclude(x => x.Recipe).ThenInclude(x => x.Ingredients)
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId(principal), cancellationToken);
        if (plan is null) return Results.NotFound();
        var list = new ShoppingList { UserId = plan.UserId, Name = $"Đi chợ: {plan.Title}" };
        db.ShoppingLists.Add(list);
        AddAggregatedItems(list, plan.Items.Select(item => (item.Recipe, item.Servings / (decimal)Math.Max(1, item.Recipe.Servings))));
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/me/shopping-lists/{list.Id}", ToListResponse(list));
    }

    private static void AddAggregatedItems(ShoppingList list, IEnumerable<(Recipe Recipe, decimal Factor)> sources)
    {
        var groups = sources.SelectMany(source => source.Recipe.Ingredients.Select(ingredient =>
                (source.Recipe.Id, ingredient, source.Factor)))
            .Where(x => !x.ingredient.IsDeleted && !string.IsNullOrWhiteSpace(x.ingredient.Name) && x.ingredient.Quantity.HasValue)
            .GroupBy(x => NormalizeKey(x.ingredient.Name, x.ingredient.Unit))
            .Select((group, index) =>
            {
                var first = group.First();
                var quantity = group.Sum(x => ConvertQuantity(x.ingredient.Quantity!.Value * x.Factor, x.ingredient.Unit, first.ingredient.Unit).Quantity);
                return new ShoppingListItem
                {
                    ShoppingList = list, UserId = list.UserId, RecipeId = first.Id,
                    Name = first.ingredient.Name.Trim(), Quantity = Math.Round(quantity, 3),
                    Unit = ConvertQuantity(1, first.ingredient.Unit, first.ingredient.Unit).Unit,
                    Category = InferCategory(first.ingredient.Name), SortOrder = index
                };
            });
        foreach (var item in groups) list.Items.Add(item);
    }

    private static (decimal Quantity, string Unit) ConvertQuantity(decimal quantity, string? from, string? to)
    {
        var fromKey = (from ?? string.Empty).Trim();
        var toKey = (to ?? string.Empty).Trim();
        if (UnitConversions.TryGetValue(fromKey, out var fromUnit) && UnitConversions.TryGetValue(toKey, out var toUnit))
            return (quantity * fromUnit.Factor / toUnit.Factor, toUnit.Unit);
        return (quantity, toKey);
    }

    private static string NormalizeKey(string name, string? unit)
    {
        var normalizedUnit = unit?.Trim() ?? string.Empty;
        if (UnitConversions.TryGetValue(normalizedUnit, out var conversion))
            normalizedUnit = conversion.Unit;
        return $"{name.Trim().ToLowerInvariant()}|{normalizedUnit.ToLowerInvariant()}";
    }
    private static string InferCategory(string name)
    {
        var value = name.ToLowerInvariant();
        if (value.Contains("thịt") || value.Contains("cá") || value.Contains("tôm") || value.Contains("gà")) return "Thịt/Cá";
        if (value.Contains("rau") || value.Contains("cà rốt") || value.Contains("hành") || value.Contains("củ")) return "Rau củ";
        if (value.Contains("muối") || value.Contains("tiêu") || value.Contains("dầu") || value.Contains("nước mắm")) return "Gia vị";
        return "Khác";
    }

    private static object ToItemResponse(ShoppingListItem item) => new
    {
        item.Id, ingredientName = item.Name, item.RecipeId, item.Quantity, item.Unit, item.Category, item.IsChecked, item.SortOrder
    };

    private static object ToListResponse(ShoppingList list) => new
    {
        list.Id, list.Name, list.CreatedAt, list.UpdatedAt, items = list.Items.OrderBy(x => x.SortOrder).Select(ToItemResponse)
    };

    private static object ToMealPlanResponse(MealPlan plan) => new
    {
        plan.Id, plan.Title, plan.StartDate, plan.EndDate,
        items = plan.Items.OrderBy(x => x.PlanDate).ThenBy(x => x.MealType).Select(x => new
        {
            x.Id, x.RecipeId, x.PlanDate, x.MealType, x.Servings, x.Note,
            recipeTitle = x.Recipe?.Title, recipeSlug = x.Recipe?.Slug
        })
    };
}
