namespace CulinaryBlog.Api.Endpoints.Recipes;

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
