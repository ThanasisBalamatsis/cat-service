using Domain.Cats;
using Domain.Tags;
using FluentAssertions;

namespace Domain.Tests.Unit.Cats;

public class CatTests
{
    [Fact]
    public void Cat_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var cat = new Cat
        {
            CatId = "abc123",
            Width = 800,
            Height = 600,
            ImageUrl = "https://cdn2.thecatapi.com/images/abc123.jpg",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        cat.CatId.Should().Be("abc123");
        cat.Width.Should().Be(800);
        cat.Height.Should().Be(600);
        cat.ImageUrl.Should().Be("https://cdn2.thecatapi.com/images/abc123.jpg");
        cat.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Cat_Tags_ShouldDefaultToEmptyList()
    {
        // Arrange & Act
        var cat = new Cat
        {
            CatId = "abc123",
            Width = 800,
            Height = 600,
            ImageUrl = "https://cdn2.thecatapi.com/images/abc123.jpg",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        cat.Tags.Should().NotBeNull();
        cat.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Cat_ShouldAllowTagAssignment()
    {
        // Arrange
        var tags = new List<Tag>
        {
            new() { Name = "playful", CreatedAt = DateTime.UtcNow },
            new() { Name = "active", CreatedAt = DateTime.UtcNow }
        };

        // Act
        var cat = new Cat
        {
            CatId = "abc123",
            Width = 800,
            Height = 600,
            ImageUrl = "https://cdn2.thecatapi.com/images/abc123.jpg",
            CreatedAt = DateTime.UtcNow,
            Tags = tags
        };

        // Assert
        cat.Tags.Should().HaveCount(2);
        cat.Tags.Should().Contain(t => t.Name == "playful");
        cat.Tags.Should().Contain(t => t.Name == "active");
    }
}
