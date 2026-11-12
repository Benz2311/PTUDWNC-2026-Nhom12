using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Ratings.Dtos;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Ratings.Commands.UpsertRating;

public record UpsertRatingCommand(
    Guid RecipeId,
    Guid UserId,
    int Score
) : IRequest<RecipeRatingSummaryDto>;

public class UpsertRatingCommandHandler : IRequestHandler<UpsertRatingCommand, RecipeRatingSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public UpsertRatingCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RecipeRatingSummaryDto> Handle(UpsertRatingCommand request, CancellationToken cancellationToken)
    {
        if (request.Score < 1 || request.Score > 5)
        {
            throw new ValidationException("Score must be between 1 and 5.");
        }

        var recipeExists = await _context.Recipes
            .AnyAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (!recipeExists)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        var existingRating = await _context.RecipeRatings
            .FirstOrDefaultAsync(r => r.RecipeId == request.RecipeId && r.UserId == request.UserId, cancellationToken);

        if (existingRating != null)
        {
            existingRating.Score = request.Score;
            existingRating.IsDeleted = false;
            existingRating.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var rating = new RecipeRating(request.RecipeId, request.UserId, request.Score);
            await _context.RecipeRatings.AddAsync(rating, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var activeRatings = await _context.RecipeRatings
            .AsNoTracking()
            .Where(r => r.RecipeId == request.RecipeId && !r.IsDeleted)
            .Select(r => r.Score)
            .ToListAsync(cancellationToken);

        var ratingCount = activeRatings.Count;
        var averageRating = ratingCount > 0
            ? Math.Round(activeRatings.Average(), 1)
            : 0;

        return new RecipeRatingSummaryDto(
            request.RecipeId,
            averageRating,
            ratingCount,
            request.Score);
    }
}
