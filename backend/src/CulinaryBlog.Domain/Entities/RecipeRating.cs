using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Entities;

public class RecipeRating : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int Score { get; set; }

    public RecipeRating()
    {
    }

    public RecipeRating(Guid recipeId, Guid userId, int score)
    {
        RecipeId = recipeId;
        UserId = userId;
        Score = score;
    }
}
