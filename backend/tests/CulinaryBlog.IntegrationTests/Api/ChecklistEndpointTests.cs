using System.Security.Claims;
using System.Text.Encodings.Web;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Categories.Commands.CreateCategory;
using CulinaryBlog.Application.Features.Categories.Commands.DeleteCategory;
using CulinaryBlog.Application.Features.Categories.Commands.UpdateCategory;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategoryStatistics;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Features.Recipes.Queries.GetDeletedRecipes;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;
using CulinaryBlog.Domain.Enums;
using RecipeDetailApiDto = CulinaryBlog.Application.Features.Recipes.Dtos.RecipeDetailDto;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Http.Json;
using Xunit;

namespace CulinaryBlog.IntegrationTests.Api;

public sealed class ChecklistEndpointTests
{
    [Fact]
    public async Task CategoryList_CacheMissLoadsAndCachesForThirtyMinutes()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        IReadOnlyList<CategoryDto> categories =
        [new(Guid.NewGuid(), "Main", "main", null, null, 1, 3)];
        cache.Setup(item => item.GetAsync<IReadOnlyList<CategoryDto>>("categories:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<CategoryDto>?)null);
        cache.Setup(item => item.SetAsync("categories:all", categories, TimeSpan.FromMinutes(30), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(item => item.Send(It.IsAny<GetCategoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            var response = await client.GetAsync("/api/v1/categories/");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            sender.Verify(item => item.Send(It.IsAny<GetCategoriesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
            cache.Verify(item => item.SetAsync("categories:all", categories, TimeSpan.FromMinutes(30), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task CategoryList_CacheHitSkipsRepositoryQuery()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        IReadOnlyList<CategoryDto> categories =
        [new(Guid.NewGuid(), "Main", "main", null, null, 1, 3)];
        cache.Setup(item => item.GetAsync<IReadOnlyList<CategoryDto>>("categories:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            var response = await client.GetAsync("/api/v1/categories/");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            sender.Verify(item => item.Send(It.IsAny<GetCategoriesQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task CategoryDetail_PassesAuthorIdAndPagination()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var responseDto = new CategoryRecipesResponseDto(
            new RecipeCategoryDto(Guid.NewGuid(), "Main", "main", null),
            new PagedResultDto<RecipeListItemDto>([], 0, 2, 5, 0));
        sender.Setup(item => item.Send(
                It.Is<GetRecipesByCategoryQuery>(query =>
                    query.CategorySlug == "main" &&
                    query.AuthorId == authorId &&
                    query.Page == 2 &&
                    query.PageSize == 5),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Author", authorId);
            var response = await client.GetAsync("/api/v1/categories/main?page=2&pageSize=5");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            sender.Verify(item => item.Send(It.IsAny<GetRecipesByCategoryQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task CategoryDetail_GuestWithUnknownSlugGetsNotFound()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        sender.Setup(item => item.Send(
                It.Is<GetRecipesByCategoryQuery>(query => query.AuthorId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryRecipesResponseDto?)null);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            var response = await client.GetAsync("/api/v1/categories/unknown");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task CreateCategory_AdminReturnsCreated_AndNonAdminIsForbidden()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        var category = new CategoryDto(Guid.NewGuid(), "Main", "main", null, null, 0, 0);
        sender.Setup(item => item.Send(It.IsAny<CreateCategoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Admin", Guid.NewGuid());
            var response = await client.PostAsJsonAsync(
                "/api/v1/categories/",
                new { name = "Main" });
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

            client.DefaultRequestHeaders.Remove("X-Test-Role");
            SetUser(client, "Author", Guid.NewGuid());
            var forbidden = await client.PostAsJsonAsync(
                "/api/v1/categories/",
                new { name = "Other" });
            forbidden.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task CreateCategory_DuplicateNameMapsToConflict()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        sender.Setup(item => item.Send(It.IsAny<CreateCategoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryDto?)null);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Admin", Guid.NewGuid());
            var response = await client.PostAsJsonAsync(
                "/api/v1/categories/",
                new { name = "Main" });

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task UpdateCategory_AdminMapsMissingAndDuplicateResults()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        sender.SetupSequence(item => item.Send(It.IsAny<UpdateCategoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateCategoryResult.NotFound)
            .ReturnsAsync(UpdateCategoryResult.NameAlreadyExists);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Admin", Guid.NewGuid());
            var missing = await client.PutAsJsonAsync(
                $"/api/v1/categories/{Guid.NewGuid()}",
                new { name = "Missing", description = (string?)null });
            var duplicate = await client.PutAsJsonAsync(
                $"/api/v1/categories/{Guid.NewGuid()}",
                new { name = "Taken", description = "Description" });

            missing.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            duplicate.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task UpdateCategory_AdminSuccessReturnsNoContent()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        sender.Setup(item => item.Send(
                It.IsAny<UpdateCategoryCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateCategoryResult.Updated);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Admin", Guid.NewGuid());
            var response = await client.PutAsJsonAsync(
                $"/api/v1/categories/{Guid.NewGuid()}",
                new { name = "Updated", description = "Description" });

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task DeleteCategory_ActiveRecipesMapsToConflict()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        sender.Setup(item => item.Send(It.IsAny<DeleteCategoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeleteCategoryResult.HasRecipes);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Admin", Guid.NewGuid());
            var response = await client.DeleteAsync($"/api/v1/categories/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task DeleteCategory_AdminSuccessReturnsNoContent()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        sender.Setup(item => item.Send(
                It.IsAny<DeleteCategoryCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeleteCategoryResult.Deleted);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Admin", Guid.NewGuid());
            var response = await client.DeleteAsync($"/api/v1/categories/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task RecipeList_PublicMissAppliesFiltersPaginationAndCachesOneMinute()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        var result = new PagedResultDto<RecipeListItemDto>([], 0, 2, 5, 0);
        cache.Setup(item => item.GetAsync<PagedResultDto<RecipeListItemDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResultDto<RecipeListItemDto>?)null);
        sender.Setup(item => item.Send(
                It.Is<GetRecipesQuery>(query =>
                    query.Page == 2 &&
                    query.PageSize == 5 &&
                    query.Options!.Search == "soup" &&
                    query.Options.CategorySlug == "main" &&
                    query.Options.Difficulty == DifficultyLevel.Easy &&
                    query.Options.MaxCookTimeMinutes == 30 &&
                    query.Options.MaxTotalTimeMinutes == 50 &&
                    query.Options.SortBy == RecipeSortField.Title &&
                    !query.Options.SortDescending &&
                    query.Options.AuthorId == null &&
                    query.Options.Status == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            var response = await client.GetAsync(
                "/api/v1/recipes?page=2&pageSize=5&search=soup&categorySlug=main&difficulty=Easy&maxCookTimeMinutes=30&maxTotalTimeMinutes=50&sortBy=title&sortDirection=asc");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            cache.Verify(item => item.SetAsync(
                It.Is<string>(key => key.StartsWith("recipes:list:", StringComparison.Ordinal)),
                result,
                TimeSpan.FromMinutes(1),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task RecipeList_PublicCacheHitSkipsQuery()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        var result = new PagedResultDto<RecipeListItemDto>([], 0, 1, 12, 0);
        cache.Setup(item => item.GetAsync<PagedResultDto<RecipeListItemDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            var response = await client.GetAsync("/api/v1/recipes");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            sender.Verify(item => item.Send(It.IsAny<GetRecipesQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task RecipeList_MineUsesAuthorAndStatusWithoutSharedCache()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        sender.Setup(item => item.Send(
                It.Is<GetRecipesQuery>(query =>
                    query.Options!.AuthorId == authorId &&
                    query.Options.Status == RecipeStatus.Draft),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultDto<RecipeListItemDto>([], 0, 1, 12, 0));

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Author", authorId);
            var response = await client.GetAsync("/api/v1/recipes?mine=true&status=Draft");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            cache.Verify(item => item.GetAsync<PagedResultDto<RecipeListItemDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            cache.Verify(item => item.SetAsync(
                It.IsAny<string>(),
                It.IsAny<PagedResultDto<RecipeListItemDto>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task RecipeList_AuthorCannotFilterByAuthorId_ButAdminCan()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        sender.Setup(item => item.Send(It.IsAny<GetRecipesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultDto<RecipeListItemDto>([], 0, 1, 12, 0));

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Author", Guid.NewGuid());
            var denied = await client.GetAsync($"/api/v1/recipes?authorId={authorId}");
            denied.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
            sender.Verify(item => item.Send(It.IsAny<GetRecipesQuery>(), It.IsAny<CancellationToken>()), Times.Never);

            client.DefaultRequestHeaders.Remove("X-Test-Role");
            SetUser(client, "Admin", Guid.NewGuid());
            var allowed = await client.GetAsync($"/api/v1/recipes?authorId={authorId}&status=Archived");
            allowed.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            sender.Verify(item => item.Send(
                It.Is<GetRecipesQuery>(query =>
                    query.Options!.AuthorId == authorId &&
                    query.Options.Status == RecipeStatus.Archived),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task RecipeDetail_PublishedIsCachedForFiveMinutes()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        var recipe = MakeRecipeDetail(RecipeStatus.Published);
        cache.Setup(item => item.GetAsync<RecipeDetailApiDto>("recipe:published", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecipeDetailApiDto?)null);
        sender.Setup(item => item.Send(It.IsAny<GetRecipeBySlugQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        cache.Setup(item => item.SetAsync("recipe:published", recipe, TimeSpan.FromMinutes(5), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            var response = await client.GetAsync("/api/v1/recipes/Published");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            sender.Verify(item => item.Send(
                It.Is<GetRecipeBySlugQuery>(query => query.CurrentUserId == null && !query.IsAdmin),
                It.IsAny<CancellationToken>()), Times.Once);
            cache.Verify(item => item.SetAsync("recipe:published", recipe, TimeSpan.FromMinutes(5), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task RecipeDetail_PublishedCacheHitSkipsQuery()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        var recipe = MakeRecipeDetail(RecipeStatus.Published);
        recipe.Slug = "cached";
        cache.Setup(item => item.GetAsync<RecipeDetailApiDto>("recipe:cached", It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            var response = await client.GetAsync("/api/v1/recipes/Cached");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            sender.Verify(item => item.Send(It.IsAny<GetRecipeBySlugQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task RecipeDetail_GuestCannotReadDraft_AndOwnerDoesNotUseSharedCache()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        cache.Setup(item => item.GetAsync<RecipeDetailApiDto>("recipe:draft", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecipeDetailApiDto?)null);
        sender.Setup(item => item.Send(It.IsAny<GetRecipeBySlugQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException());
        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            var denied = await client.GetAsync("/api/v1/recipes/Draft");
            denied.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);

            var ownerId = Guid.NewGuid();
            var draft = MakeRecipeDetail(RecipeStatus.Draft);
            sender.Setup(item => item.Send(
                    It.Is<GetRecipeBySlugQuery>(query => query.CurrentUserId == ownerId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(draft);
            SetUser(client, "Author", ownerId);
            var allowed = await client.GetAsync("/api/v1/recipes/Draft");

            allowed.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            cache.Verify(item => item.SetAsync(
                It.IsAny<string>(),
                It.IsAny<RecipeDetailApiDto>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task RecipeDetail_AdminCanReadArchivedWithoutCachingIt()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        var recipe = MakeRecipeDetail(RecipeStatus.Archived);
        cache.Setup(item => item.GetAsync<RecipeDetailApiDto>("recipe:archived", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecipeDetailApiDto?)null);
        sender.Setup(item => item.Send(
                It.Is<GetRecipeBySlugQuery>(query => query.IsAdmin),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Admin", Guid.NewGuid());
            var response = await client.GetAsync("/api/v1/recipes/Archived");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            cache.Verify(item => item.SetAsync(
                It.IsAny<string>(),
                It.IsAny<RecipeDetailApiDto>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task AdminTrash_RequiresAdminAndPassesPagination()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        var result = new PagedResultDto<RecipeTrashItemDto>([], 0, 2, 4, 0);
        sender.Setup(item => item.Send(
                It.Is<GetDeletedRecipesQuery>(query => query.Page == 2 && query.PageSize == 4),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            SetUser(client, "Author", Guid.NewGuid());
            var denied = await client.GetAsync("/api/v1/admin/recipes/trash?page=2&pageSize=4");
            denied.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);

            client.DefaultRequestHeaders.Remove("X-Test-Role");
            SetUser(client, "Admin", Guid.NewGuid());
            var allowed = await client.GetAsync("/api/v1/admin/recipes/trash?page=2&pageSize=4");
            allowed.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            sender.Verify(item => item.Send(It.IsAny<GetDeletedRecipesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task DashboardStatistics_InvokesStatisticsQuery()
    {
        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();
        sender.Setup(item => item.Send(It.IsAny<GetCategoryStatisticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CategoryStatisticsDto(0, 0, 0, 0, 0, [], [], []));

        var (app, client) = await StartApi(sender, cache);
        await using (app)
        using (client)
        {
            var response = await client.GetAsync("/api/v1/categories/statistics");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            sender.Verify(item => item.Send(It.IsAny<GetCategoryStatisticsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    private static RecipeDetailApiDto MakeRecipeDetail(RecipeStatus status)
    {
        return new RecipeDetailApiDto
        {
            Id = Guid.NewGuid(),
            Title = status.ToString(),
            Slug = status.ToString().ToLowerInvariant(),
            Status = status
        };
    }

    private static void SetUser(HttpClient client, string role, Guid userId)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Remove("X-Test-UserId");
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
    }

    private static async Task<(WebApplication App, HttpClient Client)> StartApi(
        Mock<ISender> sender,
        Mock<ICacheService> cache)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultForbidScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<ISender>(sender.Object);
        builder.Services.AddSingleton<ICacheService>(cache.Object);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCategoryEndpoints();
        app.MapRecipeEndpoints();
        await app.StartAsync();

        return (app, app.GetTestClient());
    }

    private sealed class HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Role, role.ToString())
            };
            if (Request.Headers.TryGetValue("X-Test-UserId", out var userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
