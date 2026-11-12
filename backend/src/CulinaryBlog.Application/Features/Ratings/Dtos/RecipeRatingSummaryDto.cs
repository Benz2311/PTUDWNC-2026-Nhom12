namespace CulinaryBlog.Application.Features.Ratings.Dtos;

public class RecipeRatingSummaryDto
{
    public Guid RecipeId { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int? UserRating { get; set; }

    public RecipeRatingSummaryDto()
    {
    }

    public RecipeRatingSummaryDto(Guid recipeId, double averageRating, int ratingCount, int? userRating = null)
    {
        RecipeId = recipeId;
        AverageRating = averageRating;
        RatingCount = ratingCount;
        UserRating = userRating;
    }
}
