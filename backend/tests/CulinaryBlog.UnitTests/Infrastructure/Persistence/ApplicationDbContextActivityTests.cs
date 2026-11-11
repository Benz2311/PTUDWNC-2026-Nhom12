using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.UnitTests.Infrastructure.Persistence;

public sealed class ApplicationDbContextActivityTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenRatingAdded_RecordsActivityWithoutMutatingTrackedEnumeration()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"rating-activity-{Guid.NewGuid():N}")
            .Options;

        await using var db = new ApplicationDbContext(options);
        db.RecipeRatings.Add(new RecipeRating
        {
            RecipeId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Score = 5
        });

        await db.SaveChangesAsync();

        var activity = await db.ActivityLogs.SingleAsync();
        Assert.Equal(ActivityType.RatedRecipe, activity.ActivityType);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenCommentPending_DoesNotRecordPublicActivity()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"pending-comment-activity-{Guid.NewGuid():N}")
            .Options;

        await using var db = new ApplicationDbContext(options);
        db.RecipeComments.Add(new RecipeComment
        {
            RecipeId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = "Comment awaiting moderation",
            Status = ModerationStatus.Pending
        });

        await db.SaveChangesAsync();

        Assert.Empty(await db.ActivityLogs.ToListAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenCommentApproved_RecordsActivityWithoutMutatingTrackedEnumeration()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"approved-comment-activity-{Guid.NewGuid():N}")
            .Options;

        await using var db = new ApplicationDbContext(options);
        db.RecipeComments.Add(new RecipeComment
        {
            RecipeId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = "Approved comment",
            Status = ModerationStatus.Approved
        });

        await db.SaveChangesAsync();

        var activity = await db.ActivityLogs.SingleAsync();
        Assert.Equal(ActivityType.CommentedRecipe, activity.ActivityType);
    }
}
