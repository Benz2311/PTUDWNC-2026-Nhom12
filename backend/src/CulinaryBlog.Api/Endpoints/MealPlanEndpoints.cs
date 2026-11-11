using System.Security.Claims;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.API.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints;

public static class MealPlanEndpoints
{
    public static void MapMealPlanEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/me")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy);

        group.MapGet("/meal-plan", GetMealPlan);
        group.MapPut("/meal-plan", UpsertMealPlanEntry);
        group.MapDelete("/meal-plan/{id:guid}", DeleteMealPlanEntry);
        group.MapGet("/shopping-list", GetShoppingList);
        group.MapPost("/shopping-list/generate", GenerateShoppingList);
        group.MapPost("/shopping-list", AddShoppingListItem);
        group.MapPut("/shopping-list/{id:guid}/check", CheckShoppingListItem);
        group.MapDelete("/shopping-list/{id:guid}", DeleteShoppingListItem);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException();
    }

    private static async Task<IResult> GetMealPlan(
        HttpContext http,
        ApplicationDbContext db,
        DateOnly? from,
        DateOnly? to)
    {
        var userId = GetUserId(http.User);
        var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var end = to ?? start.AddDays(14);

        var rawEntries = await db.MealPlanEntries
            .AsNoTracking()
            .Include(x => x.Recipe)
                .ThenInclude(r => r.Images)
            .Where(x => x.UserId == userId && x.PlannedDate >= start && x.PlannedDate <= end)
            .OrderBy(x => x.PlannedDate).ThenBy(x => x.MealType)
            .ToListAsync();

        var entries = rawEntries
            .Where(x => x.Recipe is not null && !x.Recipe.IsDeleted)
            .Select(x => new
            {
                x.Id,
                date = x.PlannedDate.ToString("yyyy-MM-dd"),
                x.MealType,
                x.RecipeId,
                recipeTitle = x.Recipe.Title,
                recipeSlug = x.Recipe.Slug,
                primaryImageUrl = x.Recipe.Images?.FirstOrDefault(i => i.IsPrimary)?.Url
                    ?? x.Recipe.Images?.FirstOrDefault()?.Url
                    ?? x.Recipe.CoverImageUrl
            })
            .ToList();

        return Results.Ok(new { data = entries });
    }

    private sealed record MealPlanRequest(DateOnly Date, string MealType, Guid RecipeId);

    private static async Task<IResult> UpsertMealPlanEntry(
        HttpContext http,
        ApplicationDbContext db,
        MealPlanRequest request)
    {
        var userId = GetUserId(http.User);
        var mealTypes = new[] { "Breakfast", "Lunch", "Dinner", "Snack" };
        if (!mealTypes.Contains(request.MealType))
            return Results.UnprocessableEntity(new { title = "MealType không hợp lệ" });
        if (!await db.Recipes.AnyAsync(x => x.Id == request.RecipeId && !x.IsDeleted))
            return Results.NotFound(new { title = "Không tìm thấy công thức" });

        var entry = await db.MealPlanEntries.FirstOrDefaultAsync(x =>
            x.UserId == userId && x.PlannedDate == request.Date && x.MealType == request.MealType);
        if (entry is null)
        {
            entry = new MealPlanEntry
            {
                UserId = userId,
                PlannedDate = request.Date,
                MealType = request.MealType,
                RecipeId = request.RecipeId
            };
            db.MealPlanEntries.Add(entry);
        }
        else
        {
            entry.RecipeId = request.RecipeId;
            entry.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { data = new { id = entry.Id, date = entry.PlannedDate.ToString("yyyy-MM-dd"), entry.MealType, entry.RecipeId } });
    }

    private static async Task<IResult> DeleteMealPlanEntry(
        HttpContext http, ApplicationDbContext db, Guid id)
    {
        var userId = GetUserId(http.User);
        var entry = await db.MealPlanEntries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (entry is null) return Results.NotFound(new { title = "Không tìm thấy mục kế hoạch" });
        db.MealPlanEntries.Remove(entry);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetShoppingList(HttpContext http, ApplicationDbContext db)
    {
        var userId = GetUserId(http.User);
        var items = await db.ShoppingListItems.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.IsChecked).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Quantity, x.Unit, x.IsChecked })
            .ToListAsync();
        return Results.Ok(new { data = items });
    }

    private sealed record GenerateShoppingRequest(DateOnly From, DateOnly To);

    private static async Task<IResult> GenerateShoppingList(
        HttpContext http, ApplicationDbContext db, GenerateShoppingRequest request)
    {
        if (request.To < request.From)
            return Results.UnprocessableEntity(new { title = "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu" });

        var userId = GetUserId(http.User);
        var entries = await db.MealPlanEntries.AsNoTracking()
            .Include(x => x.Recipe)
                .ThenInclude(r => r.Ingredients)
            .Where(x => x.UserId == userId && x.PlannedDate >= request.From && x.PlannedDate <= request.To)
            .ToListAsync();

        var recipes = entries
            .Where(x => x.Recipe is not null && !x.Recipe.IsDeleted)
            .Select(x => x.Recipe)
            .ToList();

        var existing = await db.ShoppingListItems
            .Where(x => x.UserId == userId && x.ShoppingListId == null && x.RecipeId != null)
            .ToListAsync();
        var checkedItems = existing
            .Where(x => x.IsChecked)
            .Select(x =>
            {
                var normalized = NormalizeShoppingItem(x.Name, x.Quantity, x.Unit);
                return (Name: NormalizeName(x.Name), normalized.Unit, normalized.Quantity);
            })
            .ToHashSet();

        var aggregated = recipes.SelectMany(recipe => recipe.Ingredients.Select(ingredient =>
            {
                var amount = NormalizeIngredientAmount(
                    ingredient.Name,
                    ingredient.Quantity,
                    ingredient.Unit);
                return new
                {
                    RecipeId = recipe.Id,
                    Ingredient = ingredient,
                    amount.Quantity,
                    amount.Unit
                };
            }))
            .Where(x => !x.Ingredient.IsDeleted && !string.IsNullOrWhiteSpace(x.Ingredient.Name))
            .GroupBy(x => new
            {
                Name = x.Ingredient.Name.Trim().ToLowerInvariant(),
                x.Unit
            })
            .Select(g =>
            {
                var quantities = g.Where(x => x.Quantity.HasValue).Select(x => x.Quantity!.Value).ToList();
                var normalized = NormalizeShoppingItem(
                    g.First().Ingredient.Name,
                    quantities.Count == 0 ? null : quantities.Sum(),
                    g.Key.Unit);
                var isChecked = checkedItems.Contains(
                    (NormalizeName(g.First().Ingredient.Name), normalized.Unit, normalized.Quantity));

                return new ShoppingListItem
                {
                    UserId = userId,
                    Name = g.First().Ingredient.Name.Trim(),
                    Quantity = normalized.Quantity,
                    Unit = string.IsNullOrWhiteSpace(normalized.Unit) ? null : normalized.Unit,
                    RecipeId = g.First().RecipeId,
                    IsChecked = isChecked
                };
            }).ToList();

        db.ShoppingListItems.RemoveRange(existing);
        db.ShoppingListItems.AddRange(aggregated);
        await db.SaveChangesAsync();

        return Results.Ok(new { data = aggregated.Select(x => new { x.Id, x.Name, x.Quantity, x.Unit, x.IsChecked }) });
    }

    private static (decimal? Quantity, string Unit) NormalizeIngredientAmount(
        string ingredientName,
        decimal? quantity,
        string? unit)
    {
        var amount = quantity;
        var normalizedUnit = (unit ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedName = ingredientName.Trim().ToLowerInvariant();

        if ((normalizedName.Contains("gạo") || normalizedName.Contains("rice")) &&
            IsCountUnit(normalizedUnit))
        {
            return (amount * 1000, "g");
        }

        if (normalizedName.Contains("tôm") && (normalizedUnit is "quả" or "trái" or "cái"))
            return (amount, "con");

        return normalizedUnit switch
        {
            "gram" or "gam" => (amount, "g"),
            "kg" or "kilogram" => (amount * 1000, "g"),
            "l" or "liter" or "litre" => (amount * 1000, "ml"),
            "milliliter" or "millilitre" => (amount, "ml"),
            _ => (amount, normalizedUnit)
        };
    }

    private static (decimal? Quantity, string Unit) NormalizeShoppingItem(
        string name,
        decimal? quantity,
        string? unit)
    {
        var normalized = NormalizeIngredientAmount(name, quantity, unit);
        var normalizedQuantity = normalized.Quantity;
        var normalizedUnit = normalized.Unit;

        if (normalizedQuantity.HasValue && normalizedQuantity.Value >= 1000 &&
            (normalizedUnit is "g" or "ml"))
        {
            normalizedQuantity /= 1000m;
            normalizedUnit = normalizedUnit == "g" ? "kg" : "l";
        }

        if (normalizedQuantity.HasValue && IsCountUnit(normalizedUnit))
            normalizedQuantity = decimal.Ceiling(normalizedQuantity.Value);

        return (normalizedQuantity, normalizedUnit);
    }

    private static string NormalizeName(string name) => name.Trim().ToLowerInvariant();

    private static bool IsCountUnit(string unit) =>
        unit is "con" or "cái" or "quả" or "trái" or "củ" or "tép" or "miếng" or "lát" or "viên";

    private sealed record ShoppingItemRequest(string Name, decimal? Quantity, string? Unit);

    private static async Task<IResult> AddShoppingListItem(
        HttpContext http, ApplicationDbContext db, ShoppingItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
            return Results.UnprocessableEntity(new { title = "Tên nguyên liệu không hợp lệ" });
        var userId = GetUserId(http.User);
        var item = new ShoppingListItem
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim()
        };
        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync();
        return Results.Ok(new { data = new { item.Id, item.Name, item.Quantity, item.Unit, item.IsChecked } });
    }

    private sealed record CheckShoppingRequest(bool IsChecked);

    private static async Task<IResult> CheckShoppingListItem(
        HttpContext http, ApplicationDbContext db, Guid id, CheckShoppingRequest request)
    {
        var userId = GetUserId(http.User);
        var item = await db.ShoppingListItems.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is null) return Results.NotFound(new { title = "Không tìm thấy mục mua sắm" });
        item.IsChecked = request.IsChecked;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new { data = new { item.Id, item.Name, item.Quantity, item.Unit, item.IsChecked } });
    }

    private static async Task<IResult> DeleteShoppingListItem(
        HttpContext http, ApplicationDbContext db, Guid id)
    {
        var userId = GetUserId(http.User);
        var item = await db.ShoppingListItems.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is null) return Results.NotFound(new { title = "Không tìm thấy mục mua sắm" });
        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
