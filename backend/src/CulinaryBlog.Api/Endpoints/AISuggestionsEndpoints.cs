using System.Linq.Expressions;
using System.Text.Json;
using CulinaryBlog.API.Extensions;
using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.AI;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints;

public static partial class AISuggestionsEndpoints
{
    private const int MaxIngredientCount = 30;
    private const int MaxOcrImageBytes = 5 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapAISuggestionsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ai")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.AIUserPolicy)
            .WithTags("AI");

        group.MapPost("/suggest-by-ingredients", SuggestByIngredients)
            .WithSummary("Gợi ý công thức từ nguyên liệu")
            .WithDescription("Chỉ trả về công thức công khai có ID tồn tại trong cơ sở dữ liệu.")
            .Produces<SuggestionResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/ocr-recipe", OcrRecipe)
            .WithSummary("Trích xuất công thức từ ảnh")
            .WithDescription("Nhận ảnh JPEG, PNG, WebP hoặc AVIF và trả về bản nháp công thức đã kiểm tra.")
            .WithMetadata(new RequestSizeLimitAttribute(MaxOcrImageBytes + 64 * 1024))
            .Produces<OcrRecipeDraft>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> SuggestByIngredients(
        SuggestionRequest request,
        ApplicationDbContext db,
        IAIService ai,
        CancellationToken cancellationToken)
    {
        var terms = NormalizeIngredients(request.Ingredients);
        if (terms.Count == 0 || terms.Count > MaxIngredientCount)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["ingredients"] = [$"Cần nhập từ 1 đến {MaxIngredientCount} nguyên liệu hợp lệ."]
            });
        }

        var publicStatuses = new[] { RecipeStatus.Published, RecipeStatus.Approved };
        var predicate = BuildIngredientPredicate(terms);
        var candidates = await db.Recipes.AsNoTracking()
            .Where(recipe => publicStatuses.Contains(recipe.Status) && !recipe.IsDeleted)
            .Where(predicate)
            .OrderByDescending(recipe => recipe.PublishedAt)
            .Take(40)
            .Select(recipe => new SuggestionCandidate(
                recipe.Id,
                recipe.Title,
                recipe.Slug,
                recipe.CoverImageUrl,
                recipe.Ingredients
                    .Where(ingredient => !ingredient.IsDeleted)
                    .OrderBy(ingredient => ingredient.SortOrder)
                    .Select(ingredient => ingredient.Name)
                    .Take(30)
                    .ToList()))
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return Results.Ok(new SuggestionResponse([]));

        var prompt = $$"""
            Choose up to 5 recipes that best use the user's available ingredients.
            Only select IDs from the supplied recipe list. Never invent or modify IDs.
            Return exactly one JSON object with this shape: {"recipeIds":["id"]}.
            Recipe IDs and data:
            {{JsonSerializer.Serialize(candidates.Select(candidate => new { candidate.Id, candidate.Title, candidate.Ingredients }))}}
            Available ingredients:
            {{JsonSerializer.Serialize(terms)}}
            """;

        string generated;
        try
        {
            generated = await ai.GenerateTextAsync(prompt, jsonResponse: true, cancellationToken);
        }
        catch (AIServiceException exception)
        {
            return AIProviderProblem(exception);
        }

        try
        {
            using var document = JsonDocument.Parse(generated);
            if (!document.RootElement.TryGetProperty("recipeIds", out var recipeIds) ||
                recipeIds.ValueKind != JsonValueKind.Array)
            {
                return Results.Problem(
                    title: "AI returned an invalid response.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

            var allowedCandidates = candidates.ToDictionary(candidate => candidate.Id);
            var ids = recipeIds.EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.String)
                .Select(value => Guid.TryParse(value.GetString(), out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty && allowedCandidates.ContainsKey(id))
                .Distinct()
                .Take(5)
                .Select(id => allowedCandidates[id])
                .Select(candidate => new SuggestedRecipeResponse(
                    candidate.Id, candidate.Title, candidate.Slug, candidate.ImageUrl, candidate.Ingredients))
                .ToArray();
            return Results.Ok(new SuggestionResponse(ids));
        }
        catch (JsonException)
        {
            return Results.Problem(
                title: "AI returned an invalid response.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> OcrRecipe(
        HttpRequest request,
        IAIService ai,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(AISuggestionsEndpoints));

        if (!request.HasFormContentType)
            return Results.Problem(title: "Expected multipart/form-data.", statusCode: StatusCodes.Status415UnsupportedMediaType);

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("image");
        if (file is null)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = ["Vui lòng chọn một ảnh công thức."] });
        if (file.Length > MaxOcrImageBytes)
            return Results.Problem(title: "Image exceeds the 5 MB limit.", statusCode: StatusCodes.Status413PayloadTooLarge);

        await using var imageStream = file.OpenReadStream();
        var validation = ImageValidator.Validate(imageStream, file.ContentType, file.FileName, file.Length);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["image"] = [validation.ErrorMessage ?? "Ảnh không hợp lệ."]
            });
        }

        imageStream.Position = 0;
        using var buffer = new MemoryStream((int)file.Length);
        await imageStream.CopyToAsync(buffer, cancellationToken);
        var prompt = """
            Extract only information visible in this recipe image. Do not invent missing amounts or steps.
            Return JSON with keys title, description, ingredients, and steps.
            ingredients must be an array of {name, quantity, unit}; steps an array of {title, description}.
            Use empty strings for unreadable optional values. Keep all text concise and preserve the source language.
            """;

        string generated;
        try
        {
            generated = await ai.GenerateFromImageAsync(
                prompt,
                buffer.ToArray(),
                validation.DetectedMimeType,
                jsonResponse: true,
                cancellationToken);
        }
        catch (AIServiceException exception)
        {
            return AIProviderProblem(exception);
        }

        OcrRecipeDraft? draft;
        try
        {
            draft = JsonSerializer.Deserialize<OcrRecipeDraft>(generated, JsonOptions);
        }
        catch (JsonException)
        {
            logger.LogWarning("Gemini returned malformed OCR JSON.");
            return Results.Problem(title: "AI returned an invalid recipe format.", statusCode: StatusCodes.Status502BadGateway);
        }

        if (!IsValidDraft(draft))
        {
            logger.LogWarning("Gemini returned OCR data that failed recipe validation.");
            return Results.Problem(title: "AI recipe data failed validation.", statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(draft);
    }

    private static List<string> NormalizeIngredients(IReadOnlyCollection<string>? ingredients) =>
        ingredients?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Where(value => value.Length <= 80 && !value.Any(char.IsControl))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxIngredientCount + 1)
            .ToList() ?? [];

    private static Expression<Func<Recipe, bool>> BuildIngredientPredicate(IReadOnlyList<string> terms)
    {
        Expression<Func<Recipe, bool>> predicate = recipe => false;
        foreach (var term in terms)
        {
            var pattern = $"%{term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            Expression<Func<Recipe, bool>> termPredicate = recipe =>
                recipe.Ingredients.Any(ingredient =>
                    !ingredient.IsDeleted && EF.Functions.ILike(ingredient.Name, pattern, "\\"));
            predicate = Or(predicate, termPredicate);
        }

        return predicate;
    }

    private static Expression<Func<T, bool>> Or<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(leftBody, rightBody), parameter);
    }

    private static bool IsValidDraft(OcrRecipeDraft? draft) =>
        draft is not null &&
        IsSafeText(draft.Title, 150, required: true) &&
        IsSafeText(draft.Description, 2000, required: false) &&
        draft.Ingredients is { Count: > 0 and <= 50 } &&
        draft.Ingredients.All(ingredient =>
            IsSafeText(ingredient.Name, 150, required: true) &&
            IsSafeText(ingredient.Quantity, 80, required: false) &&
            IsSafeText(ingredient.Unit, 40, required: false)) &&
        draft.Steps is { Count: > 0 and <= 50 } &&
        draft.Steps.All(step =>
            IsSafeText(step.Title, 120, required: false) &&
            IsSafeText(step.Description, 2000, required: true));

    private static bool IsSafeText(string? value, int maxLength, bool required) =>
        value is not null &&
        value.Length <= maxLength &&
        (!required || !string.IsNullOrWhiteSpace(value)) &&
        !value.Any(character => char.IsControl(character) && character is not '\n' and not '\r' and not '\t') &&
        !value.Contains('<') &&
        !value.Contains('>');

    private static IResult AIProviderProblem(AIServiceException exception)
    {
        var statusCode = exception.Code == "AI_PROVIDER_NOT_CONFIGURED"
            ? StatusCodes.Status503ServiceUnavailable
            : exception.Retryable
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status502BadGateway;
        return Results.Problem(
            title: exception.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = exception.Code });
    }

    /// <summary>Ingredients provided to the recipe suggestion endpoint.</summary>
    public sealed record SuggestionRequest
    {
        /// <summary>Ingredient names currently available to the user.</summary>
        public required IReadOnlyList<string> Ingredients { get; init; }
    }

    /// <summary>A recipe suggestion backed by a published database recipe.</summary>
    public sealed record SuggestedRecipeResponse(
        Guid Id, string Title, string Slug, string? ImageUrl, IReadOnlyList<string> Ingredients);

    /// <summary>Recipes selected from the database for the supplied ingredients.</summary>
    public sealed record SuggestionResponse(IReadOnlyList<SuggestedRecipeResponse> Recipes);

    /// <summary>A recipe draft extracted from an uploaded image.</summary>
    public sealed record OcrRecipeDraft
    {
        /// <summary>Recipe title read from the image.</summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>Optional recipe description read from the image.</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>Ingredient lines read from the image.</summary>
        public List<OcrIngredientDraft> Ingredients { get; init; } = [];

        /// <summary>Preparation steps read from the image.</summary>
        public List<OcrStepDraft> Steps { get; init; } = [];
    }

    /// <summary>An ingredient line extracted from a recipe image.</summary>
    public sealed record OcrIngredientDraft(string Name, string? Quantity, string? Unit);

    /// <summary>A preparation step extracted from a recipe image.</summary>
    public sealed record OcrStepDraft(string Title, string Description);

    private sealed record SuggestionCandidate(
        Guid Id, string Title, string Slug, string? ImageUrl, IReadOnlyList<string> Ingredients);

    private sealed class ParameterReplacer(Expression source, Expression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}
