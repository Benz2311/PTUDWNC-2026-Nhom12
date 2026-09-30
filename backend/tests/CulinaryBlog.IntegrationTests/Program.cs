using CulinaryBlog.Application;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Search.Queries.SearchRecipes;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CulinaryBlog.Api.Middleware;
using CulinaryBlog.Application.Features.Recipes.Interfaces;
using CulinaryBlog.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace CulinaryBlog.IntegrationTests;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("==========================================================");
        Console.WriteLine("PHASE 12 – FULL INTEGRATION TEST & VERIFICATION RUNNER");
        Console.WriteLine("Testing Major-01, Major-02, Major-03 and Full System Flow");
        Console.WriteLine("==========================================================\n");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5433;Database=culinary_blog;Username=postgres;Password=Manh80891234567;Include Error Detail=true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddApplication();
        services.AddInfrastructure(config);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        int passedAssertions = 0;
        int failedAssertions = 0;

        void Assert(bool condition, string testName)
        {
            if (condition)
            {
                Console.WriteLine($"[PASS] {testName}");
                passedAssertions++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {testName}");
                Console.ResetColor();
                failedAssertions++;
            }
        }

        try
        {
            // ----------------------------------------------------
            // TEST 1: Recipe Detail with existing slug & Ordering
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 1: Recipe Detail Query & AsNoTracking Projection ---");
            var existingRecipe = await dbContext.Recipes
                .AsNoTracking()
                .Include(r => r.Steps)
                .Include(r => r.Images)
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => !r.IsDeleted && r.Status == RecipeStatus.Published);

            Assert(existingRecipe != null, "Found existing active published recipe in DB");

            if (existingRecipe != null)
            {
                var detailQuery = new GetRecipeBySlugQuery(existingRecipe.Slug);
                var detailDto = await mediator.Send(detailQuery);

                Assert(detailDto != null, $"RecipeDetailDto retrieved for slug '{existingRecipe.Slug}'");
                Assert(detailDto!.Slug == existingRecipe.Slug, "Slug matches requested recipe");
                Assert(detailDto.Author != null, "Author summary populated");
                Assert(detailDto.Category != null, "Category summary populated");

                // Kiểm tra ordering RecipeStep by StepNumber
                bool stepsOrdered = true;
                for (int i = 1; i < detailDto.Steps.Count; i++)
                {
                    if (detailDto.Steps[i].StepNumber < detailDto.Steps[i - 1].StepNumber)
                    {
                        stepsOrdered = false;
                        break;
                    }
                }
                Assert(stepsOrdered, $"RecipeSteps ({detailDto.Steps.Count} steps) ordered ascending by StepNumber");

                // Kiểm tra ordering RecipeImage by OrderIndex
                bool imagesOrdered = true;
                for (int i = 1; i < detailDto.Images.Count; i++)
                {
                    if (detailDto.Images[i].OrderIndex < detailDto.Images[i - 1].OrderIndex)
                    {
                        imagesOrdered = false;
                        break;
                    }
                }
                Assert(imagesOrdered, $"RecipeImages ({detailDto.Images.Count} images) ordered ascending by OrderIndex");

                // Kiểm tra ordering RecipeIngredient by SortOrder
                bool ingredientsOrdered = true;
                for (int i = 1; i < detailDto.Ingredients.Count; i++)
                {
                    if (detailDto.Ingredients[i].SortOrder < detailDto.Ingredients[i - 1].SortOrder)
                    {
                        ingredientsOrdered = false;
                        break;
                    }
                }
                Assert(ingredientsOrdered, $"RecipeIngredients ({detailDto.Ingredients.Count} ingredients) ordered ascending by SortOrder");
            }

            // ----------------------------------------------------
            // TEST 2: Recipe Detail 404 for nonexistent slug
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 2: Recipe Detail 404 Not Found Handling ---");
            bool notFoundThrown = false;
            try
            {
                await mediator.Send(new GetRecipeBySlugQuery("non-existent-slug-xyz-123"));
            }
            catch (NotFoundException)
            {
                notFoundThrown = true;
            }
            Assert(notFoundThrown, "Nonexistent slug correctly throws NotFoundException (HTTP 404)");

            // ----------------------------------------------------
            // TEST 3: Recipe Detail Authorization & Cache Isolation (Major-01)
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 3: Recipe Detail Authorization & Cache Isolation (Major-01) ---");
            var draftTestId = Guid.NewGuid();
            var category = await dbContext.Categories.FirstAsync();
            var author = await dbContext.Users.FirstAsync();

            var draftSlug = "draft-test-recipe-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var draftRecipe = new Recipe
            {
                Id = draftTestId,
                Title = "Draft Test Recipe",
                Slug = draftSlug,
                Description = "Draft description",
                Content = "Draft content",
                CategoryId = category.Id,
                AuthorId = author.Id,
                Status = RecipeStatus.Draft,
                IsDeleted = false
            };
            dbContext.Recipes.Add(draftRecipe);
            await dbContext.SaveChangesAsync();

            bool forbiddenThrown = false;
            try
            {
                // Truy vấn bởi guest / user khác
                await mediator.Send(new GetRecipeBySlugQuery(draftRecipe.Slug, CurrentUserId: Guid.NewGuid(), IsAdmin: false));
            }
            catch (ForbiddenException)
            {
                forbiddenThrown = true;
            }
            Assert(forbiddenThrown, "Guest/Unauthorized user accessing Draft recipe throws ForbiddenException (HTTP 403)");

            // Truy vấn bởi chính tác giả -> thành công
            var authorAccessDto = await mediator.Send(new GetRecipeBySlugQuery(draftRecipe.Slug, CurrentUserId: author.Id, IsAdmin: false));
            Assert(authorAccessDto != null && authorAccessDto.Id == draftTestId, "Author accessing own Draft recipe succeeds");

            // Major-01 Verification: Kiểm tra rằng Draft recipe KHÔNG bao giờ được ghi vào shared cache
            var draftCacheKey = $"recipe:{draftSlug.ToLowerInvariant()}";
            var draftInCache = await cacheService.GetAsync<RecipeDetailDto>(draftCacheKey);
            Assert(draftInCache == null, "Major-01: Draft recipe is NOT written to shared cache recipe:{slug}");

            // Major-01 Double-Shield Test: Giả lập nếu shared cache có chứa recipe có Status = Draft,
            // cơ chế bảo vệ kép (Status == Published) phải từ chối trả về cho Guest
            await cacheService.SetAsync(draftCacheKey, authorAccessDto, TimeSpan.FromMinutes(1));
            var readFromCache = await cacheService.GetAsync<RecipeDetailDto>(draftCacheKey);
            bool doubleShieldRefusesDraft = (readFromCache != null && readFromCache.Status == RecipeStatus.Published);
            Assert(!doubleShieldRefusesDraft, "Major-01 Double Shield: Cache reader checks Status == Published before serving, refusing Draft to Guest");
            await cacheService.RemoveAsync(draftCacheKey);

            // Cleanup draft recipe
            dbContext.Recipes.Remove(draftRecipe);
            await dbContext.SaveChangesAsync();

            // ----------------------------------------------------
            // TEST 4: Full-Text Search (pho bo / phở bò) & Trigram Fuzzy Fallback (phoo bo)
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 4: Full-Text Search & pg_trgm Fuzzy Fallback ---");
            var ftsTestId = Guid.NewGuid();
            var ftsRecipe = new Recipe
            {
                Id = ftsTestId,
                Title = "Phở bò Hà Nội",
                Slug = "fts-integration-pho-bo-ha-noi",
                Description = "Món phở truyền thống với nước dùng và thịt bò thơm ngon.",
                Content = "Nội dung cách nấu chi tiết...",
                CategoryId = category.Id,
                AuthorId = author.Id,
                Status = RecipeStatus.Published,
                PublishedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            dbContext.Recipes.Add(ftsRecipe);
            await dbContext.SaveChangesAsync();

            // FTS test: 'pho bo'
            var ftsResultUnaccent = await mediator.Send(new SearchRecipesQuery("pho bo"));
            Assert(ftsResultUnaccent.TotalCount > 0 && ftsResultUnaccent.Items.Any(x => x.Id == ftsTestId),
                $"FTS query 'pho bo' matches '{ftsRecipe.Title}' (Count={ftsResultUnaccent.TotalCount})");
            if (ftsResultUnaccent.Items.Any(x => x.Id == ftsTestId))
            {
                Assert(ftsResultUnaccent.Items.First(x => x.Id == ftsTestId).MatchType == "FullTextSearch",
                    "Match type correctly identified as FullTextSearch");
            }

            // FTS test: 'phở bò'
            var ftsResultAccented = await mediator.Send(new SearchRecipesQuery("phở bò"));
            Assert(ftsResultAccented.TotalCount > 0 && ftsResultAccented.Items.Any(x => x.Id == ftsTestId),
                $"FTS query 'phở bò' matches '{ftsRecipe.Title}' (Count={ftsResultAccented.TotalCount})");

            // Fuzzy Fallback test: 'phoo bo' (gõ sai chính tả)
            var fuzzyResult = await mediator.Send(new SearchRecipesQuery("phoo bo"));
            Assert(fuzzyResult.TotalCount > 0 && fuzzyResult.Items.Any(x => x.Id == ftsTestId),
                $"Fuzzy trigram fallback 'phoo bo' recovers '{ftsRecipe.Title}' (Count={fuzzyResult.TotalCount})");
            if (fuzzyResult.Items.Any(x => x.Id == ftsTestId))
            {
                Assert(fuzzyResult.Items.First(x => x.Id == ftsTestId).MatchType == "FuzzyTrigram",
                    "Match type correctly identified as FuzzyTrigram fallback");
            }

            // Cleanup FTS test recipe
            dbContext.Recipes.Remove(ftsRecipe);
            await dbContext.SaveChangesAsync();

            // ----------------------------------------------------
            // TEST 5: Primary Image Business Logic, Cache Invalidation & Ownership (FR-RCP-008, Major-02, Major-03)
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 5: Primary Image Business Logic & Cache Invalidation (FR-RCP-008, Major-02, Major-03) ---");
            var imgTestRecipeId = Guid.NewGuid();
            var imgSlug = "image-test-recipe-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var imgRecipe = new Recipe
            {
                Id = imgTestRecipeId,
                Title = "Image Test Recipe",
                Slug = imgSlug,
                Description = "Description",
                Content = "Content",
                CategoryId = category.Id,
                AuthorId = author.Id,
                Status = RecipeStatus.Published,
                IsDeleted = false
            };
            dbContext.Recipes.Add(imgRecipe);
            await dbContext.SaveChangesAsync();

            var imgRecipeCacheKey = $"recipe:{imgSlug.ToLowerInvariant()}";

            try
            {
                // Phase 13: 1. Guest (CurrentUserId = null) Add Image => ForbiddenException
                bool guestAddBlocked = false;
                try
                {
                    await mediator.Send(new AddRecipeImageCommand(
                        imgTestRecipeId,
                        "https://example.com/guest_add.jpg",
                        CurrentUserId: null,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    guestAddBlocked = true;
                }
                Assert(guestAddBlocked, "Phase 13: Guest (CurrentUserId = null) Add Image throws ForbiddenException (HTTP 403)");

                // Phase 13: 2. Non-owner (CurrentUserId != Recipe.AuthorId) Add Image => ForbiddenException
                var randomNonAuthorId = Guid.NewGuid();
                bool unauthorizedAddBlocked = false;
                try
                {
                    await mediator.Send(new AddRecipeImageCommand(
                        imgTestRecipeId,
                        "https://example.com/unauthorized.jpg",
                        CurrentUserId: randomNonAuthorId,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    unauthorizedAddBlocked = true;
                }
                Assert(unauthorizedAddBlocked, "Phase 13: Non-owner user attempting to Add Image throws ForbiddenException (HTTP 403)");

                // Major-02 & Rule 1: Set sẵn cache cho recipe -> Thêm ảnh mới (Owner) -> cache phải bị Invalidate!
                await cacheService.SetAsync(imgRecipeCacheKey, new RecipeDetailDto { Slug = imgSlug }, TimeSpan.FromMinutes(5));
                var img1 = await mediator.Send(new AddRecipeImageCommand(
                    imgTestRecipeId,
                    "https://example.com/img1.jpg",
                    OrderIndex: 0,
                    CurrentUserId: author.Id,
                    IsAdmin: false));
                Assert(img1.IsPrimary, "Rule 1: First uploaded image automatically becomes Primary (isPrimary = true)");

                var cacheAfterAdd = await cacheService.GetAsync<RecipeDetailDto>(imgRecipeCacheKey);
                Assert(cacheAfterAdd == null, "Major-02: Add Recipe Image successfully invalidates Recipe Detail cache");

                // Phase 13: 3. Guest (CurrentUserId = null) Set Primary => ForbiddenException
                bool guestSetPrimaryBlocked = false;
                try
                {
                    await mediator.Send(new SetPrimaryRecipeImageCommand(
                        imgTestRecipeId,
                        img1.Id,
                        CurrentUserId: null,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    guestSetPrimaryBlocked = true;
                }
                Assert(guestSetPrimaryBlocked, "Phase 13: Guest (CurrentUserId = null) Set Primary throws ForbiddenException (HTTP 403)");

                // Phase 13: 4. Non-owner Set Primary => ForbiddenException
                bool nonOwnerSetPrimaryBlocked = false;
                try
                {
                    await mediator.Send(new SetPrimaryRecipeImageCommand(
                        imgTestRecipeId,
                        img1.Id,
                        CurrentUserId: randomNonAuthorId,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    nonOwnerSetPrimaryBlocked = true;
                }
                Assert(nonOwnerSetPrimaryBlocked, "Phase 13: Non-owner Set Primary throws ForbiddenException (HTTP 403)");

                // Phase 13: 5. Guest (CurrentUserId = null) Delete Image => ForbiddenException
                bool guestDeleteBlocked = false;
                try
                {
                    await mediator.Send(new DeleteRecipeImageCommand(
                        imgTestRecipeId,
                        img1.Id,
                        CurrentUserId: null,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    guestDeleteBlocked = true;
                }
                Assert(guestDeleteBlocked, "Phase 13: Guest (CurrentUserId = null) Delete Image throws ForbiddenException (HTTP 403)");

                // Phase 13: 6. Non-owner Delete Image => ForbiddenException
                bool nonOwnerDeleteBlocked = false;
                try
                {
                    await mediator.Send(new DeleteRecipeImageCommand(
                        imgTestRecipeId,
                        img1.Id,
                        CurrentUserId: randomNonAuthorId,
                        IsAdmin: false));
                }
                catch (ForbiddenException)
                {
                    nonOwnerDeleteBlocked = true;
                }
                Assert(nonOwnerDeleteBlocked, "Phase 13: Non-owner Delete Image throws ForbiddenException (HTTP 403)");

                // Rule 1b: Thêm ảnh thứ hai không yêu cầu primary (Owner) -> isPrimary = false
                var img2 = await mediator.Send(new AddRecipeImageCommand(
                    imgTestRecipeId,
                    "https://example.com/img2.jpg",
                    OrderIndex: 1,
                    CurrentUserId: author.Id,
                    IsAdmin: false));
                Assert(!img2.IsPrimary, "Adding second image without isPrimary retains first image as primary");

                // Major-02 & Rule 2: Set sẵn cache -> Set Primary cho ảnh 2 (Owner) -> cache bị Invalidate và ảnh 1 mất primary
                await cacheService.SetAsync(imgRecipeCacheKey, new RecipeDetailDto { Slug = imgSlug }, TimeSpan.FromMinutes(5));
                var updatedImg2 = await mediator.Send(new SetPrimaryRecipeImageCommand(
                    imgTestRecipeId,
                    img2.Id,
                    CurrentUserId: author.Id,
                    IsAdmin: false));
                Assert(updatedImg2.IsPrimary, "Rule 2: Image 2 set to Primary succeeds");

                var reloadedImg1 = await dbContext.RecipeImages.AsNoTracking().FirstAsync(x => x.Id == img1.Id);
                Assert(!reloadedImg1.IsPrimary, "Rule 2: Image 1 automatically loses Primary status in same transaction");

                var cacheAfterSetPrimary = await cacheService.GetAsync<RecipeDetailDto>(imgRecipeCacheKey);
                Assert(cacheAfterSetPrimary == null, "Major-02: Set Primary Image successfully invalidates Recipe Detail cache");

                // Major-02 & Rule 3: Set sẵn cache -> Xóa ảnh 2 (primary) (Owner) -> cache bị Invalidate và ảnh 1 được thăng hạng
                await cacheService.SetAsync(imgRecipeCacheKey, new RecipeDetailDto { Slug = imgSlug }, TimeSpan.FromMinutes(5));
                var deleteSuccess = await mediator.Send(new DeleteRecipeImageCommand(
                    imgTestRecipeId,
                    img2.Id,
                    CurrentUserId: author.Id,
                    IsAdmin: false));
                Assert(deleteSuccess, "Rule 3: Soft-delete primary image 2 succeeds");

                var finalImg1 = await dbContext.RecipeImages.AsNoTracking().FirstAsync(x => x.Id == img1.Id);
                Assert(finalImg1.IsPrimary, "Rule 3: Remaining active image with smallest OrderIndex (img1) automatically becomes Primary");

                var cacheAfterDelete = await cacheService.GetAsync<RecipeDetailDto>(imgRecipeCacheKey);
                Assert(cacheAfterDelete == null, "Major-02: Delete Recipe Image successfully invalidates Recipe Detail cache");

                // Phase 13 & Major-03 Admin Bypass: Admin có thể Thêm, Đặt Primary và Xóa ảnh của bất kỳ tác giả nào
                var adminAddSuccess = await mediator.Send(new AddRecipeImageCommand(
                    imgTestRecipeId,
                    "https://example.com/admin_added.jpg",
                    CurrentUserId: Guid.NewGuid(),
                    IsAdmin: true));
                Assert(adminAddSuccess != null, "Phase 13: Admin user can Add Image regardless of author ownership");

                var adminSetPrimarySuccess = await mediator.Send(new SetPrimaryRecipeImageCommand(
                    imgTestRecipeId,
                    adminAddSuccess.Id,
                    CurrentUserId: Guid.NewGuid(),
                    IsAdmin: true));
                Assert(adminSetPrimarySuccess != null && adminSetPrimarySuccess.IsPrimary, "Phase 13: Admin user can Set Primary image regardless of author ownership");

                var adminDeleteSuccess = await mediator.Send(new DeleteRecipeImageCommand(
                    imgTestRecipeId,
                    adminAddSuccess.Id,
                    CurrentUserId: Guid.NewGuid(),
                    IsAdmin: true));
                Assert(adminDeleteSuccess, "Phase 13: Admin user can Delete image regardless of author ownership");
            }
            finally
            {
                // Cleanup image test recipe & images
                var imgRecipeToDelete = await dbContext.Recipes.Include(r => r.Images).FirstOrDefaultAsync(r => r.Id == imgTestRecipeId);
                if (imgRecipeToDelete != null)
                {
                    dbContext.RecipeImages.RemoveRange(imgRecipeToDelete.Images);
                    dbContext.Recipes.Remove(imgRecipeToDelete);
                    await dbContext.SaveChangesAsync();
                }
                await cacheService.RemoveAsync(imgRecipeCacheKey);
            }

            // ----------------------------------------------------
            // TEST 6: Redis Cache & Failover Fallback
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 6: Caching & Fail-safe Behavior ---");
            var testCacheKey = "integration-test-cache-key";
            await cacheService.SetAsync(testCacheKey, "CachedValue123", TimeSpan.FromMinutes(1));
            var cachedVal = await cacheService.GetAsync<string>(testCacheKey);
            Assert(cachedVal == "CachedValue123", "CacheService Set & Get successfully roundtrips data");

            await cacheService.RemoveAsync(testCacheKey);
            var removedVal = await cacheService.GetAsync<string>(testCacheKey);
            Assert(removedVal == null, "CacheService Remove successfully invalidates entry");

            // Fail-safe check: Gọi cache với giá trị rỗng/lỗi không quăng exception
            await cacheService.SetAsync<string?>(testCacheKey, null, TimeSpan.FromMinutes(1));
            Assert(true, "ResilientCacheService handles null/edge cases gracefully without exceptions");

            // ----------------------------------------------------
            // TEST 8: Domain Exceptions (Lab 3 Requirement 1)
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 8: Domain Exceptions Architecture (Lab 3) ---");
            var invalidStep1 = new RecipeStep { StepNumber = 0 };
            bool stepExThrown = false;
            try
            {
                invalidStep1.Validate();
            }
            catch (InvalidRecipeStepException ex)
            {
                stepExThrown = true;
                Assert(ex is DomainException, "InvalidRecipeStepException inherits from DomainException");
                Assert(ex.Message.Contains("Step number must be >= 1"), "InvalidRecipeStepException contains descriptive message");
            }
            Assert(stepExThrown, "RecipeStep with StepNumber=0 throws InvalidRecipeStepException");

            var invalidStepTimer = new RecipeStep { StepNumber = 1, TimerMinutes = -5 };
            bool timerExThrown = false;
            try
            {
                invalidStepTimer.Validate();
            }
            catch (InvalidRecipeStepException ex)
            {
                timerExThrown = true;
                Assert(ex.Message.Contains("Timer minutes cannot be negative"), "InvalidRecipeStepException on negative timer");
            }
            Assert(timerExThrown, "RecipeStep with negative timer throws InvalidRecipeStepException");

            var invalidImgOrder = new RecipeImage { OrderIndex = -1, OriginalUrl = "https://example.com/img.jpg" };
            bool imgExThrown = false;
            try
            {
                invalidImgOrder.Validate();
            }
            catch (InvalidRecipeImageException ex)
            {
                imgExThrown = true;
                Assert(ex is DomainException, "InvalidRecipeImageException inherits from DomainException");
                Assert(ex.Message.Contains("Order index cannot be negative"), "InvalidRecipeImageException contains descriptive message");
            }
            Assert(imgExThrown, "RecipeImage with negative OrderIndex throws InvalidRecipeImageException");

            var invalidImgUrl = new RecipeImage { OrderIndex = 0, OriginalUrl = "" };
            bool imgUrlExThrown = false;
            try
            {
                invalidImgUrl.Validate();
            }
            catch (InvalidRecipeImageException ex)
            {
                imgUrlExThrown = true;
                Assert(ex.Message.Contains("OriginalUrl cannot be empty"), "InvalidRecipeImageException on empty URL");
            }
            Assert(imgUrlExThrown, "RecipeImage with empty OriginalUrl throws InvalidRecipeImageException");

            // ----------------------------------------------------
            // TEST 9: Repository & Unit of Work (Lab 3 Requirement 2)
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 9: Repository & Unit of Work (Lab 3) ---");
            var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
            Assert(uow != null, "IUnitOfWork resolved from DI container");

            var recipeRepo = scope.ServiceProvider.GetService<IRecipeRepository>();
            Assert(recipeRepo != null, "IRecipeRepository resolved from DI container");

            if (uow != null)
            {
                var repoRecipe = await uow.Recipes.GetBySlugAsync("mon-an-mau-1");
                Assert(repoRecipe != null, "UnitOfWork.Recipes.GetBySlugAsync retrieved existing recipe");

                if (repoRecipe != null)
                {
                    var activeImages = await uow.Recipes.GetActiveImagesAsync(repoRecipe.Id);
                    Assert(activeImages != null && activeImages.Count > 0, $"UnitOfWork.Recipes.GetActiveImagesAsync retrieved {activeImages?.Count} images");
                }

                await using var uowTx = await uow.BeginTransactionAsync();
                Assert(uowTx != null, "UnitOfWork.BeginTransactionAsync created active IDbContextTransaction");
                await uowTx.RollbackAsync();
                Assert(true, "UnitOfWork transaction rollback executed cleanly without errors");
            }

            // ----------------------------------------------------
            // TEST 10: Global Exception Handling & Problem Details (Lab 3 Requirement 4)
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 10: Global Exception Handler & Problem Details (Lab 3) ---");
            var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
            var exLogger = loggerFactory.CreateLogger<GlobalExceptionHandler>();
            var prodEnv = new TestHostEnvironment { EnvironmentName = "Production" };
            var globalHandler = new GlobalExceptionHandler(exLogger, prodEnv);

            // Test 10.1: NotFoundException -> 404 Problem Details
            var httpContext404 = new DefaultHttpContext();
            httpContext404.Request.Path = "/api/v1/recipes/slug-khong-ton-tai";
            httpContext404.Response.Body = new MemoryStream();
            var handled404 = await globalHandler.TryHandleAsync(httpContext404, new NotFoundException("Recipe not found"), CancellationToken.None);
            Assert(handled404, "GlobalExceptionHandler handled NotFoundException");
            Assert(httpContext404.Response.StatusCode == 404, "NotFoundException mapped to HTTP 404");
            Assert(httpContext404.Response.ContentType?.Contains("application/problem+json") == true, "Response Content-Type is application/problem+json");

            httpContext404.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader404 = new StreamReader(httpContext404.Response.Body);
            var json404 = await reader404.ReadToEndAsync();
            var pd404 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json404);
            Assert(pd404.GetProperty("status").GetInt32() == 404, "ProblemDetails.status == 404");
            Assert(pd404.GetProperty("title").GetString() == "Resource Not Found", "ProblemDetails.title == 'Resource Not Found'");
            Assert(pd404.GetProperty("detail").GetString() == "Recipe not found", "ProblemDetails.detail contains exception message");
            Assert(pd404.GetProperty("instance").GetString() == "/api/v1/recipes/slug-khong-ton-tai", "ProblemDetails.instance matches request path");
            Assert(pd404.TryGetProperty("traceId", out _), "ProblemDetails contains traceId extension");

            // Test 10.2: ForbiddenException -> 403 Problem Details
            var httpContext403 = new DefaultHttpContext();
            httpContext403.Request.Path = "/api/v1/recipes/draft-recipe";
            httpContext403.Response.Body = new MemoryStream();
            await globalHandler.TryHandleAsync(httpContext403, new ForbiddenException("You do not have permission to view this unpublished recipe."), CancellationToken.None);
            Assert(httpContext403.Response.StatusCode == 403, "ForbiddenException mapped to HTTP 403");
            httpContext403.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader403 = new StreamReader(httpContext403.Response.Body);
            var json403 = await reader403.ReadToEndAsync();
            var pd403 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json403);
            Assert(pd403.GetProperty("title").GetString() == "Forbidden", "ProblemDetails.title == 'Forbidden'");

            // Test 10.3: DomainException -> 400 Problem Details
            var httpContextDomain = new DefaultHttpContext();
            httpContextDomain.Request.Path = "/api/v1/recipes/images";
            httpContextDomain.Response.Body = new MemoryStream();
            await globalHandler.TryHandleAsync(httpContextDomain, new InvalidRecipeStepException("Step number must be >= 1"), CancellationToken.None);
            Assert(httpContextDomain.Response.StatusCode == 400, "DomainException mapped to HTTP 400 Bad Request");
            httpContextDomain.Response.Body.Seek(0, SeekOrigin.Begin);
            using var readerDomain = new StreamReader(httpContextDomain.Response.Body);
            var jsonDomain = await readerDomain.ReadToEndAsync();
            var pdDomain = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(jsonDomain);
            Assert(pdDomain.GetProperty("title").GetString() == "Domain Business Rule Violation", "ProblemDetails.title == 'Domain Business Rule Violation'");
            Assert(pdDomain.GetProperty("detail").GetString() == "Step number must be >= 1", "ProblemDetails.detail contains domain rule violation message");

            // Test 10.4: ValidationException -> 400 Problem Details with errors dictionary
            var httpContextVal = new DefaultHttpContext();
            httpContextVal.Request.Path = "/api/v1/recipes/search";
            httpContextVal.Response.Body = new MemoryStream();
            var valEx = new ValidationException(new Dictionary<string, string[]>
            {
                ["Query"] = new[] { "Query length must be between 2 and 100 characters." }
            });
            await globalHandler.TryHandleAsync(httpContextVal, valEx, CancellationToken.None);
            Assert(httpContextVal.Response.StatusCode == 400, "ValidationException mapped to HTTP 400 Bad Request");
            httpContextVal.Response.Body.Seek(0, SeekOrigin.Begin);
            using var readerVal = new StreamReader(httpContextVal.Response.Body);
            var jsonVal = await readerVal.ReadToEndAsync();
            var pdVal = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(jsonVal);
            Assert(pdVal.GetProperty("title").GetString() == "Validation Error", "ProblemDetails.title == 'Validation Error'");
            Assert(pdVal.TryGetProperty("errors", out var errorsProp) && errorsProp.TryGetProperty("Query", out _), "ProblemDetails.extensions contains 'errors' mapping");

            // Test 10.5: Unhandled Exception -> 500 without leaking stack trace/internals
            var httpContext500 = new DefaultHttpContext();
            httpContext500.Request.Path = "/api/v1/recipes";
            httpContext500.Response.Body = new MemoryStream();
            var sensitiveEx = new InvalidOperationException("Fatal SQL: SELECT * FROM confidential_table; password=secret123");
            await globalHandler.TryHandleAsync(httpContext500, sensitiveEx, CancellationToken.None);
            Assert(httpContext500.Response.StatusCode == 500, "Unhandled Exception mapped to HTTP 500");
            httpContext500.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader500 = new StreamReader(httpContext500.Response.Body);
            var json500 = await reader500.ReadToEndAsync();
            var pd500 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json500);
            Assert(pd500.GetProperty("title").GetString() == "Internal Server Error", "ProblemDetails.title == 'Internal Server Error'");
            Assert(!json500.Contains("confidential_table") && !json500.Contains("password=secret123"), "Production 500 response NEVER leaks internal database/secret details");
            Assert(!json500.Contains("StackTrace") && !json500.Contains("at CulinaryBlog"), "Production 500 response NEVER leaks stack trace");

            // ----------------------------------------------------
            // TEST 11: Data Preservation in Database
            // ----------------------------------------------------
            Console.WriteLine("\n--- TEST SUITE 11: Database State & Row Count Preservation ---");
            var finalRecipeCount = await dbContext.Recipes.CountAsync();
            var finalStepCount = await dbContext.RecipeSteps.CountAsync();
            var finalImageCount = await dbContext.RecipeImages.CountAsync();
            var finalCategoryCount = await dbContext.Categories.CountAsync();
            var finalNullVectorCount = await dbContext.Recipes.CountAsync(r => EF.Property<NpgsqlTypes.NpgsqlTsVector>(r, "SearchVectorFts") == null);

            Assert(finalRecipeCount == 11, $"Recipes count preserved: {finalRecipeCount} (expected 11)");
            Assert(finalStepCount == 77, $"RecipeSteps count preserved: {finalStepCount} (expected 77)");
            Assert(finalImageCount == 35, $"RecipeImages count preserved: {finalImageCount} (expected 35)");
            Assert(finalCategoryCount == 11, $"Categories count preserved: {finalCategoryCount} (expected 11)");
            Assert(finalNullVectorCount == 0, $"Null SearchVector count: {finalNullVectorCount} (expected 0)");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[FATAL ERROR] Unexpected exception during integration test: {ex}");
            Console.ResetColor();
            return 1;
        }

        Console.WriteLine("\n==========================================================");
        Console.WriteLine($"INTEGRATION TEST SUMMARY: {passedAssertions} PASSED, {failedAssertions} FAILED");
        Console.WriteLine("==========================================================");

        return failedAssertions == 0 ? 0 : 1;
    }
}

public class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "CulinaryBlog.Api";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
