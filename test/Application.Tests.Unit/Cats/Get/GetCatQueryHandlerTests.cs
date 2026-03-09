using Application.Abstractions;
using Application.Cats.Get;
using Domain.Cats;
using Domain.Tags;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Application.Tests.Unit.Cats.Get;

public class GetCatQueryHandlerTests
{
    private readonly GetCatQueryHandler _sut;
    private readonly ICatRepository _catRepository = Substitute.For<ICatRepository>();

    public GetCatQueryHandlerTests()
    {
        _sut = new GetCatQueryHandler(_catRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnCatResponse_WhenCatExists()
    {
        // Arrange
        var cat = new Cat
        {
            CatId = "abc123",
            Width = 800,
            Height = 600,
            ImageUrl = "https://cdn2.thecatapi.com/images/abc123.jpg",
            CreatedAt = DateTime.UtcNow,
            Tags = new List<Tag>
            {
                new() { Name = "playful", CreatedAt = DateTime.UtcNow },
                new() { Name = "active", CreatedAt = DateTime.UtcNow }
            }
        };

        _catRepository
            .GetAsync(1, Arg.Any<CancellationToken>())
            .Returns(cat);

        var query = new GetCatQuery { Id = 1 };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.CatId.Should().Be("abc123");
        result.Width.Should().Be(800);
        result.Height.Should().Be(600);
        result.ImageUrl.Should().Be("https://cdn2.thecatapi.com/images/abc123.jpg");
        result.Tags.Should().BeEquivalentTo(new[] { "playful", "active" });
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenCatDoesNotExist()
    {
        // Arrange
        _catRepository
            .GetAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var query = new GetCatQuery { Id = 999 };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnCatResponse_WithEmptyTags_WhenCatHasNoTags()
    {
        // Arrange
        var cat = new Cat
        {
            CatId = "xyz789",
            Width = 1024,
            Height = 768,
            ImageUrl = "https://cdn2.thecatapi.com/images/xyz789.jpg",
            CreatedAt = DateTime.UtcNow,
            Tags = new List<Tag>()
        };

        _catRepository
            .GetAsync(2, Arg.Any<CancellationToken>())
            .Returns(cat);

        var query = new GetCatQuery { Id = 2 };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        _catRepository
            .GetAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var query = new GetCatQuery { Id = 42 };

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _catRepository.Received(1).GetAsync(42, Arg.Any<CancellationToken>());
    }
}
