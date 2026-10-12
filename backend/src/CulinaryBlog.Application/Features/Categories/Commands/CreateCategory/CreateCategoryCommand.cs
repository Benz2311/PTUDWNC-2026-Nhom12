using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description = null,
    string? ImageUrl = null,
    int OrderIndex = 0) : IRequest<CategoryDto?>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);
    }
}

public sealed class CreateCategoryCommandHandler
    : IRequestHandler<CreateCategoryCommand, CategoryDto?>
{
    private const string CategoryListCacheKey = "categories:all";

    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CulinaryBlog.Application.Common.Interfaces.ICacheService _cache;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        CulinaryBlog.Application.Common.Interfaces.ICacheService cache)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<CategoryDto?> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (await _categoryRepository.NameExistsAsync(
                request.Name.Trim(),
                cancellationToken: cancellationToken))
        {
            return null;
        }

        var baseSlug = SlugHelper.Generate(request.Name);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "category";
        }

        var slug = baseSlug;
        var suffix = 2;
        while (await _categoryRepository.SlugExistsAsync(slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        var category = Category.Create(
            request.Name.Trim(),
            slug,
            request.Description,
            request.ImageUrl,
            request.OrderIndex);
        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CategoryListCacheKey, cancellationToken);

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.OrderIndex,
            0);
    }
}