using Application.Cats.Fetch;
using Application.Cats.Get;
using Application.Cats.GetAll;
using Application.Cats.Requests;
using Application.Cats.Responses;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Presentation.Controllers;

namespace Api.Tests.Unit.Controllers;

public class CatsControllerTests
{
    private readonly CatsController _sut;
    private readonly ISender _sender = Substitute.For<ISender>();

    public CatsControllerTests()
    {
        _sut = new CatsController(_sender);
    }

    [Fact]
    public async Task Fetch_ShouldReturnAcceptedResult_WithJobDetails()
    {
        // Arrange
        var response = new FetchCatsResponse
        {
            JobId = 1,
            Status = "Pending"
        };

        _sender
            .Send(Arg.Any<FetchCatsCommand>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = (AcceptedAtActionResult)await _sut.Fetch(CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(202);
        result.ActionName.Should().Be(nameof(JobsController.Get));
        result.ControllerName.Should().Be("Jobs");
        result.RouteValues!["id"].Should().Be(response.JobId);
        result.Value.As<FetchCatsResponse>().Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenCatExists()
    {
        // Arrange
        var response = new CatResponse
        {
            Id = 1,
            CreatedAt = DateTime.UtcNow,
            CatId = "abc123",
            Width = 800,
            Height = 600,
            ImageUrl = "https://cdn2.thecatapi.com/images/abc123.jpg",
            Tags = new List<string> { "playful", "active" }
        };

        _sender
            .Send(Arg.Any<GetCatQuery>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = (OkObjectResult)await _sut.Get(1, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Value.As<CatResponse>().Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenCatDoesNotExist()
    {
        // Arrange
        _sender
            .Send(Arg.Any<GetCatQuery>(), Arg.Any<CancellationToken>())
            .Returns((CatResponse?)null);

        // Act
        var result = (NotFoundResult)await _sut.Get(999, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithCatsResponse()
    {
        // Arrange
        var response = new CatsResponse
        {
            Cats = new List<CatResponse>
            {
                new()
                {
                    Id = 1,
                    CreatedAt = DateTime.UtcNow,
                    CatId = "abc123",
                    Width = 800,
                    Height = 600,
                    ImageUrl = "https://cdn2.thecatapi.com/images/abc123.jpg",
                    Tags = new List<string> { "playful" }
                }
            },
            Page = 1,
            PageSize = 10,
            Total = 1
        };

        _sender
            .Send(Arg.Any<GetAllCatsQuery>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var request = new GetAllCatsRequest
        {
            Page = 1,
            PageSize = 10,
            Tag = "playful"
        };

        // Act
        var result = (OkObjectResult)await _sut.GetAll(request, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Value.As<CatsResponse>().Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WhenNoCatsFound()
    {
        // Arrange
        var response = new CatsResponse
        {
            Cats = Enumerable.Empty<CatResponse>(),
            Page = 1,
            PageSize = 10,
            Total = 0
        };

        _sender
            .Send(Arg.Any<GetAllCatsQuery>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var request = new GetAllCatsRequest
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = (OkObjectResult)await _sut.GetAll(request, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Value.As<CatsResponse>().Total.Should().Be(0);
    }

    [Fact]
    public async Task Fetch_ShouldSendFetchCatsCommand()
    {
        // Arrange
        var response = new FetchCatsResponse { JobId = 1, Status = "Pending" };
        _sender
            .Send(Arg.Any<FetchCatsCommand>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        await _sut.Fetch(CancellationToken.None);

        // Assert
        await _sender.Received(1).Send(Arg.Any<FetchCatsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_ShouldPassCorrectQueryParameters()
    {
        // Arrange
        var response = new CatsResponse
        {
            Cats = Enumerable.Empty<CatResponse>(),
            Page = 2,
            PageSize = 5,
            Total = 0
        };

        _sender
            .Send(Arg.Any<GetAllCatsQuery>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var request = new GetAllCatsRequest
        {
            Page = 2,
            PageSize = 5,
            Tag = "active"
        };

        // Act
        await _sut.GetAll(request, CancellationToken.None);

        // Assert
        await _sender.Received(1).Send(
            Arg.Is<GetAllCatsQuery>(q =>
                q.Page == 2 &&
                q.PageSize == 5 &&
                q.Tag == "active"),
            Arg.Any<CancellationToken>());
    }
}
