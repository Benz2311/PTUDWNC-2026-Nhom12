using CulinaryBlog.Application;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Categories.Commands.DeleteCategory;
using CulinaryBlog.Application.Features.Categories.Commands.UpdateCategory;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Features.Search.Dtos;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using ICacheService = CulinaryBlog.Application.Common.Interfaces.ICacheService;
using IAppRecipeRepository = CulinaryBlog.Application.Interfaces.IRecipeRepository;
using AppValidationException = CulinaryBlog.Application.Exceptions.ValidationException;

namespace CulinaryBlog.UnitTests.Application;

public class ApplicationMiscellaneousTests
{
    [Fact]
    public void DependencyInjection_AddApplication_RegistersRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => new Mock<IApplicationDbContext>().Object);
        services.AddScoped(_ => new Mock<ICacheService>().Object);
        services.AddScoped(_ => new Mock<IAppRecipeRepository>().Object);
        services.AddScoped(_ => new Mock<IUnitOfWork>().Object);

        services.AddApplication();
        var provider = services.BuildServiceProvider();

        // Verify MediatR and RecipeWriteService
        var writeService = provider.GetService<IRecipeWriteService>();
        writeService.Should().NotBeNull();
        writeService.Should().BeOfType<CulinaryBlog.Application.Features.Recipes.RecipeWriteService>();

        // Verify Validators
        var updateCategoryValidator = provider.GetService<IValidator<UpdateCategoryCommand>>();
        updateCategoryValidator.Should().NotBeNull();
    }

    [Fact]
    public void ApiResponse_And_PaginationMeta_ConstructAndSerializeCorrectly()
    {
        var meta = new PaginationMeta(
            Page: 2,
            PageSize: 10,
            Total: 35,
            TotalPages: 4,
            HasNextPage: true,
            HasPreviousPage: true
        );

        meta.Page.Should().Be(2);
        meta.PageSize.Should().Be(10);
        meta.Total.Should().Be(35);
        meta.TotalPages.Should().Be(4);
        meta.HasNextPage.Should().BeTrue();
        meta.HasPreviousPage.Should().BeTrue();

        var singleResp = ApiResponse.Success("payload_data");
        singleResp.Data.Should().Be("payload_data");
        singleResp.Meta.Should().BeNull();

        var pagedResp = ApiResponse.Paged(new List<string> { "item1", "item2" }, meta);
        pagedResp.Data.Should().HaveCount(2);
        pagedResp.Meta.Should().Be(meta);

        var pagedResult = new PagedResult<string>(new List<string> { "a", "b" }, 25, 1, 10);
        var fromPaged = ApiResponse.FromPaged(pagedResult);
        fromPaged.Data.Should().HaveCount(2);
        fromPaged.Meta!.Total.Should().Be(25);
        fromPaged.Meta.TotalPages.Should().Be(3);
        fromPaged.Meta.HasNextPage.Should().BeTrue();
        fromPaged.Meta.HasPreviousPage.Should().BeFalse();

        var pagedDto = new PagedResultDto<string>(new List<string> { "c" }, 1, 1, 10, 1);
        var fromDto = ApiResponse.FromPaged(pagedDto);
        fromDto.Data.Should().HaveCount(1);
        fromDto.Meta!.Total.Should().Be(1);
    }

    [Fact]
    public void ApplicationExceptions_CoverConstructorsAndProperties()
    {
        var notFound1 = new NotFoundException("Item missing");
        notFound1.Message.Should().Be("Item missing");

        var notFound2 = new NotFoundException("Recipe", Guid.Empty);
        notFound2.Message.Should().Contain("Recipe").And.Contain(Guid.Empty.ToString());

        var forbidden = new ForbiddenException("Access denied custom");
        forbidden.Message.Should().Be("Access denied custom");

        var defaultForbidden = new ForbiddenException();
        defaultForbidden.Message.Should().Contain("permission");

        var val1 = new AppValidationException("Invalid fields");
        val1.Errors.Should().BeEmpty();

        var errorsDict = new Dictionary<string, string[]> { ["Title"] = ["Required"] };
        var val2 = new AppValidationException(errorsDict);
        val2.Errors.Should().ContainKey("Title");

        var storageEx1 = new StorageException("Disk error");
        storageEx1.Message.Should().Be("Disk error");

        var storageEx2 = new StorageException("IO error", new InvalidOperationException());
        storageEx2.InnerException.Should().NotBeNull();

        var unavail1 = new StorageUnavailableException();
        unavail1.Message.Should().Contain("không khả dụng");

        var unavail2 = new StorageUnavailableException("Network timeout", new TimeoutException());
        unavail2.InnerException.Should().BeOfType<TimeoutException>();
    }

    [Fact]
    public async Task UpdateCategory_WhenCategoryNotFound_ReturnsNotFoundOutcome()
    {
        var categoryRepo = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new Mock<CulinaryBlog.Application.Common.Interfaces.ICacheService>();

        categoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var handler = new UpdateCategoryCommandHandler(categoryRepo.Object, unitOfWork.Object, cache.Object);
        var result = await handler.Handle(new UpdateCategoryCommand(Guid.NewGuid(), "New Name"), CancellationToken.None);

        result.Status.Should().Be(UpdateCategoryResult.NotFound);
        result.Category.Should().BeNull();
    }

    [Fact]
    public void UpdateCategoryCommandValidator_ValidatesNameCorrectly()
    {
        var validator = new UpdateCategoryCommandValidator();

        var valid = validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), "Món Tráng Miệng"));
        valid.IsValid.Should().BeTrue();

        var empty = validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), ""));
        empty.IsValid.Should().BeFalse();

        var shortName = validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), "A"));
        shortName.IsValid.Should().BeFalse();

        var longName = validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), new string('X', 105)));
        longName.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCategory_WhenCategoryNotFound_ReturnsNotFoundOutcome()
    {
        var categoryRepo = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new Mock<CulinaryBlog.Application.Common.Interfaces.ICacheService>();

        categoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var handler = new DeleteCategoryCommandHandler(categoryRepo.Object, unitOfWork.Object, cache.Object);
        var result = await handler.Handle(new DeleteCategoryCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().Be(DeleteCategoryResult.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_WhenHasPublishedRecipes_ReturnsConflictOutcome()
    {
        var category = Category.Create("Món Xào", "mon-xao");
        var categoryRepo = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new Mock<CulinaryBlog.Application.Common.Interfaces.ICacheService>();

        categoryRepo.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        categoryRepo.Setup(r => r.CountRecipesAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5); // 5 recipes exist!

        var handler = new DeleteCategoryCommandHandler(categoryRepo.Object, unitOfWork.Object, cache.Object);
        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        result.Should().Be(DeleteCategoryResult.HasRecipes);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategory_WhenNoPublishedRecipes_SoftDeletesAndInvalidatesCache()
    {
        var category = Category.Create("Món Rỗng", "mon-rong");
        var categoryRepo = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new Mock<CulinaryBlog.Application.Common.Interfaces.ICacheService>();

        categoryRepo.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        categoryRepo.Setup(r => r.CountRecipesAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new DeleteCategoryCommandHandler(categoryRepo.Object, unitOfWork.Object, cache.Object);
        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        result.Should().Be(DeleteCategoryResult.Deleted);
        category.IsDeleted.Should().BeTrue();
        categoryRepo.Verify(r => r.Update(category), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("categories:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DTOs_CoverPropertiesAndConstructors()
    {
        var authResp = new AuthResponse
        {
            AccessToken = "jwt.token",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            RefreshToken = "refresh.token",
            User = new UserResponse
            {
                Id = "user-1",
                Email = "user@test.com",
                UserName = "testuser",
                DisplayName = "Test User",
                Roles = ["Author"]
            }
        };
        authResp.AccessToken.Should().Be("jwt.token");
        authResp.RefreshToken.Should().Be("refresh.token");
        authResp.User.Roles.Should().Contain("Author");

        var chgPass = new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "new" };
        chgPass.CurrentPassword.Should().Be("old");

        var confEmail = new ConfirmEmailRequest { Token = "tok" };
        confEmail.Token.Should().Be("tok");

        var forgot = new ForgotPasswordRequest { Email = "forgot@b.com" };
        forgot.Email.Should().Be("forgot@b.com");

        var resend = new ResendConfirmationEmailRequest { Email = "resend@b.com" };
        resend.Email.Should().Be("resend@b.com");

        var reset = new ResetPasswordRequest { Token = "t", NewPassword = "p" };
        reset.Token.Should().Be("t");

        var roleReq = new UpdateAuthorRoleRequest("Author", true);
        roleReq.Role.Should().Be("Author");
        roleReq.Enabled.Should().BeTrue();

        var profileReq = new UpdateProfileRequest { DisplayName = "New", Bio = "Bio", AvatarUrl = "url" };
        profileReq.DisplayName.Should().Be("New");

        var statusReq = new UpdateUserStatusRequest { IsActive = false };
        statusReq.IsActive.Should().BeFalse();

        var catStatItem = new CategoryStatisticItemDto(Guid.NewGuid(), "Cat", 12, 10.5m);
        catStatItem.RecipeCount.Should().Be(12);

        var monthStat = new RecipeMonthlyStatisticDto(2026, 11, 42);
        monthStat.RecipeCount.Should().Be(42);

        var catStats = new CategoryStatisticsDto(10, 100, 80, 15, 5, [catStatItem], [catStatItem], [monthStat]);
        catStats.TotalRecipes.Should().Be(100);

        var detailDto = new CulinaryBlog.Application.Features.Recipes.Dtos.RecipeDetailDto
        {
            Id = Guid.NewGuid(),
            Title = "T",
            Slug = "s",
            Description = "D",
            Content = "C",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            Servings = 2,
            Difficulty = CulinaryBlog.Domain.Enums.DifficultyLevel.Easy,
            Author = new AuthorSummaryDto { DisplayName = "A" },
            Category = new CategorySummaryDto { Name = "C" },
            Nutrition = new CulinaryBlog.Application.Features.Recipes.Dtos.RecipeNutritionDto { Calories = 100 },
            Steps = [],
            Ingredients = [],
            Images = []
        };
        detailDto.Title.Should().Be("T");

        // RecipeReadDtos coverage
        var catDto = new RecipeCategoryDto(Guid.NewGuid(), "Món Việt", "mon-viet", "Mô tả");
        catDto.Name.Should().Be("Món Việt");

        var listDto = new RecipeListItemDto(
            Guid.NewGuid(), "Phở bò", "pho-bo", "Ngon", 15, 45, 4, "Medium", "https://img.local/p.jpg", catDto, DateTime.UtcNow);
        listDto.Title.Should().Be("Phở bò");

        var pagedRes = new PagedResultDto<RecipeListItemDto>([listDto], 1, 1, 10, 1);
        pagedRes.Total.Should().Be(1);
        pagedRes.HasNextPage.Should().BeFalse();
        pagedRes.HasPreviousPage.Should().BeFalse();
        var paginated = pagedRes.ToPaginatedResponse();
        paginated.Data.Items.Should().HaveCount(1);
        paginated.Meta.Total.Should().Be(1);

        var catRecipesResp = new CategoryRecipesResponseDto(catDto, pagedRes);
        catRecipesResp.Data.Category.Name.Should().Be("Món Việt");
        catRecipesResp.Meta.TotalPages.Should().Be(1);

        var trashDto = new RecipeTrashItemDto(Guid.NewGuid(), "Deleted Recipe", "del", "Draft", DateTime.UtcNow);
        trashDto.Title.Should().Be("Deleted Recipe");

        var ingDto = new CulinaryBlog.Application.DTOs.RecipeIngredientDto(Guid.NewGuid(), "Thịt bò", 500, "g", "Thái mỏng", 1);
        ingDto.Name.Should().Be("Thịt bò");

        var stepDto = new CulinaryBlog.Application.DTOs.RecipeStepDto(Guid.NewGuid(), 1, "Sơ chế", "Rửa sạch");
        stepDto.Title.Should().Be("Sơ chế");

        var imgDto = new CulinaryBlog.Application.DTOs.RecipeImageDto(Guid.NewGuid(), "https://img.local/1.jpg", "Alt", true, 0);
        imgDto.IsPrimary.Should().BeTrue();

        var nutDto = new CulinaryBlog.Application.DTOs.RecipeNutritionDto(500, 30, 40, 20, 5, 400);
        nutDto.Carbs.Should().Be(40);
        nutDto.Calories.Should().Be(500);

        var authDto = new RecipeAuthorDto(Guid.NewGuid(), "Bếp Trưởng", "https://avatar.local/chef.png");
        authDto.DisplayName.Should().Be("Bếp Trưởng");

        var readDetailDto = new CulinaryBlog.Application.DTOs.RecipeDetailDto(
            Guid.NewGuid(), "Bún chả", "bun-cha", "Mô tả", "Nội dung", 20, 30, 2, "Easy", DateTime.UtcNow,
            catDto, [ingDto], [stepDto], [imgDto], nutDto, authDto);
        readDetailDto.Title.Should().Be("Bún chả");
        readDetailDto.Author!.DisplayName.Should().Be("Bếp Trưởng");

        // Search & Email & UpdateCategory coverage
        var searchDto = new RecipeSearchResultDto
        {
            Id = Guid.NewGuid(),
            Title = "Search Result",
            Slug = "search-result",
            Description = "Desc",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 15,
            Servings = 2,
            Difficulty = CulinaryBlog.Domain.Enums.DifficultyLevel.Easy,
            PublishedAt = DateTime.UtcNow,
            PrimaryImageUrl = "https://img.local/s.jpg",
            AuthorName = "Chef",
            CategoryName = "Món Việt",
            CategorySlug = "mon-viet",
            MatchType = "FullTextSearch"
        };
        searchDto.Title.Should().Be("Search Result");
        searchDto.MatchType.Should().Be("FullTextSearch");

        var emailMsg = new EmailMessage("test@example.com", "Test Subject", "<p>Hello</p>", "Hello");
        emailMsg.To.Should().Be("test@example.com");
        emailMsg.PlainTextBody.Should().Be("Hello");

        var googleReq = new GoogleLoginRequest { IdToken = "google-id-token" };
        googleReq.IdToken.Should().Be("google-id-token");
    }
}
