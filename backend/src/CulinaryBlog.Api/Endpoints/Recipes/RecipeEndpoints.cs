using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CulinaryBlog.Api.Helpers;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace CulinaryBlog.Api.Endpoints.Recipes;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this WebApplication app)
    {
        MapRecipeGroup(app.MapGroup("/api/recipes").WithTags("Recipes").RequireRateLimiting("api"));
        MapRecipeGroup(app.MapGroup("/api/v1/recipes").WithTags("Recipes").RequireRateLimiting("api"));
        MapAdminRecipeGroup(app.MapGroup("/api/admin/recipes").WithTags("Admin Recipes").RequireRateLimiting("api"));
        MapAdminRecipeGroup(app.MapGroup("/api/v1/admin/recipes").WithTags("Admin Recipes").RequireRateLimiting("api"));
    }

    private static void MapAdminRecipeGroup(RouteGroupBuilder group)
    {
        group.MapGet("/trash", async (ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            if (!principal.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            var recipes = await db.Recipes
                .AsNoTracking()
                .Where(recipe => recipe.IsDeleted)
                .OrderByDescending(recipe => recipe.DeletedAt ?? recipe.UpdatedAt ?? recipe.CreatedAt)
                .Select(recipe => new DeletedRecipeResponse(
                    recipe.Id,
                    recipe.Title,
                    recipe.Slug,
                    recipe.Status.ToString(),
                    recipe.DeletedAt ?? recipe.UpdatedAt ?? recipe.CreatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(recipes);
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/restore", async (Guid id, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeCacheInvalidator invalidator, CancellationToken cancellationToken) =>
        {
            if (!principal.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!recipe.IsDeleted)
            {
                return Results.UnprocessableEntity(new { error = "Recipe is not soft deleted." });
            }

            var deletedAt = recipe.DeletedAt ?? recipe.UpdatedAt ?? recipe.CreatedAt;
            if (DateTime.UtcNow - deletedAt > RecipePurgeService.RetentionPeriod)
            {
                return Results.Conflict(new { error = "Recipe retention expired and cannot be restored." });
            }

            recipe.IsDeleted = false;
            recipe.DeletedAt = null;
            recipe.UpdatedAt = DateTime.UtcNow;
            await db.RecipeAuditLogs.AddAsync(new RecipeAuditLog
            {
                RecipeId = recipe.Id,
                ActorId = GetUserId(principal),
                Action = "Restore",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            var categorySlug = await db.Categories
                .Where(category => category.Id == recipe.CategoryId)
                .Select(category => category.Slug)
                .SingleOrDefaultAsync(cancellationToken);
            await invalidator.InvalidateAsync(recipe.Id, recipe.Slug, recipe.CategoryId, categorySlug, cancellationToken);
            SetRecipeEtag(httpContext, recipe);
            return Results.Ok(new { recipe.Id, status = recipe.Status.ToString() });
        }).RequireAuthorization();

        group.MapDelete("/{id:guid}/purge", async (Guid id, ClaimsPrincipal principal, IRecipePurgeService purgeService, CancellationToken cancellationToken) =>
        {
            if (!principal.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            if (!await purgeService.PurgeAsync(id, GetUserId(principal), cancellationToken))
            {
                return Results.NotFound();
            }
            return Results.NoContent();
        }).RequireAuthorization();
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
            if (isAdmin)
            {
                if (query.Status.HasValue)
                {
                    recipes = recipes.Where(recipe => recipe.Status == query.Status.Value);
                }
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

        group.MapGet("/search", async ([AsParameters] RecipeSearchQuery query, ApplicationDbContext db, IDistributedCache cache, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(query.Query) || query.Query.Trim().Length < 2 || query.Page is < 1 || query.PageSize is < 1 or > 50)
            {
                return Results.UnprocessableEntity(new { error = "Search query must contain at least 2 characters." });
            }

            var search = query.Query.Trim();
            var page = Math.Max(query.Page ?? 1, 1);
            var pageSize = Math.Clamp(query.PageSize ?? 12, 1, 50);
            var generation = await cache.GetStringAsync("recipes:search:generation", cancellationToken) ?? "initial";
            var cacheSeed = $"{search.ToUpperInvariant()}|{query.CategoryId}|{page}|{pageSize}";
            var cacheHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cacheSeed)));
            var cacheKey = $"recipes:search:{generation}:{cacheHash}";
            var cachedJson = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (cachedJson is not null)
            {
                var cachedResponse = JsonSerializer.Deserialize<PagedResponse<RecipeSummaryResponse>>(cachedJson);
                if (cachedResponse is not null)
                {
                    return Results.Ok(cachedResponse);
                }
            }

            var recipes = db.Recipes
                .AsNoTracking()
                .Where(recipe => !recipe.IsDeleted && recipe.Status == RecipeStatus.Published)
                .Where(recipe => recipe.Title.Contains(search) || (recipe.Description != null && recipe.Description.Contains(search)));

            if (query.CategoryId.HasValue)
            {
                recipes = recipes.Where(recipe => recipe.CategoryId == query.CategoryId.Value);
            }

            var total = await recipes.CountAsync(cancellationToken);
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

            var response = new PagedResponse<RecipeSummaryResponse>(items, page, pageSize, total);
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            }, cancellationToken);
            return Results.Ok(response);
        });

        group.MapGet("/{slug}", async (string slug, ClaimsPrincipal principal, HttpContext httpContext, IDistributedCache cache, IRecipeRepository recipeRepository, CancellationToken cancellationToken) =>
        {
            var generation = await cache.GetStringAsync("recipes:cache:generation", cancellationToken) ?? "initial";
            var cacheKey = $"recipes:detail:{generation}:{Uri.EscapeDataString(slug)}";
            var cachedJson = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (cachedJson is not null)
            {
                var cached = JsonSerializer.Deserialize<CachedRecipeDetail>(cachedJson);
                if (cached is not null)
                {
                    httpContext.Response.Headers.ETag = cached.ETag;
                    return Results.Ok(cached.Detail);
                }
            }

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

            SetRecipeEtag(httpContext, recipe);
            var detail = ToDetail(recipe);
            if (recipe.Status == RecipeStatus.Published)
            {
                await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(new CachedRecipeDetail(detail, $"\"{recipe.xmin}\"")), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                }, cancellationToken);
            }

            return Results.Ok(detail);
        });

        group.MapPost("", async (RecipeRequest request, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeRepository recipeRepository, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
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

            var slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Title, recipeRepository, db, cancellationToken: cancellationToken);

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

            SetRecipeEtag(httpContext, recipe);
            return Results.Created($"/api/recipes/{recipe.Slug}", new { recipe.Id, recipe.Slug });
        }).RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, RecipeRequest request, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeRepository recipeRepository, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
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

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            request = request with { AuthorId = recipe.AuthorId };
            var validation = await ValidateRequest(request, db, cancellationToken, id);
            if (validation is not null)
            {
                return validation == "Recipe slug already exists."
                    ? Results.Conflict(new { error = validation })
                    : Results.UnprocessableEntity(new { error = validation });
            }

            var requestedSlug = request.Slug is null ? recipe.Slug : Slugify(request.Slug);
            if ((recipe.Status is RecipeStatus.Published or RecipeStatus.Archived) &&
                !string.Equals(requestedSlug, recipe.Slug, StringComparison.Ordinal))
            {
                return Results.UnprocessableEntity(new { error = "Published or archived recipes cannot change their slug." });
            }

            var slug = recipe.Status is RecipeStatus.Published or RecipeStatus.Archived
                ? recipe.Slug
                : await GenerateUniqueSlugAsync(request.Slug ?? request.Title, recipeRepository, db, id, cancellationToken);

            var previousSlug = recipe.Slug;
            if (!string.Equals(previousSlug, slug, StringComparison.Ordinal))
            {
                if (!await db.RecipeSlugHistories.AnyAsync(item => item.RecipeId == recipe.Id && item.Slug == previousSlug, cancellationToken))
                {
                    db.RecipeSlugHistories.Add(new RecipeSlugHistory
                    {
                        RecipeId = recipe.Id,
                        Slug = previousSlug,
                        CreatedAt = DateTime.UtcNow
                    });
                }
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

            SetRecipeEtag(httpContext, recipe);
            return Results.Ok(new { recipe.Id, recipe.Slug });
        }).RequireAuthorization();

        MapStatusEndpoint(group, "publish", RecipeStatus.Published);
        MapStatusEndpoint(group, "unpublish", RecipeStatus.Draft);
        MapStatusEndpoint(group, "archive", RecipeStatus.Archived);
        MapStatusEndpoint(group, "unarchive", RecipeStatus.Draft);

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, HttpContext httpContext, IRecipeRepository recipeRepository, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
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

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            await recipeWriteService.DeleteAsync(recipe, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/ingredients", async (Guid id, IngredientRequest request, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var validation = ValidateIngredient(request);
            if (validation is not null)
            {
                return Results.UnprocessableEntity(new { error = validation });
            }

            var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!await CanManageRecipeAsync(id, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            var ingredient = new RecipeIngredient
            {
                RecipeId = id,
                Name = request.Name.Trim(),
                Quantity = ReadIngredientQuantity(request.Quantity),
                Unit = request.Unit?.Trim(),
                Notes = request.Notes?.Trim(),
                SortOrder = request.OrderIndex ?? request.SortOrder
            };
            await recipeWriteService.AddIngredientAsync(ingredient, cancellationToken);
            await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
            return Results.Created($"/api/recipes/{id}/ingredients/{ingredient.Id}", ingredient);
        }).RequireAuthorization();

        group.MapPut("/{recipeId:guid}/ingredients/{ingredientId:guid}", async (Guid recipeId, Guid ingredientId, IngredientRequest request, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var validation = ValidateIngredient(request);
            if (validation is not null)
            {
                return Results.UnprocessableEntity(new { error = validation });
            }

            var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == recipeId && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            var ingredient = await db.RecipeIngredients.SingleOrDefaultAsync(item => item.Id == ingredientId && item.RecipeId == recipeId, cancellationToken);
            if (ingredient is null)
            {
                return Results.NotFound();
            }

            ingredient.Name = request.Name.Trim();
            ingredient.Quantity = ReadIngredientQuantity(request.Quantity);
            ingredient.Unit = request.Unit?.Trim();
            ingredient.Notes = request.Notes?.Trim();
            ingredient.SortOrder = request.OrderIndex ?? request.SortOrder;
            await recipeWriteService.UpdateIngredientAsync(ingredient, cancellationToken);
            await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
            return Results.Ok(ingredient);
        }).RequireAuthorization();

        group.MapDelete("/{recipeId:guid}/ingredients/{ingredientId:guid}", async (Guid recipeId, Guid ingredientId, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == recipeId && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            var ingredient = await db.RecipeIngredients.SingleOrDefaultAsync(item => item.Id == ingredientId && item.RecipeId == recipeId, cancellationToken);
            if (ingredient is null)
            {
                return Results.NotFound();
            }

            await recipeWriteService.DeleteIngredientAsync(ingredient, cancellationToken);
            await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/steps", async (Guid id, StepRequest request, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
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

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            var step = new RecipeStep
            {
                RecipeId = id,
                StepNumber = recipe.Steps.Count == 0 ? 1 : recipe.Steps.Max(item => item.StepNumber) + 1,
                Title = request.Title?.Trim() ?? string.Empty,
                Description = request.Description.Trim()
            };
            await recipeWriteService.AddStepAsync(step, cancellationToken);
            await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
            return Results.Created($"/api/recipes/{id}/steps/{step.Id}", step);
        }).RequireAuthorization();

        group.MapPut("/{recipeId:guid}/steps/{stepId:guid}", async (Guid recipeId, Guid stepId, StepRequest request, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == recipeId && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
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
            await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
            return Results.Ok(step);
        }).RequireAuthorization();

        group.MapDelete("/{recipeId:guid}/steps/{stepId:guid}", async (Guid recipeId, Guid stepId, ClaimsPrincipal principal, HttpContext httpContext, IRecipeWriteService recipeWriteService, ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == recipeId && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            if (!await recipeWriteService.DeleteStepAndReorderAsync(recipeId, stepId, cancellationToken))
            {
                return Results.NotFound();
            }
            await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPatch("/{recipeId:guid}/images/{imageId:guid}/primary", async (Guid recipeId, Guid imageId, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == recipeId && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            if (!await recipeWriteService.SetPrimaryImageAsync(recipeId, imageId, cancellationToken))
            {
                return Results.NotFound();
            }

            await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
            return Results.Ok(new { Id = imageId, IsPrimary = true });
        }).RequireAuthorization();

        group.MapDelete("/{recipeId:guid}/images/{imageId:guid}", async (Guid recipeId, Guid imageId, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken) =>
        {
            var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == recipeId && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            if (!await recipeWriteService.DeleteImageAsync(recipeId, imageId, cancellationToken))
            {
                return Results.NotFound();
            }
            await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static void MapStatusEndpoint(RouteGroupBuilder group, string action, RecipeStatus status)
    {
        async Task<IResult> UpdateStatus(Guid id, ClaimsPrincipal principal, HttpContext httpContext, ApplicationDbContext db, IRecipeWriteService recipeWriteService, CancellationToken cancellationToken)
        {
            var recipe = await db.Recipes
                .Include(item => item.Ingredients)
                .Include(item => item.Steps)
                .Include(item => item.Category)
                .SingleOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!IsOwnerOrAdmin(principal, recipe.AuthorId))
            {
                return Results.Forbid();
            }

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            if (status == RecipeStatus.Published)
            {
                var author = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == recipe.AuthorId, cancellationToken);
                if (author is null || !author.EmailConfirmed)
                {
                    return Results.UnprocessableEntity(new { error = "The recipe author must have a confirmed email before publishing." });
                }

                if (!CanPublishRecipe(recipe, author))
                {
                    return Results.UnprocessableEntity(new { error = "Recipe must have valid fields, an active category, at least one ingredient, and at least one step before publishing." });
                }
            }

            if (recipe.Status == status)
            {
                SetRecipeEtag(httpContext, recipe);
                return Results.Ok(new { recipe.Id, status = recipe.Status.ToString() });
            }

            if (action == "unpublish" && recipe.Status != RecipeStatus.Published ||
                action == "unarchive" && recipe.Status != RecipeStatus.Archived)
            {
                return Results.Conflict(new { error = "Recipe is not in a state that supports this transition." });
            }

            recipe.Status = status;
            if (status == RecipeStatus.Published)
            {
                recipe.PublishedAt ??= DateTime.UtcNow;
            }
            recipe.UpdatedAt = DateTime.UtcNow;
            await recipeWriteService.UpdateAsync(recipe, cancellationToken);
            db.RecipeAuditLogs.Add(new RecipeAuditLog
            {
                RecipeId = recipe.Id,
                ActorId = GetUserId(principal),
                Action = action,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            SetRecipeEtag(httpContext, recipe);
            return Results.Ok(new { recipe.Id, status = recipe.Status.ToString() });
        }

        group.MapPost("/{id:guid}/" + action, UpdateStatus).RequireAuthorization();
        group.MapPatch("/{id:guid}/" + action, UpdateStatus).RequireAuthorization();
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

        return null;
    }

    private static async Task<string> GenerateUniqueSlugAsync(string value, IRecipeRepository repository, ApplicationDbContext db, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var baseSlug = Slugify(value);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "recipe";
        }

        var candidate = baseSlug[..Math.Min(baseSlug.Length, 220)];
        var suffix = 2;
         while (await repository.ExistsBySlugAsync(candidate, excludeId, cancellationToken) ||
             await db.RecipeSlugHistories.AnyAsync(history => history.Slug == candidate && history.RecipeId != excludeId, cancellationToken))
        {
            var suffixText = $"-{suffix++}";
            candidate = $"{baseSlug[..Math.Min(baseSlug.Length, 220 - suffixText.Length)]}{suffixText}";
        }

        return candidate;
    }

    private static string? ValidateIngredient(IngredientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
        {
            return "Ingredient name is required and must be at most 200 characters.";
        }

        if (!TryReadIngredientQuantity(request.Quantity, out var quantity))
        {
            return "Ingredient quantity must be a JSON number or null.";
        }

        if (quantity is <= 0)
        {
            return "Ingredient quantity must be greater than zero when provided.";
        }

        if (request.Unit?.Trim().Length > 50)
        {
            return "Ingredient unit must be at most 50 characters.";
        }

        if ((request.OrderIndex ?? request.SortOrder) < 0)
        {
            return "Ingredient sort order cannot be negative.";
        }

        if (request.Notes?.Length > 500)
        {
            return "Ingredient notes must be at most 500 characters.";
        }

        return null;
    }

    private static bool TryReadIngredientQuantity(JsonElement value, out decimal? quantity)
    {
        quantity = null;
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var parsed))
        {
            return false;
        }

        quantity = parsed;
        return true;
    }

    private static decimal? ReadIngredientQuantity(JsonElement value)
    {
        TryReadIngredientQuantity(value, out var quantity);
        return quantity;
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

        nutrition.Source = NutritionSource.Manual;
        nutrition.Calories = request.Calories;
        nutrition.Protein = request.Protein;
        nutrition.Carbohydrates = request.Carbohydrates;
        nutrition.Fat = request.Fat;
        nutrition.Fiber = request.Fiber;
        return nutrition;
    }

    private static bool CanPublishRecipe(Recipe recipe, ApplicationUser author)
    {
        if (string.IsNullOrWhiteSpace(recipe.Title) || recipe.Title.Trim().Length is < 5 or > 200 ||
            string.IsNullOrWhiteSpace(recipe.Content) || recipe.PrepTimeMinutes <= 0 ||
            recipe.CookTimeMinutes < 0 || recipe.Servings <= 0 || !Enum.IsDefined(recipe.Difficulty) ||
            recipe.Description.Length > 2000 ||
            recipe.Category is null || recipe.Category.IsDeleted ||
            recipe.Ingredients.Count == 0 || recipe.Steps.Count == 0 ||
            new[] { recipe.Nutrition.Calories, recipe.Nutrition.Protein, recipe.Nutrition.Carbohydrates, recipe.Nutrition.Fat, recipe.Nutrition.Fiber, recipe.Nutrition.Sodium }.Any(value => value < 0) ||
            recipe.Ingredients.Any(ingredient => string.IsNullOrWhiteSpace(ingredient.Name) || ingredient.Name.Trim().Length > 200 || ingredient.Quantity is <= 0 || ingredient.Unit?.Length > 50 || ingredient.Notes?.Length > 500 || ingredient.SortOrder < 0) ||
            recipe.Steps.Any(step => string.IsNullOrWhiteSpace(step.Description) || step.Description.Trim().Length > 2000 || step.Title.Trim().Length > 200))
        {
            return false;
        }

        if (!author.EmailConfirmed)
        {
            return false;
        }

        return true;
    }

    private static bool MatchesIfMatchHeader(Recipe recipe, string? ifMatchHeader)
    {
        if (string.IsNullOrWhiteSpace(ifMatchHeader))
        {
            return false;
        }

        var candidate = ifMatchHeader.Trim();
        if (candidate.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[2..];
        }

        candidate = candidate.Trim('"');
        return uint.TryParse(candidate, out var version) && version == recipe.xmin;
    }

    private static void SetRecipeEtag(HttpContext httpContext, Recipe recipe) =>
        httpContext.Response.Headers.ETag = $"\"{recipe.xmin}\"";

    private static async Task TouchRecipeAsync(Recipe recipe, ApplicationDbContext db, HttpContext httpContext, CancellationToken cancellationToken)
    {
        recipe.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        SetRecipeEtag(httpContext, recipe);
    }

    private static List<RecipeIngredient> ToIngredients(IEnumerable<IngredientRequest>? requests) =>
        (requests ?? []).Select((request, index) => new RecipeIngredient
        {
            Name = request.Name.Trim(),
            Quantity = ReadIngredientQuantity(request.Quantity),
            Unit = request.Unit?.Trim(),
            Notes = request.Notes?.Trim(),
            SortOrder = (request.OrderIndex ?? request.SortOrder) == 0 ? index : request.OrderIndex ?? request.SortOrder
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
        new(recipe.Id, recipe.Title, recipe.Slug, recipe.Description, recipe.Content, recipe.CategoryId, recipe.Category.Name, recipe.Author.FullName, recipe.PrepTimeMinutes, recipe.CookTimeMinutes, recipe.Servings, recipe.Difficulty.ToString(), recipe.Status.ToString(), recipe.CreatedAt, recipe.Ingredients.Select(ingredient => new IngredientResponse(ingredient.Id, ingredient.Name, ingredient.Quantity, ingredient.Unit, ingredient.SortOrder, ingredient.Notes)), recipe.Steps.OrderBy(step => step.StepNumber).Select(step => new StepResponse(step.Id, step.StepNumber, step.Title, step.Description)), recipe.Nutrition is null ? null : new NutritionResponse(recipe.Nutrition.Calories, recipe.Nutrition.Protein, recipe.Nutrition.Carbohydrates, recipe.Nutrition.Fat, recipe.Nutrition.Fiber), recipe.Images.OrderBy(image => image.SortOrder).Select(image => new ImageResponse(image.Id, image.Url, image.AltText, image.IsPrimary, image.SortOrder)));

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
public sealed record IngredientRequest(string Name, JsonElement Quantity, string? Unit, int SortOrder = 0, string? Notes = null, int? OrderIndex = null);
public sealed record ImageRequest(string Url, string? AltText);
public sealed record StepRequest(string? Title, string Description);
public sealed record RecipeSummaryResponse(Guid Id, string Title, string Slug, string? Description, string Category, string Author, int PrepTimeMinutes, int CookTimeMinutes, int Servings, string Difficulty, string Status, DateTime CreatedAt, string? PrimaryImageUrl);
public sealed record RecipeDetailResponse(Guid Id, string Title, string Slug, string? Description, string Content, Guid CategoryId, string Category, string Author, int PrepTimeMinutes, int CookTimeMinutes, int Servings, string Difficulty, string Status, DateTime CreatedAt, IEnumerable<IngredientResponse> Ingredients, IEnumerable<StepResponse> Steps, NutritionResponse? Nutrition, IEnumerable<ImageResponse> Images);
public sealed record CachedRecipeDetail(RecipeDetailResponse Detail, string ETag);
public sealed record IngredientResponse(Guid Id, string Name, decimal? Quantity, string? Unit, int SortOrder, string? Notes);
public sealed record StepResponse(Guid Id, int StepNumber, string Title, string Description);
public sealed record NutritionResponse(decimal? Calories, decimal? Protein, decimal? Carbohydrates, decimal? Fat, decimal? Fiber);
public sealed record ImageResponse(Guid Id, string Url, string? AltText, bool IsPrimary, int SortOrder);
public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
public sealed record DeletedRecipeResponse(Guid Id, string Title, string Slug, string Status, DateTime DeletedAt);
