using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.Interfaces;
using MediatR;

namespace CulinaryBlog.Application.Features.Categories.Commands.DeleteCategory;

public enum DeleteCategoryResult
{
    Deleted,
    NotFound,
    HasRecipes
}

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<DeleteCategoryResult>;

public sealed class DeleteCategoryCommandHandler
    : IRequestHandler<DeleteCategoryCommand, DeleteCategoryResult>
{
    private const string CategoryListCacheKey = "categories:all";

    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CulinaryBlog.Application.Common.Interfaces.ICacheService _cache;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        CulinaryBlog.Application.Common.Interfaces.ICacheService cache)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<DeleteCategoryResult> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
        if (category is null)
        {
            return DeleteCategoryResult.NotFound;
        }

        if (await _categoryRepository.CountRecipesAsync(
                category.Id,
                cancellationToken) > 0)
        {
            return DeleteCategoryResult.HasRecipes;
        }

        category.SoftDelete();
        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CategoryListCacheKey, cancellationToken);

        return DeleteCategoryResult.Deleted;
    }
}