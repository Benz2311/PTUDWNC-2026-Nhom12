using CulinaryBlog.Application.Features.Recipes.Dtos;
using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.Validators;

public class CreateRecipeIngredientDtoValidator : AbstractValidator<CreateRecipeIngredientDto>
{
    public CreateRecipeIngredientDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ingredient name is required.")
            .MaximumLength(200).WithMessage("Ingredient name must not exceed 200 characters.");

        RuleFor(x => x.Unit)
            .MaximumLength(50).WithMessage("Unit must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Unit));

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be greater than or equal to 0.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must be greater than or equal to 0.")
            .When(x => x.Quantity.HasValue);
    }
}

public class CreateRecipeStepDtoValidator : AbstractValidator<CreateRecipeStepDto>
{
    public CreateRecipeStepDtoValidator()
    {
        RuleFor(x => x.StepNumber)
            .GreaterThan(0).WithMessage("Step number must be greater than 0.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Step title is required.")
            .MaximumLength(200).WithMessage("Step title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Step description is required.")
            .MaximumLength(5000).WithMessage("Step description must not exceed 5000 characters.");

        RuleFor(x => x.TimerMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Timer minutes must be greater than or equal to 0.")
            .When(x => x.TimerMinutes.HasValue);

        RuleFor(x => x.ImageUrl)
            .Must(BeValidUrl).WithMessage("Image URL must be a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}

public class CreateRecipeNutritionDtoValidator : AbstractValidator<CreateRecipeNutritionDto>
{
    public CreateRecipeNutritionDtoValidator()
    {
        RuleFor(x => x.Calories)
            .GreaterThanOrEqualTo(0).WithMessage("Calories must be greater than or equal to 0.")
            .When(x => x.Calories.HasValue);

        RuleFor(x => x.Protein)
            .GreaterThanOrEqualTo(0).WithMessage("Protein must be greater than or equal to 0.")
            .When(x => x.Protein.HasValue);

        RuleFor(x => x.Carbohydrates)
            .GreaterThanOrEqualTo(0).WithMessage("Carbohydrates must be greater than or equal to 0.")
            .When(x => x.Carbohydrates.HasValue);

        RuleFor(x => x.Fat)
            .GreaterThanOrEqualTo(0).WithMessage("Fat must be greater than or equal to 0.")
            .When(x => x.Fat.HasValue);

        RuleFor(x => x.Fiber)
            .GreaterThanOrEqualTo(0).WithMessage("Fiber must be greater than or equal to 0.")
            .When(x => x.Fiber.HasValue);

        RuleFor(x => x.Sodium)
            .GreaterThanOrEqualTo(0).WithMessage("Sodium must be greater than or equal to 0.")
            .When(x => x.Sodium.HasValue);
    }
}

public class CreateRecipeDtoValidator : AbstractValidator<CreateRecipeDto>
{
    public CreateRecipeDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Recipe title is required.")
            .MaximumLength(200).WithMessage("Recipe title must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .MaximumLength(220).WithMessage("Slug must not exceed 220 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Content)
            .MaximumLength(50000).WithMessage("Content must not exceed 50000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Content));

        RuleFor(x => x.PrepTimeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Prep time must be greater than or equal to 0.");

        RuleFor(x => x.CookTimeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Cook time must be greater than or equal to 0.");

        RuleFor(x => x.Servings)
            .GreaterThan(0).WithMessage("Servings must be greater than 0.");

        RuleFor(x => x.Difficulty)
            .NotEmpty().WithMessage("Difficulty is required.")
            .Must(BeValidDifficulty).WithMessage("Invalid difficulty level.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Ingredients)
            .NotNull().WithMessage("Ingredients list is required.")
            .Must(x => x.Count > 0).WithMessage("At least one ingredient is required.");

        RuleForEach(x => x.Ingredients)
            .SetValidator(new CreateRecipeIngredientDtoValidator());

        RuleFor(x => x.Steps)
            .NotNull().WithMessage("Steps list is required.")
            .Must(x => x.Count > 0).WithMessage("At least one step is required.");

        RuleForEach(x => x.Steps)
            .SetValidator(new CreateRecipeStepDtoValidator());

        RuleFor(x => x.Nutrition)
            .SetValidator(new CreateRecipeNutritionDtoValidator())
            .When(x => x.Nutrition != null);
    }

    private static bool BeValidDifficulty(string difficulty)
    {
        return Enum.TryParse<CulinaryBlog.Domain.Enums.DifficultyLevel>(difficulty, true, out _);
    }
}

public class UpdateRecipeDtoValidator : AbstractValidator<UpdateRecipeDto>
{
    public UpdateRecipeDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Recipe title is required.")
            .MaximumLength(200).WithMessage("Recipe title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Content)
            .MaximumLength(50000).WithMessage("Content must not exceed 50000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Content));

        RuleFor(x => x.PrepTimeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Prep time must be greater than or equal to 0.");

        RuleFor(x => x.CookTimeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Cook time must be greater than or equal to 0.");

        RuleFor(x => x.Servings)
            .GreaterThan(0).WithMessage("Servings must be greater than 0.");

        RuleFor(x => x.Difficulty)
            .NotEmpty().WithMessage("Difficulty is required.")
            .Must(BeValidDifficulty).WithMessage("Invalid difficulty level.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Ingredients)
            .NotNull().WithMessage("Ingredients list is required.")
            .Must(x => x.Count > 0).WithMessage("At least one ingredient is required.");

        RuleForEach(x => x.Ingredients)
            .SetValidator(new CreateRecipeIngredientDtoValidator());

        RuleFor(x => x.Steps)
            .NotNull().WithMessage("Steps list is required.")
            .Must(x => x.Count > 0).WithMessage("At least one step is required.");

        RuleForEach(x => x.Steps)
            .SetValidator(new CreateRecipeStepDtoValidator());

        RuleFor(x => x.Nutrition)
            .SetValidator(new CreateRecipeNutritionDtoValidator())
            .When(x => x.Nutrition != null);
    }

    private static bool BeValidDifficulty(string difficulty)
    {
        return Enum.TryParse<CulinaryBlog.Domain.Enums.DifficultyLevel>(difficulty, true, out _);
    }
}