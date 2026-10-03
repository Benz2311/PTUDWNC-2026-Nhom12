using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryRequest(
    string Name,
    string? Description);

public enum UpdateCategoryResult
{
    Updated,
    NotFound,
    NameAlreadyExists
}

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description) : IRequest<UpdateCategoryResult>;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public sealed class UpdateCategoryCommandHandler
    : IRequestHandler<UpdateCategoryCommand, UpdateCategoryResult>
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

    public async Task<UpdateCategoryResult> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
        if (category is null)
        {
            return UpdateCategoryResult.NotFound;
        }

        var name = request.Name.Trim();
        if (await _categoryRepository.NameExistsAsync(
                name,
                request.Id,
                cancellationToken))
        {
            return UpdateCategoryResult.NameAlreadyExists;
        }

        category.UpdateDetails(name, request.Description);
        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CategoryListCacheKey, cancellationToken);

        return UpdateCategoryResult.Updated;
    }
}