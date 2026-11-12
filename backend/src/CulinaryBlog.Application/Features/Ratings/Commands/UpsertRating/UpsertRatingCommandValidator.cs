using FluentValidation;

namespace CulinaryBlog.Application.Features.Ratings.Commands.UpsertRating;

public class UpsertRatingCommandValidator : AbstractValidator<UpsertRatingCommand>
{
    public UpsertRatingCommandValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty().WithMessage("RecipeId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Score)
            .InclusiveBetween(1, 5).WithMessage("Score must be between 1 and 5.");
    }
}
