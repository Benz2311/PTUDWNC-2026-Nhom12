using System.Security.Claims;
using CulinaryBlog.Api.Helpers;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints.Recipes;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this WebApplication app)
    {
        MapRecipeGroup(app.MapGroup("/api/recipes").WithTags("Recipes").RequireRateLimiting("api"));
        MapRecipeGroup(app.MapGroup("/api/v1/recipes").WithTags("Recipes").RequireRateLimiting("api"));
    }

    private static void MapRecipeGroup(RouteGroupBuilder group)
    {
        group.MapGet("", async ([AsParameters] RecipeQuery query, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            if (query.Page is < 1 || query.PageSize is < 1 or > 50)
            {
                return Results.UnprocessableEntity(new { error = "Page must be at least 1 and pageSize must be between 1 and 50." });
            }

            var recipes = db.Recipes
                .AsNoTracking()
                .Where(recipe => !recipe.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                recipes = recipes.Where(recipe => recipe.Title.Contains(search) ||
                    (recipe.Description != null && recipe.Description.Contains(search)));
            }

            if (query.CategoryId.HasValue)
            {
                recipes = recipes.Where(recipe => recipe.CategoryId == query.CategoryId.Value);
            }

            var userId = GetUserId(principal);
            var isAdmin = principal.IsInRole("Admin");
            if (isAdmin && query.Status.HasValue)
            {
                recipes = recipes.Where(recipe => recipe.Status == query.Status.Value);
            }
            else if (userId == Guid.Empty)
            {
                recipes = recipes.Where(recipe => recipe.Status == RecipeStatus.Published);
            }
            else
            {
                recipes = recipes.Where(recipe => recipe.Status == RecipeStatus.Published || recipe.AuthorId == userId);
                if (query.Status.HasValue)
                {
                    recipes = recipes.Where(recipe => recipe.Status == query.Status.Value &&
                        (query.Status.Value == RecipeStatus.Published || recipe.AuthorId == userId));
                }
            }

            var total = await recipes.CountAsync(cancellationToken);
            var page = Math.Max(query.Page ?? 1, 1);
            var pageSize = Math.Clamp(query.PageSize ?? 12, 1, 50);
            var sort = query.Sort?.ToLowerInvariant();
            recipes = sort switch
            {
                "oldest" => recipes.OrderBy(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id),
                "title" => recipes.OrderBy(recipe => recipe.Title).ThenBy(recipe => recipe.Id),
                "cooktime" => recipes.OrderBy(recipe => recipe.CookTimeMinutes).ThenBy(recipe => recipe.Id),
                _ => recipes.OrderByDescending(recipe => recipe.CreatedAt).ThenByDescending(recipe => recipe.Id)
            };

            var rows = await recipes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(recipe => new
                {
                    recipe.Id,
                    recipe.Title,
                    recipe.Slug,
                    recipe.Description,
                    Category = recipe.Category.Name,
                    Author = recipe.Author.FullName,
                    recipe.PrepTimeMinutes,
                    recipe.CookTimeMinutes,
                    recipe.Servings,
                    recipe.Difficulty,
                    recipe.Status,
                    recipe.CreatedAt,
                    PrimaryImageUrl = recipe.Images
                        .OrderBy(image => image.SortOrder)
                        .Select(image => image.Url)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);
            var items = rows.Select(recipe => new RecipeSummaryResponse(
                recipe.Id,
                recipe.Title,
                recipe.Slug,
                recipe.Description,
                recipe.Category,
                recipe.Author,
                recipe.PrepTimeMinutes,
                recipe.CookTimeMinutes,
                recipe.Servings,
                recipe.Difficulty.ToString(),
                recipe.Status.ToString(),
                recipe.CreatedAt,
                recipe.PrimaryImageUrl)).ToList();

            return Results.Ok(new PagedResponse<RecipeSummaryResponse>(items, page, pageSize, total));
        });

        group.MapGet("/search", async ([AsParameters] RecipeSearchQuery query, ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(query.Query) || query.Query.Trim().Length < 2 || query.Page is < 1 || query.PageSize is < 1 or > 50)
            {
                return Results.UnprocessableEntity(new { error = "Search query must contain at least 2 characters." });
            }

            var search = query.Query.Trim();
            var recipes = db.Recipes
                .AsNoTracking()
                .Where(recipe => !recipe.IsDeleted && recipe.Status == RecipeStatus.Published)
                .Where(recipe => recipe.Title.Contains(search) || (recipe.Description != null && recipe.Description.Contains(search)));

            if (query.CategoryId.HasValue)
            {
                recipes = recipes.Where(recipe => recipe.CategoryId == query.CategoryId.Value);
            }

            var total = await recipes.CountAsync(cancellationToken);
            var page = Math.Max(query.Page ?? 1, 1);
            var pageSize = Math.Clamp(query.PageSize ?? 12, 1, 50);
            var rows = await recipes
                .OrderByDescending(recipe => recipe.CreatedAt)
                .ThenByDescending(recipe => recipe.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(recipe => new
                {
                    recipe.Id,
                    recipe.Title,
                    recipe.Slug,
                    recipe.Description,
                    Category = recipe.Category.Name,
                    Author = recipe.Author.FullName,
                    recipe.PrepTimeMinutes,
                    recipe.CookTimeMinutes,
                    recipe.Servings,
                    recipe.Difficulty,
                    recipe.Status,
                    recipe.CreatedAt,
                    PrimaryImageUrl = recipe.Images
                        .OrderBy(image => image.SortOrder)
                        .Select(image => image.Url)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);
            var items = rows.Select(recipe => new RecipeSummaryResponse(
                recipe.Id,
                recipe.Title,
                recipe.Slug,
                recipe.Description,
                recipe.Category,
                recipe.Author,
                recipe.PrepTimeMinutes,
                recipe.CookTimeMinutes,
                recipe.Servings,
                recipe.Difficulty.ToString(),
                recipe.Status.ToString(),
                recipe.CreatedAt,
                recipe.PrimaryImageUrl)).ToList();

            return Results.Ok(new PagedResponse<RecipeSummaryResponse>(items, page, pageSize, total));
        });

        group.MapGet("/{slug}", async (string slug, ClaimsPrincipal principal, IRecipeRepository recipeRepository, CancellationToken cancellationToken) =>
        {
            Recipe? recipe = null;
            foreach (var lookupSlug in GetSlugAliases(slug))
            {
                recipe = await recipeRepository.GetBySlugAsync(lookupSlug, includeDeleted: false, cancellationToken);
                if (recipe is not null)
                {
                    break;
                }
            }

            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (recipe.Status != RecipeStatus.Published && !IsOwnerOrAdmin(principal, recipe.AuthorId))
            {
                return Results.Forbid();
            }

            return Results.Ok(ToDetail(recipe));
        });

        group.MapPost("", async (RecipeRequest request, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeRepository recipeRepository, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var authorId = GetUserId(principal);
            if (authorId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            request = request with { AuthorId = authorId };
            var validation = await ValidateRequest(request, db, cancellationToken);
            if (validation is not null)
            {
                return validation == "Recipe slug already exists."
                    ? Results.Conflict(new { error = validation })
                    : Results.UnprocessableEntity(new { error = validation });
            }

            var slug = Slugify(request.Slug ?? request.Title);
            if (await recipeRepository.ExistsBySlugAsync(slug, cancellationToken: cancellationToken))
            {
                return Results.Conflict(new { error = "Recipe slug already exists." });
            }

            var recipe = new Recipe
            {
                AuthorId = request.AuthorId!.Value,
                CategoryId = request.CategoryId,
                Title = request.Title.Trim(),
                Slug = slug,
                Description = request.Description?.Trim() ?? string.Empty,
                Content = request.Content.Trim(),
                PrepTimeMinutes = request.PrepTimeMinutes,
                CookTimeMinutes = request.CookTimeMinutes,
                Servings = request.Servings,
                Difficulty = request.Difficulty,
                Status = RecipeStatus.Draft,
                Nutrition = ToNutrition(request.Nutrition),
                Images = ToImages(request.Images),
                Steps = ToSteps(request.Steps)
            };
            recipe.Ingredients = ToIngredients(request.Ingredients);
            await recipeWriteService.CreateAsync(recipe, cancellationToken);

            return Results.Created($"/api/recipes/{recipe.Slug}", new { recipe.Id, recipe.Slug });
        }).RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, RecipeRequest request, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeRepository recipeRepository, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var recipe = await recipeRepository.GetForUpdateAsync(id, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!IsOwnerOrAdmin(principal, recipe.AuthorId))
            {
                return Results.Forbid();
            }

            request = request with { AuthorId = recipe.AuthorId };
            var validation = await ValidateRequest(request, db, cancellationToken, id);
            if (validation is not null)
            {
                return validation == "Recipe slug already exists."
                    ? Results.Conflict(new { error = validation })
                    : Results.UnprocessableEntity(new { error = validation });
            }

            var slug = Slugify(request.Slug ?? request.Title);
            if (await recipeRepository.ExistsBySlugAsync(slug, id, cancellationToken))
            {
                return Results.Conflict(new { error = "Recipe slug already exists." });
            }

            recipe.CategoryId = request.CategoryId;
            recipe.Title = request.Title.Trim();
            recipe.Slug = slug;
            recipe.Description = request.Description?.Trim() ?? string.Empty;
            recipe.Content = request.Content.Trim();
            recipe.PrepTimeMinutes = request.PrepTimeMinutes;
            recipe.CookTimeMinutes = request.CookTimeMinutes;
            recipe.Servings = request.Servings;
            recipe.Difficulty = request.Difficulty;
            recipe.UpdatedAt = DateTime.UtcNow;
            recipe.Nutrition = ToNutrition(request.Nutrition, recipe.Nutrition);
            await recipeWriteService.ReplaceContentsAsync(
                recipe,
                ToIngredients(request.Ingredients),
                ToSteps(request.Steps),
                ToImages(request.Images),
                cancellationToken);

            return Results.Ok(new { recipe.Id, recipe.Slug });
        }).RequireAuthorization();

        MapStatusEndpoint(group, "publish", RecipeStatus.Published);
        MapStatusEndpoint(group, "unpublish", RecipeStatus.Draft);
        MapStatusEndpoint(group, "archive", RecipeStatus.Archived);
        MapStatusEndpoint(group, "unarchive", RecipeStatus.Draft);

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IRecipeRepository recipeRepository, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var recipe = await recipeRepository.GetByIdAsync(id, includeDetails: false, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!IsOwnerOrAdmin(principal, recipe.AuthorId))
            {
                return Results.Forbid();
            }

            await recipeWriteService.DeleteAsync(recipe, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/ingredients", async (Guid id, IngredientRequest request, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var validation = ValidateIngredient(request);
            if (validation is not null)
            {
                return Results.UnprocessableEntity(new { error = validation });
            }

            if (!await db.Recipes.AnyAsync(recipe => recipe.Id == id && !recipe.IsDeleted, cancellationToken))
            {
                return Results.NotFound();
            }

            if (!await CanManageRecipeAsync(id, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            var ingredient = new RecipeIngredient
            {
                RecipeId = id,
                Name = request.Name.Trim(),
                Quantity = request.Quantity,
                Unit = request.Unit?.Trim(),
                SortOrder = request.SortOrder
            };
            await recipeWriteService.AddIngredientAsync(ingredient, cancellationToken);
            return Results.Created($"/api/recipes/{id}/ingredients/{ingredient.Id}", ingredient);
        }).RequireAuthorization();

        group.MapPut("/{recipeId:guid}/ingredients/{ingredientId:guid}", async (Guid recipeId, Guid ingredientId, IngredientRequest request, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var validation = ValidateIngredient(request);
            if (validation is not null)
            {
                return Results.UnprocessableEntity(new { error = validation });
            }

            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            var ingredient = await db.RecipeIngredients.SingleOrDefaultAsync(item => item.Id == ingredientId && item.RecipeId == recipeId, cancellationToken);
            if (ingredient is null)
            {
                return Results.NotFound();
            }

            ingredient.Name = request.Name.Trim();
            ingredient.Quantity = request.Quantity;
            ingredient.Unit = request.Unit?.Trim();
            ingredient.SortOrder = request.SortOrder;
            await recipeWriteService.UpdateIngredientAsync(ingredient, cancellationToken);
            return Results.Ok(ingredient);
        }).RequireAuthorization();

        group.MapDelete("/{recipeId:guid}/ingredients/{ingredientId:guid}", async (Guid recipeId, Guid ingredientId, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            var ingredient = await db.RecipeIngredients.SingleOrDefaultAsync(item => item.Id == ingredientId && item.RecipeId == recipeId, cancellationToken);
            if (ingredient is null)
            {
                return Results.NotFound();
            }

            await recipeWriteService.DeleteIngredientAsync(ingredient, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/steps", async (Guid id, StepRequest request, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var validation = ValidateStep(request);
            if (validation is not null)
            {
                return Results.UnprocessableEntity(new { error = validation });
            }

            var recipe = await db.Recipes.Include(item => item.Steps).SingleOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!IsOwnerOrAdmin(principal, recipe.AuthorId))
            {
                return Results.Forbid();
            }

            var step = new RecipeStep
            {
                RecipeId = id,
                StepNumber = recipe.Steps.Count == 0 ? 1 : recipe.Steps.Max(item => item.StepNumber) + 1,
                Title = request.Title?.Trim() ?? string.Empty,
                Description = request.Description.Trim()
            };
            await recipeWriteService.AddStepAsync(step, cancellationToken);
            return Results.Created($"/api/recipes/{id}/steps/{step.Id}", step);
        }).RequireAuthorization();

        group.MapPut("/{recipeId:guid}/steps/{stepId:guid}", async (Guid recipeId, Guid stepId, StepRequest request, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            var validation = ValidateStep(request);
            if (validation is not null)
            {
                return Results.UnprocessableEntity(new { error = validation });
            }

            var step = await db.RecipeSteps.SingleOrDefaultAsync(item => item.Id == stepId && item.RecipeId == recipeId, cancellationToken);
            if (step is null)
            {
                return Results.NotFound();
            }

            step.Title = request.Title?.Trim() ?? string.Empty;
            step.Description = request.Description.Trim();
            await recipeWriteService.UpdateStepAsync(step, cancellationToken);
            return Results.Ok(step);
        }).RequireAuthorization();

        group.MapDelete("/{recipeId:guid}/steps/{stepId:guid}", async (Guid recipeId, Guid stepId, ClaimsPrincipal principal, IRecipeWriteService recipeWriteService, ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!await recipeWriteService.DeleteStepAndReorderAsync(recipeId, stepId, cancellationToken))
            {
                return Results.NotFound();
            }
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPatch("/{recipeId:guid}/images/{imageId:guid}/primary", async (Guid recipeId, Guid imageId, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!await recipeWriteService.SetPrimaryImageAsync(recipeId, imageId, cancellationToken))
            {
                return Results.NotFound();
            }

            return Results.Ok(new { Id = imageId, IsPrimary = true });
        }).RequireAuthorization();

        group.MapDelete("/{recipeId:guid}/images/{imageId:guid}", async (Guid recipeId, Guid imageId, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!await recipeWriteService.DeleteImageAsync(recipeId, imageId, cancellationToken))
            {
                return Results.NotFound();
            }
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static void MapStatusEndpoint(RouteGroupBuilder group, string action, RecipeStatus status)
    {
        group.MapPatch("/{id:guid}/" + action, async (Guid id, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var recipe = await db.Recipes
                .Include(item => item.Ingredients)
                .Include(item => item.Steps)
                .SingleOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!IsOwnerOrAdmin(principal, recipe.AuthorId))
            {
                return Results.Forbid();
            }

            if (status == RecipeStatus.Published && (recipe.Ingredients.Count == 0 || recipe.Steps.Count == 0))
            {
                return Results.UnprocessableEntity(new { error = "Recipe must contain at least one ingredient and one step before publishing." });
            }

            recipe.Status = status;
            recipe.PublishedAt = status == RecipeStatus.Published ? DateTime.UtcNow : recipe.PublishedAt;
            recipe.UpdatedAt = DateTime.UtcNow;
            await recipeWriteService.UpdateAsync(recipe, cancellationToken);
            return Results.Ok(new { recipe.Id, status = recipe.Status.ToString() });
        }).RequireAuthorization();
    }

    private static async Task<string?> ValidateRequest(RecipeRequest request, ApplicationDbContext db, CancellationToken cancellationToken, Guid? existingId = null)
    {
        if (!request.AuthorId.HasValue || request.AuthorId.Value == Guid.Empty || request.CategoryId == Guid.Empty || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return "AuthorId, CategoryId, Title and Content are required.";
        }

        if (request.Title.Trim().Length < 5 || request.Title.Trim().Length > 200)
        {
            return "Title must be between 5 and 200 characters.";
        }

        if (request.Description?.Length > 2000)
        {
            return "Description must be at most 2000 characters.";
        }

        if (request.PrepTimeMinutes <= 0 || request.CookTimeMinutes < 0 || request.Servings <= 0)
        {
            return "PrepTimeMinutes must be greater than zero, CookTimeMinutes cannot be negative, and Servings must be greater than zero.";
        }

        if (!Enum.IsDefined(request.Difficulty))
        {
            return "Difficulty is invalid.";
        }

        if (request.Nutrition is not null && new[]
            {
                request.Nutrition.Calories,
                request.Nutrition.Protein,
                request.Nutrition.Carbohydrates,
                request.Nutrition.Fat,
                request.Nutrition.Fiber
            }.Any(value => value < 0))
        {
            return "Nutrition values cannot be negative.";
        }

        foreach (var ingredient in request.Ingredients ?? [])
        {
            var ingredientError = ValidateIngredient(ingredient);
            if (ingredientError is not null)
            {
                return ingredientError;
            }
        }

        if (!await db.Users.AnyAsync(user => user.Id == request.AuthorId!.Value, cancellationToken))
        {
            return "Author was not found.";
        }

        if (!await db.Categories.AnyAsync(category => category.Id == request.CategoryId && !category.IsDeleted, cancellationToken))
        {
            return "Category was not found.";
        }

        var slug = Slugify(request.Slug ?? request.Title);
        if (await db.Recipes.AnyAsync(recipe => recipe.Slug == slug && recipe.Id != existingId, cancellationToken))
        {
            return "Recipe slug already exists.";
        }

        return null;
    }

    private static string? ValidateIngredient(IngredientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
        {
            return "Ingredient name is required and must be at most 200 characters.";
        }

        if (request.Quantity is <= 0)
        {
            return "Ingredient quantity must be greater than zero when provided.";
        }

        if (request.Unit?.Trim().Length > 50)
        {
            return "Ingredient unit must be at most 50 characters.";
        }

        if (request.SortOrder < 0)
        {
            return "Ingredient sort order cannot be negative.";
        }

        return null;
    }

    private static string? ValidateStep(StepRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 2000)
        {
            return "Step description is required and must be at most 2000 characters.";
        }

        if (request.Title?.Trim().Length > 200)
        {
            return "Step title must be at most 200 characters.";
        }

        return null;
    }

    private static RecipeNutrition ToNutrition(NutritionRequest? request, RecipeNutrition? existing = null)
    {
        var nutrition = existing ?? new RecipeNutrition();
        if (request is null)
        {
            return nutrition;
        }

        nutrition.Calories = request.Calories;
        nutrition.Protein = request.Protein;
        nutrition.Carbohydrates = request.Carbohydrates;
        nutrition.Fat = request.Fat;
        nutrition.Fiber = request.Fiber;
        return nutrition;
    }

    private static List<RecipeIngredient> ToIngredients(IEnumerable<IngredientRequest>? requests) =>
        (requests ?? []).Select((request, index) => new RecipeIngredient
        {
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            Unit = request.Unit?.Trim(),
            SortOrder = request.SortOrder == 0 ? index : request.SortOrder
        }).ToList();

    private static List<RecipeImage> ToImages(IEnumerable<ImageRequest>? requests) =>
        (requests ?? [])
            .Where(request => !string.IsNullOrWhiteSpace(request.Url))
            .Select((request, index) => new RecipeImage
            {
                Url = request.Url.Trim(),
                AltText = request.AltText?.Trim(),
                SortOrder = index,
                IsPrimary = index == 0
            })
            .ToList();

    private static List<RecipeStep> ToSteps(IEnumerable<StepRequest>? requests) =>
        (requests ?? [])
            .Where(request => !string.IsNullOrWhiteSpace(request.Description))
            .Select((request, index) => new RecipeStep
            {
                StepNumber = index + 1,
                Title = request.Title?.Trim() ?? string.Empty,
                Description = request.Description.Trim()
            })
            .ToList();

    private static RecipeDetailResponse ToDetail(Recipe recipe) =>
        new(recipe.Id, recipe.Title, recipe.Slug, recipe.Description, recipe.Content, recipe.CategoryId, recipe.Category.Name, recipe.Author.FullName, recipe.PrepTimeMinutes, recipe.CookTimeMinutes, recipe.Servings, recipe.Difficulty.ToString(), recipe.Status.ToString(), recipe.CreatedAt, recipe.Ingredients.Select(ingredient => new IngredientResponse(ingredient.Id, ingredient.Name, ingredient.Quantity, ingredient.Unit, ingredient.SortOrder)), recipe.Steps.OrderBy(step => step.StepNumber).Select(step => new StepResponse(step.Id, step.StepNumber, step.Title, step.Description)), recipe.Nutrition is null ? null : new NutritionResponse(recipe.Nutrition.Calories, recipe.Nutrition.Protein, recipe.Nutrition.Carbohydrates, recipe.Nutrition.Fat, recipe.Nutrition.Fiber), recipe.Images.OrderBy(image => image.SortOrder).Select(image => new ImageResponse(image.Id, image.Url, image.AltText, image.IsPrimary, image.SortOrder)));

    public static string SlugifyForTests(string value) => SlugHelper.Generate(value);

    private static string Slugify(string value) => SlugHelper.Generate(value);

    private static string[] GetSlugAliases(string slug)
    {
        if (slug.StartsWith("rcp-", StringComparison.OrdinalIgnoreCase))
        {
            return [slug, $"recipe-{slug[4..]}"];
        }

        if (slug.StartsWith("recipe-", StringComparison.OrdinalIgnoreCase))
        {
            return [slug, $"rcp-{slug[7..]}"];
        }

        return [slug];
    }

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)
            ? userId
            : Guid.Empty;

    private static bool IsOwnerOrAdmin(ClaimsPrincipal principal, Guid authorId) =>
        principal.IsInRole("Admin") && GetUserId(principal) != Guid.Empty ||
        GetUserId(principal) == authorId;

    private static async Task<bool> CanManageRecipeAsync(Guid recipeId, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken cancellationToken)
    {
        if (principal.IsInRole("Admin"))
        {
            return true;
        }

        var authorId = await db.Recipes
            .Where(recipe => recipe.Id == recipeId && !recipe.IsDeleted)
            .Select(recipe => recipe.AuthorId)
            .SingleOrDefaultAsync(cancellationToken);
        return authorId != Guid.Empty && authorId == GetUserId(principal);
    }
}

