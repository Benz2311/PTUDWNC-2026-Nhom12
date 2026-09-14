using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Categories.Queries
    .GetCategoryStatistics;

public class GetCategoryStatisticsQueryHandler
    : IRequestHandler<
        GetCategoryStatisticsQuery,
        CategoryStatisticsDto>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryStatisticsQueryHandler(
        ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryStatisticsDto> Handle(
        GetCategoryStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        return await _categoryRepository.GetCategoryStatisticsAsync(cancellationToken);
    }
}