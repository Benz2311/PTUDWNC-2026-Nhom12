using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.UnitTests.Domain;

/// <summary>
/// Unit tests for the PersonalRecipeNote entity.
/// </summary>
public sealed class PersonalRecipeNoteTests
{
    [Fact]
    public void PersonalRecipeNote_DefaultId_IsNotEmpty()
    {
        var note = new PersonalRecipeNote();

        Assert.NotEqual(Guid.Empty, note.Id);
    }

    [Fact]
    public void PersonalRecipeNote_TwoInstances_HaveDifferentIds()
    {
        var note1 = new PersonalRecipeNote();
        var note2 = new PersonalRecipeNote();

        Assert.NotEqual(note1.Id, note2.Id);
    }

    [Fact]
    public void PersonalRecipeNote_DefaultCreatedAt_IsRecentUtc()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-2);
        var note = new PersonalRecipeNote();
        var after = DateTimeOffset.UtcNow.AddSeconds(2);

        Assert.True(note.CreatedAt >= before, $"CreatedAt {note.CreatedAt} should be >= {before}");
        Assert.True(note.CreatedAt <= after, $"CreatedAt {note.CreatedAt} should be <= {after}");
    }

    [Fact]
    public void PersonalRecipeNote_DefaultUpdatedAt_IsNull()
    {
        var note = new PersonalRecipeNote();

        Assert.Null(note.UpdatedAt);
    }

    [Fact]
    public void PersonalRecipeNote_DefaultContent_IsEmpty()
    {
        var note = new PersonalRecipeNote();

        Assert.Equal(string.Empty, note.Content);
    }

    [Fact]
    public void PersonalRecipeNote_CanSetContent()
    {
        const string expectedContent = "Nên dùng nước xương bò tươi, hầm ít nhất 6 tiếng.";

        var note = new PersonalRecipeNote { Content = expectedContent };

        Assert.Equal(expectedContent, note.Content);
    }

    [Fact]
    public void PersonalRecipeNote_LinksRecipeAndUser_Correctly()
    {
        var recipeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var note = new PersonalRecipeNote
        {
            RecipeId = recipeId,
            UserId = userId,
            Content = "Ghi chú thử nghiệm",
        };

        Assert.Equal(recipeId, note.RecipeId);
        Assert.Equal(userId, note.UserId);
    }

    [Fact]
    public void PersonalRecipeNote_CanUpdateContent()
    {
        var note = new PersonalRecipeNote { Content = "Nội dung cũ" };

        note.Content = "Nội dung đã cập nhật";
        note.UpdatedAt = DateTimeOffset.UtcNow;

        Assert.Equal("Nội dung đã cập nhật", note.Content);
        Assert.NotNull(note.UpdatedAt);
    }

    [Fact]
    public void PersonalRecipeNote_UpdatedAt_IsSetAfterEdit()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var note = new PersonalRecipeNote { Content = "Nội dung ban đầu" };

        note.Content = "Nội dung mới";
        note.UpdatedAt = DateTimeOffset.UtcNow;

        Assert.True(note.UpdatedAt >= before);
    }

    [Fact]
    public void PersonalRecipeNote_SameRecipe_DifferentUsers_HaveDifferentIds()
    {
        var recipeId = Guid.NewGuid();

        var noteA = new PersonalRecipeNote { RecipeId = recipeId, UserId = Guid.NewGuid() };
        var noteB = new PersonalRecipeNote { RecipeId = recipeId, UserId = Guid.NewGuid() };

        Assert.NotEqual(noteA.Id, noteB.Id);
        Assert.Equal(noteA.RecipeId, noteB.RecipeId);
        Assert.NotEqual(noteA.UserId, noteB.UserId);
    }

    [Fact]
    public void PersonalRecipeNote_ContentCanBeVeryLong()
    {
        // Max content is 5000 chars per EF config
        var longContent = new string('A', 5000);
        var note = new PersonalRecipeNote { Content = longContent };

        Assert.Equal(5000, note.Content.Length);
    }

    [Fact]
    public void PersonalRecipeNote_RecipeIdAndUserId_DefaultToEmpty()
    {
        var note = new PersonalRecipeNote();

        // Guid.Empty is the default for Guid
        Assert.Equal(Guid.Empty, note.RecipeId);
        Assert.Equal(Guid.Empty, note.UserId);
    }

    [Fact]
    public void PersonalRecipeNote_MultilineContent_IsPreserved()
    {
        const string multiline = "Dòng 1\nDòng 2\nDòng 3";
        var note = new PersonalRecipeNote { Content = multiline };

        Assert.Equal(multiline, note.Content);
    }
}

