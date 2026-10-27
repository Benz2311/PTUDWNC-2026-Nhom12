using CulinaryBlog.Application.Features.Recipes.Dtos;
using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.Validators;

public class RecipeSearchQueryDtoValidator : AbstractValidator<RecipeSearchQueryDto>
{
    public RecipeSearchQueryDtoValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");

        RuleFor(x => x.Search)
            .MaximumLength(200).WithMessage("Search term must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField).WithMessage("Invalid sort field.");

        RuleFor(x => x.MaxTotalTimeMinutes)
            .GreaterThan(0).WithMessage("Max total time must be greater than 0.")
            .When(x => x.MaxTotalTimeMinutes.HasValue);

        RuleFor(x => x.MaxCookTimeMinutes)
            .GreaterThan(0).WithMessage("Max cook time must be greater than 0.")
            .When(x => x.MaxCookTimeMinutes.HasValue);
    }

    private static bool BeValidSortField(string sortBy)
    {
        var validFields = new[] { "PublishedAt", "Title", "PrepTime", "CookTime", "TotalTime" };
        return validFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }
}