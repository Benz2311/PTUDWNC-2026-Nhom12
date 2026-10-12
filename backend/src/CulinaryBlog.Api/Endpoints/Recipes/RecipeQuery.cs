using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Api.Endpoints.Recipes;

public sealed class RecipeQuery
{
    public string? Search { get; init; }

    public Guid? CategoryId { get; init; }

    public RecipeStatus? Status { get; init; }

    public string? Sort { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