public sealed class RecipeQuery
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public RecipeStatus? Status { get; init; }
    public string? Sort { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}

public sealed class RecipeSearchQuery
{
    public string? Query { get; init; }
    public Guid? CategoryId { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}

public sealed record RecipeRequest(Guid? AuthorId, Guid CategoryId, string Title, string? Slug, string? Description, string Content, int PrepTimeMinutes, int CookTimeMinutes, int Servings, DifficultyLevel Difficulty, NutritionRequest? Nutrition, IEnumerable<IngredientRequest>? Ingredients, IEnumerable<ImageRequest>? Images, IEnumerable<StepRequest>? Steps = null);
public sealed record NutritionRequest(decimal Calories, decimal Protein, decimal Carbohydrates, decimal Fat, decimal Fiber);
public sealed record IngredientRequest(string Name, decimal? Quantity, string? Unit, int SortOrder = 0);
public sealed record ImageRequest(string Url, string? AltText);
public sealed record StepRequest(string? Title, string Description);
public sealed record RecipeSummaryResponse(Guid Id, string Title, string Slug, string? Description, string Category, string Author, int PrepTimeMinutes, int CookTimeMinutes, int Servings, string Difficulty, string Status, DateTime CreatedAt, string? PrimaryImageUrl);
public sealed record RecipeDetailResponse(Guid Id, string Title, string Slug, string? Description, string Content, Guid CategoryId, string Category, string Author, int PrepTimeMinutes, int CookTimeMinutes, int Servings, string Difficulty, string Status, DateTime CreatedAt, IEnumerable<IngredientResponse> Ingredients, IEnumerable<StepResponse> Steps, NutritionResponse? Nutrition, IEnumerable<ImageResponse> Images);
public sealed record IngredientResponse(Guid Id, string Name, decimal? Quantity, string? Unit, int SortOrder);
public sealed record StepResponse(Guid Id, int StepNumber, string Title, string Description);
public sealed record NutritionResponse(decimal? Calories, decimal? Protein, decimal? Carbohydrates, decimal? Fat, decimal? Fiber);
public sealed record ImageResponse(Guid Id, string Url, string? AltText, bool IsPrimary, int SortOrder);
public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
