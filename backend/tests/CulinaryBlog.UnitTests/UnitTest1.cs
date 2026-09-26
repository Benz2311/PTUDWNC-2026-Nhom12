using CulinaryBlog.Api.Endpoints.Recipes;

namespace CulinaryBlog.UnitTests;

public class SlugTests
{
    [Fact]
    public void Slugify_Should_Remove_Diacritics_And_Keep_Ascii()
    {
        var slug = RecipeEndpoints.SlugifyForTests("Bún chả Hà Nội");

        Assert.Equal("bun-cha-ha-noi", slug);
    }
}
