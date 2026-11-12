using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Ratings.Dtos;
using CulinaryBlog.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Ratings.Queries.GetRecipeRatingSummary;

public record GetRecipeRatingSummaryQuery(
    Guid RecipeId,
    Guid? UserId = null
) : IRequest<RecipeRatingSummaryDto>;

public class GetRecipeRatingSummaryQueryHandler : IRequestHandler<GetRecipeRatingSummaryQuery, RecipeRatingSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetRecipeRatingSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RecipeRatingSummaryDto> Handle(GetRecipeRatingSummaryQuery request, CancellationToken cancellationToken)
    {
        var recipeExists = await _context.Recipes
            .AnyAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (!recipeExists)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        var activeRatings = await _context.RecipeRatings
            .AsNoTracking()
            .Where(r => r.RecipeId == request.RecipeId && !r.IsDeleted)
            .Select(r => new { r.UserId, r.Score })
            .ToListAsync(cancellationToken);

        var ratingCount = activeRatings.Count;
        var averageRating = ratingCount > 0
            ? Math.Round(activeRatings.Average(r => r.Score), 1)
            : 0;

        int? userRating = null;
        if (request.UserId.HasValue)
        {
            userRating = activeRatings
                .Where(r => r.UserId == request.UserId.Value)
                .Select(r => (int?)r.Score)
                .FirstOrDefault();
        }

        return new RecipeRatingSummaryDto(
            request.RecipeId,
            averageRating,
            ratingCount,
            userRating);
    }
}
