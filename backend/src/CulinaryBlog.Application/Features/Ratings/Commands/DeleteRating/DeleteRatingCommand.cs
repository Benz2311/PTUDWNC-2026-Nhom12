using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Ratings.Dtos;
using CulinaryBlog.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Ratings.Commands.DeleteRating;

public record DeleteRatingCommand(
    Guid RecipeId,
    Guid UserId,
    bool IsAdmin = false
) : IRequest<RecipeRatingSummaryDto>;

public class DeleteRatingCommandHandler : IRequestHandler<DeleteRatingCommand, RecipeRatingSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public DeleteRatingCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RecipeRatingSummaryDto> Handle(DeleteRatingCommand request, CancellationToken cancellationToken)
    {
        var rating = await _context.RecipeRatings
            .FirstOrDefaultAsync(r => r.RecipeId == request.RecipeId && r.UserId == request.UserId && !r.IsDeleted, cancellationToken);

        if (rating == null)
        {
            throw new NotFoundException($"Rating for recipe '{request.RecipeId}' by user '{request.UserId}' was not found.");
        }

        if (rating.UserId != request.UserId && !request.IsAdmin)
        {
            throw new ForbiddenException("You can only delete your own rating.");
        }

        _context.RecipeRatings.Remove(rating);
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
            null);
    }
}
