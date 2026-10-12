namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>Identifies the image selected as a recipe's primary image.</summary>
public sealed record PrimaryImageResponse(Guid Id, bool IsPrimary);
