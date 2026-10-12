namespace CulinaryBlog.Api.Endpoints.Recipes;

public sealed class RecipeSearchQuery
{
    public string? Query { get; init; }

    public Guid? CategoryId { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
