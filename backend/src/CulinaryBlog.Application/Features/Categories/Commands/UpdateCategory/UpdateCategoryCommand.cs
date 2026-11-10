using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryRequest(
    string Name,
    string? Description = null,
    string? ImageUrl = null,
    int? OrderIndex = null);

public enum UpdateCategoryResult
{
    Updated,
    NotFound,
    NameAlreadyExists
}

public sealed record UpdateCategoryOutcome(UpdateCategoryResult Status, CategoryDto? Category = null);

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description = null,
    string? ImageUrl = null,
    int? OrderIndex = null) : IRequest<UpdateCategoryOutcome>;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);
    }
}

public sealed class UpdateCategoryCommandHandler
    : IRequestHandler<UpdateCategoryCommand, UpdateCategoryOutcome>
{
    private const string CategoryListCacheKey = "categories:all";

    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CulinaryBlog.Application.Common.Interfaces.ICacheService _cache;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        CulinaryBlog.Application.Common.Interfaces.ICacheService cache)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<UpdateCategoryOutcome> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
        if (category is null)
        {
            return new UpdateCategoryOutcome(UpdateCategoryResult.NotFound);
        }

        var name = request.Name.Trim();
        if (await _categoryRepository.NameExistsAsync(
                name,
                request.Id,
                cancellationToken))
        {
            return new UpdateCategoryOutcome(UpdateCategoryResult.NameAlreadyExists);
        }

        category.Update(name, request.Description, request.ImageUrl, request.OrderIndex);
        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CategoryListCacheKey, cancellationToken);
        await _cache.RemoveAsync("sitemap:xml", cancellationToken);

        var count = await _categoryRepository.CountPublishedRecipesAsync(category.Id, cancellationToken);
        var dto = new CategoryDto(category.Id, category.Name, category.Slug, category.Description,
            category.ImageUrl, category.OrderIndex, count);
        return new UpdateCategoryOutcome(UpdateCategoryResult.Updated, dto);
    }
}
