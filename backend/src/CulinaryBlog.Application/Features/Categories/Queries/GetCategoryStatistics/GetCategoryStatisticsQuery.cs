using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Categories.Queries
    .GetCategoryStatistics;

public record GetCategoryStatisticsQuery
    : IRequest<CategoryStatisticsDto>;