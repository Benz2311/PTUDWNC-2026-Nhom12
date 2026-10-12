namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>A numbered preparation step for a recipe.</summary>
public sealed record StepResponse(Guid Id, int StepNumber, string Title, string Description);
