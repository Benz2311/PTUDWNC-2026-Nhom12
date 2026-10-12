namespace CulinaryBlog.Api.Endpoints.Recipes;

public sealed record CachedRecipeDetail(RecipeDetailResponse Detail, string ETag);
