using System.Security.Claims;
using CulinaryBlog.API.Extensions;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints;

public static class SocialNutritionEndpoints
{
    public static void MapSocialNutritionEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/api/v1/users").RequireAuthorization().WithTags("Social");
        users.MapPost("/{id:guid}/follow", Follow).WithSummary("Theo dõi tác giả");
        users.MapDelete("/{id:guid}/follow", Unfollow).WithSummary("Bỏ theo dõi tác giả");
        users.MapGet("/{id:guid}/follow-status", GetFollowStatus).WithSummary("Kiểm tra trạng thái theo dõi");
        users.MapGet("/{id:guid}/followers", GetFollowers).WithSummary("Lấy danh sách người theo dõi");

        var currentUser = app.MapGroup("/api/v1/me")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy)
            .WithTags("Social and Nutrition");
        currentUser.MapGet("/feed", GetFeed).WithSummary("Lấy bảng tin từ các tác giả đang theo dõi");
        currentUser.MapGet("/nutrition-goals", GetNutritionGoals).WithSummary("Lấy mục tiêu dinh dưỡng cá nhân");
        currentUser.MapPut("/nutrition-goals", UpdateNutritionGoals).WithSummary("Tính lại mục tiêu dinh dưỡng cá nhân");
        currentUser.MapPatch("/meal-plans/{planId:guid}/items/{itemId:guid}/consumed", SetMealConsumed)
            .WithSummary("Đánh dấu bữa ăn đã dùng hoặc chưa dùng");
        currentUser.MapGet("/nutrition-summary", GetNutritionSummary).WithSummary("Tổng hợp dinh dưỡng từ bữa ăn đã dùng");
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(claim, out var userId) ? userId : throw new UnauthorizedAccessException();
    }

    private static async Task<IResult> Follow(
        Guid id,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        ICacheService cache,
        CancellationToken cancellationToken)
    {
        var followerId = GetUserId(principal);
        if (id == followerId)
            return Results.Problem(
                title: "Bạn không thể tự theo dõi chính mình.",
                statusCode: StatusCodes.Status422UnprocessableEntity);

        if (!await db.Users.AnyAsync(user => user.Id == id && user.IsActive, cancellationToken))
            return Results.NotFound();

        var alreadyFollowing = await db.UserFollows.AnyAsync(
            follow => follow.FollowerId == followerId && follow.FollowingId == id,
            cancellationToken);
        if (!alreadyFollowing)
        {
            db.UserFollows.Add(new UserFollow { FollowerId = followerId, FollowingId = id });
            await db.SaveChangesAsync(cancellationToken);
            await BumpFeedGenerationAsync(cache, cancellationToken);
        }

        return Results.Ok(new { data = new { following = true } });
    }

    private static async Task<IResult> Unfollow(
        Guid id,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        ICacheService cache,
        CancellationToken cancellationToken)
    {
        var followerId = GetUserId(principal);
        var follow = await db.UserFollows.SingleOrDefaultAsync(
            item => item.FollowerId == followerId && item.FollowingId == id,
            cancellationToken);
        if (follow is not null)
        {
            db.UserFollows.Remove(follow);
            await db.SaveChangesAsync(cancellationToken);
            await BumpFeedGenerationAsync(cache, cancellationToken);
        }

        return Results.Ok(new { data = new { following = false } });
    }

    private static async Task<IResult> GetFollowStatus(
        Guid id, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var followerId = GetUserId(principal);
        var following = await db.UserFollows.AnyAsync(
            item => item.FollowerId == followerId && item.FollowingId == id,
            cancellationToken);
        return Results.Ok(new { data = new { following } });
    }

    private static async Task<IResult> GetFollowers(
        Guid id,
        ApplicationDbContext db,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Clamp(page, 1, 1_000_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        if (!await db.Users.AnyAsync(user => user.Id == id && user.IsActive, cancellationToken))
            return Results.NotFound();

        var query = db.UserFollows.AsNoTracking()
            .Where(follow => follow.FollowingId == id && follow.Follower.IsActive);
        var total = await query.CountAsync(cancellationToken);
        var followers = await query
            .Where(follow => follow.Follower.IsActive)
            .OrderByDescending(follow => follow.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(follow => new FollowerResponse(
                follow.FollowerId,
                follow.Follower.DisplayName,
                follow.Follower.AvatarUrl,
                follow.CreatedAt))
            .ToListAsync(cancellationToken);
        return Results.Ok(new FollowerPageResponse(followers, page, pageSize, total));
    }

    private static async Task<IResult> GetFeed(
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        ICacheService cache,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(principal);
        page = Math.Clamp(page, 1, 1_000_000);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var generation = await cache.GetAsync<string>("activity:cache:generation", cancellationToken) ?? "initial";
        var cacheKey = $"feed:user:{userId:N}:{generation}:{page}:{pageSize}";
        var cached = await cache.GetAsync<FeedPageResponse>(cacheKey, cancellationToken);
        if (cached is not null)
            return Results.Ok(cached);

        var followingIds = db.UserFollows.AsNoTracking()
            .Where(follow => follow.FollowerId == userId)
            .Select(follow => follow.FollowingId);
        var publicStatuses = new[] { RecipeStatus.Published, RecipeStatus.Approved };
        var query = db.ActivityLogs.AsNoTracking()
            .Where(activity => followingIds.Contains(activity.UserId))
            .Where(activity => activity.ActivityType == ActivityType.CommentedRecipe
                ? db.RecipeComments.Any(comment =>
                    comment.Id == activity.TargetId &&
                    comment.Status == ModerationStatus.Approved &&
                    publicStatuses.Contains(comment.Recipe.Status) &&
                    !comment.Recipe.IsDeleted)
                : db.Recipes.Any(recipe =>
                    recipe.Id == activity.TargetId &&
                    publicStatuses.Contains(recipe.Status) &&
                    !recipe.IsDeleted));
        var total = await query.CountAsync(cancellationToken);
        var activities = await query.OrderByDescending(activity => activity.CreatedAt)
            .ThenByDescending(activity => activity.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(activity => new ActivityResponse(
                activity.Id,
                activity.UserId,
                activity.User.DisplayName,
                activity.User.AvatarUrl,
                activity.ActivityType,
                activity.TargetId,
                activity.CreatedAt))
            .ToListAsync(cancellationToken);

        var commentIds = activities
            .Where(activity => activity.ActivityType == ActivityType.CommentedRecipe)
            .Select(activity => activity.TargetId)
            .ToArray();
        var commentRecipeIds = await db.RecipeComments.AsNoTracking()
            .Where(comment => commentIds.Contains(comment.Id))
            .Select(comment => new { comment.Id, comment.RecipeId })
            .ToDictionaryAsync(comment => comment.Id, comment => comment.RecipeId, cancellationToken);
        var recipeIds = activities
            .Select(activity => activity.ActivityType == ActivityType.CommentedRecipe
                ? commentRecipeIds.GetValueOrDefault(activity.TargetId)
                : activity.TargetId)
            .Where(recipeId => recipeId != Guid.Empty)
            .Distinct()
            .ToArray();
        var recipes = await db.Recipes.AsNoTracking()
            .Where(recipe => recipeIds.Contains(recipe.Id))
            .Select(recipe => new { recipe.Id, recipe.Title, recipe.Slug, recipe.CoverImageUrl })
            .ToDictionaryAsync(recipe => recipe.Id, cancellationToken);
        var items = activities
            .Where(activity =>
            {
                var recipeId = activity.ActivityType == ActivityType.CommentedRecipe
                    ? commentRecipeIds.GetValueOrDefault(activity.TargetId)
                    : activity.TargetId;
                return recipes.ContainsKey(recipeId);
            })
            .Select(activity =>
            {
                var recipeId = activity.ActivityType == ActivityType.CommentedRecipe
                    ? commentRecipeIds[activity.TargetId]
                    : activity.TargetId;
                var recipe = recipes[recipeId];
                return new FeedItemResponse(
                    activity.Id,
                    activity.UserId,
                    activity.UserName,
                    activity.UserAvatarUrl,
                    activity.ActivityType,
                    activity.TargetId,
                    recipe.Id,
                    recipe.Title,
                    recipe.Slug,
                    recipe.CoverImageUrl,
                    activity.CreatedAt);
            })
            .ToList();
        var result = new FeedPageResponse(items, page, pageSize, total);
        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(1), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetNutritionGoals(
        ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var goal = await db.UserNutritionGoals.AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == GetUserId(principal), cancellationToken);
        if (goal is null)
            return Results.NotFound();

        var targets = NutritionGoalCalculator.Calculate(
            goal.Age, goal.Gender, goal.HeightCm, goal.WeightKg, goal.ActivityLevel, goal.GoalType);
        return Results.Ok(new NutritionGoalResponse(
            goal.Age, goal.Gender, goal.HeightCm, goal.WeightKg, goal.ActivityLevel, goal.GoalType,
            goal.TargetCalories, goal.TargetProteinG, goal.TargetCarbG, goal.TargetFatG, goal.UpdatedAt,
            targets.BasalMetabolicRate, targets.TotalDailyEnergyExpenditure));
    }

    private static async Task<IResult> UpdateNutritionGoals(
        UpdateNutritionGoalRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        if (request.Age is < 13 or > 120 ||
            request.HeightCm is < 100 or > 250 ||
            request.WeightKg is < 30 or > 350 ||
            !double.IsFinite(request.HeightCm) ||
            !double.IsFinite(request.WeightKg) ||
            !Enum.IsDefined(request.Gender) ||
            !Enum.IsDefined(request.ActivityLevel) ||
            !Enum.IsDefined(request.GoalType))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["Age phải từ 13-120, chiều cao 100-250 cm và cân nặng 30-350 kg."]
            });

        NutritionTargets targets;
        try
        {
            targets = NutritionGoalCalculator.Calculate(
                request.Age, request.Gender, request.HeightCm, request.WeightKg, request.ActivityLevel, request.GoalType);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = [exception.Message] });
        }

        var userId = GetUserId(principal);
        var goal = await db.UserNutritionGoals.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (goal is null)
        {
            goal = new UserNutritionGoal { UserId = userId };
            db.UserNutritionGoals.Add(goal);
        }
        goal.Age = request.Age;
        goal.Gender = request.Gender;
        goal.HeightCm = request.HeightCm;
        goal.WeightKg = request.WeightKg;
        goal.ActivityLevel = request.ActivityLevel;
        goal.GoalType = request.GoalType;
        goal.TargetCalories = targets.Calories;
        goal.TargetProteinG = targets.ProteinG;
        goal.TargetCarbG = targets.CarbohydratesG;
        goal.TargetFatG = targets.FatG;
        goal.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new NutritionGoalResponse(
            goal.Age, goal.Gender, goal.HeightCm, goal.WeightKg, goal.ActivityLevel, goal.GoalType,
            goal.TargetCalories, goal.TargetProteinG, goal.TargetCarbG, goal.TargetFatG, goal.UpdatedAt,
            targets.BasalMetabolicRate, targets.TotalDailyEnergyExpenditure));
    }

    private static async Task<IResult> SetMealConsumed(
        Guid planId,
        Guid itemId,
        ConsumedRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var item = await db.MealPlanItems
            .Include(mealItem => mealItem.MealPlan)
            .SingleOrDefaultAsync(
                mealItem => mealItem.Id == itemId && mealItem.MealPlanId == planId && mealItem.MealPlan.UserId == userId,
                cancellationToken);
        if (item is null)
            return Results.NotFound();
        item.Consumed = request.Consumed;
        item.ConsumedAt = request.Consumed ? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { data = new { item.Id, item.Consumed, item.ConsumedAt } });
    }

    private static async Task<IResult> GetNutritionSummary(
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        DateOnly? date,
        string? period,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var targetDate = date ?? today;
        if (targetDate > today)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Không thể thống kê cho ngày trong tương lai."] });

        var normalizedPeriod = string.IsNullOrWhiteSpace(period) ? "day" : period.Trim().ToLowerInvariant();
        if (normalizedPeriod is not ("day" or "week"))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["period"] = ["Chỉ hỗ trợ day hoặc week."] });

        var startDate = normalizedPeriod == "week"
            ? targetDate.AddDays(-(((int)targetDate.DayOfWeek + 6) % 7))
            : targetDate;
        var endDate = startDate.AddDays(normalizedPeriod == "week" ? 7 : 1);
        var start = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(endDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var meals = await db.MealPlanItems.AsNoTracking()
            .Where(item => item.MealPlan.UserId == userId &&
                item.Consumed &&
                item.ConsumedAt >= start &&
                item.ConsumedAt < end)
            .Select(item => new
            {
                Servings = item.Servings,
                RecipeServings = item.Recipe.Servings,
                item.Recipe.Nutrition.Calories,
                item.Recipe.Nutrition.Protein,
                item.Recipe.Nutrition.Carbohydrates,
                item.Recipe.Nutrition.Fat
            })
            .ToListAsync(cancellationToken);

        var totalCalories = meals.Sum(meal => (double)(meal.Calories ?? 0) * meal.Servings / Math.Max(1, meal.RecipeServings));
        var totalProtein = meals.Sum(meal => (double)(meal.Protein ?? 0) * meal.Servings / Math.Max(1, meal.RecipeServings));
        var totalCarbs = meals.Sum(meal => (double)(meal.Carbohydrates ?? 0) * meal.Servings / Math.Max(1, meal.RecipeServings));
        var totalFat = meals.Sum(meal => (double)(meal.Fat ?? 0) * meal.Servings / Math.Max(1, meal.RecipeServings));
        return Results.Ok(new NutritionSummaryResponse(targetDate, startDate, endDate.AddDays(-1), normalizedPeriod, meals.Count,
            Math.Round(totalCalories, 1), Math.Round(totalProtein, 1),
            Math.Round(totalCarbs, 1), Math.Round(totalFat, 1)));
    }

    private static Task BumpFeedGenerationAsync(ICacheService cache, CancellationToken cancellationToken) =>
        cache.SetAsync("activity:cache:generation", Guid.NewGuid().ToString("N"), TimeSpan.FromDays(365), cancellationToken);

    /// <summary>A public follower summary.</summary>
    public sealed record FollowerResponse(Guid Id, string DisplayName, string? AvatarUrl, DateTimeOffset FollowedAt);

    /// <summary>A paginated follower list.</summary>
    public sealed record FollowerPageResponse(IReadOnlyList<FollowerResponse> Items, int Page, int PageSize, int TotalCount);

    /// <summary>An activity record before recipe details are resolved.</summary>
    public sealed record ActivityResponse(
        Guid Id, Guid UserId, string UserName, string? UserAvatarUrl, ActivityType ActivityType, Guid TargetId, DateTimeOffset CreatedAt);

    /// <summary>An activity item in a user's feed.</summary>
    public sealed record FeedItemResponse(
        Guid Id, Guid UserId, string UserName, string? UserAvatarUrl, ActivityType ActivityType, Guid TargetId,
        Guid RecipeId, string RecipeTitle, string RecipeSlug, string? ImageUrl, DateTimeOffset CreatedAt);

    /// <summary>A page of activities from followed authors.</summary>
    public sealed record FeedPageResponse(IReadOnlyList<FeedItemResponse> Items, int Page, int PageSize, int TotalCount);

    /// <summary>Body to set the consumed state of a planned meal.</summary>
    public sealed record ConsumedRequest(bool Consumed);

    /// <summary>Input values used to calculate daily nutrition targets.</summary>
    public sealed record UpdateNutritionGoalRequest(
        int Age,
        NutritionGender Gender,
        double HeightCm,
        double WeightKg,
        NutritionActivityLevel ActivityLevel,
        NutritionGoalType GoalType);

    /// <summary>Stored daily calorie and macro targets.</summary>
    public sealed record NutritionGoalResponse(
        int Age, NutritionGender Gender, double HeightCm, double WeightKg,
        NutritionActivityLevel ActivityLevel, NutritionGoalType GoalType,
        double TargetCalories, double TargetProteinG, double TargetCarbG, double TargetFatG,
        DateTimeOffset UpdatedAt, double? BasalMetabolicRate = null, double? TotalDailyEnergyExpenditure = null);

    /// <summary>Daily nutrients from meals marked as consumed.</summary>
    public sealed record NutritionSummaryResponse(
        DateOnly Date, DateOnly StartDate, DateOnly EndDate, string Period,
        int ConsumedMeals, double Calories, double ProteinG, double CarbohydratesG, double FatG);
}
