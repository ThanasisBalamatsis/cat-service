using Application.Abstractions;
using Application.Cats.GetAll;
using Domain.Cats;
using Domain.Tags;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Cats.GetAll;

public class GetAllCatsQueryHandlerTests
{
    private readonly GetAllCatsQueryHandler _sut;
    private readonly ICatRepository _catRepository = Substitute.For<ICatRepository>();

    public GetAllCatsQueryHandlerTests()
    {
        _sut = new GetAllCatsQueryHandler(_catRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnCatsResponse_WithPaginationInfo()
    {
        // Arrange
        var cats = new List<Cat>
        {
            new()
            {
                CatId = "abc123",
                Width = 800,
                Height = 600,
                ImageUrl = "https://cdn2.thecatapi.com/images/abc123.jpg",
                CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { new() { Name = "playful", CreatedAt = DateTime.UtcNow } }
            }
        };

        _catRepository
            .GetAllAsync(null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((cats.AsEnumerable(), 1));

        var query = new GetAllCatsQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Total.Should().Be(1);
        result.HasNextPage.Should().BeFalse();
        result.Cats.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldNormaliseTag_ToLowercase()
    {
        // Arrange
        _catRepository
            .GetAllAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<Cat>(), 0));

        var query = new GetAllCatsQuery { Page = 1, PageSize = 10, Tag = " Active " };

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _catRepository.Received(1).GetAllAsync(
            "active",
            1,
            10,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassNullTag_WhenTagIsNull()
    {
        // Arrange
        _catRepository
            .GetAllAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<Cat>(), 0));

        var query = new GetAllCatsQuery { Page = 1, PageSize = 10, Tag = null };

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _catRepository.Received(1).GetAllAsync(
            null,
            1,
            10,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCats_WhenNoCatsFound()
    {
        // Arrange
        _catRepository
            .GetAllAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<Cat>(), 0));

        var query = new GetAllCatsQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Cats.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldSetHasNextPage_WhenMorePagesExist()
    {
        // Arrange
        var cats = new List<Cat>
        {
            new()
            {
                CatId = "abc123",
                Width = 800,
                Height = 600,
                ImageUrl = "https://cdn2.thecatapi.com/images/abc123.jpg",
                CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag>()
            }
        };

        _catRepository
            .GetAllAsync(Arg.Any<string?>(), 1, 1, Arg.Any<CancellationToken>())
            .Returns((cats.AsEnumerable(), 5));

        var query = new GetAllCatsQuery { Page = 1, PageSize = 1 };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.HasNextPage.Should().BeTrue();
        result.Total.Should().Be(5);
    }
}
