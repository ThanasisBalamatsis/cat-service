using Application.Jobs.Get;
using Application.Jobs.Responses;
using Domain.Jobs;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Presentation.Controllers;

namespace Api.Tests.Unit.Controllers;

public class JobsControllerTests
{
    private readonly JobsController _sut;
    private readonly ISender _sender = Substitute.For<ISender>();

    public JobsControllerTests()
    {
        _sut = new JobsController(_sender);
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenJobExists()
    {
        // Arrange
        var response = new JobResponse
        {
            Id = 1,
            Status = CatFetchJobStatus.Completed.ToString(),
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow,
            CatsFetched = 25
        };

        _sender
            .Send(Arg.Any<GetJobQuery>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = (OkObjectResult)await _sut.Get(1, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Value.As<JobResponse>().Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        _sender
            .Send(Arg.Any<GetJobQuery>(), Arg.Any<CancellationToken>())
            .Returns((JobResponse?)null);

        // Act
        var result = (NotFoundResult)await _sut.Get(999, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Get_ShouldSendGetJobQueryWithCorrectId()
    {
        // Arrange
        var response = new JobResponse
        {
            Id = 42,
            Status = CatFetchJobStatus.Pending.ToString(),
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        _sender
            .Send(Arg.Any<GetJobQuery>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        await _sut.Get(42, CancellationToken.None);

        // Assert
        await _sender.Received(1).Send(
            Arg.Is<GetJobQuery>(q => q.Id == 42),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenJobIsPending()
    {
        // Arrange
        var response = new JobResponse
        {
            Id = 1,
            Status = CatFetchJobStatus.Pending.ToString(),
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        _sender
            .Send(Arg.Any<GetJobQuery>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = (OkObjectResult)await _sut.Get(1, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Value.As<JobResponse>().Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenJobHasFailed()
    {
        // Arrange
        var response = new JobResponse
        {
            Id = 1,
            Status = CatFetchJobStatus.Failed.ToString(),
            ErrorMessage = "API call failed",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            CompletedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        _sender
            .Send(Arg.Any<GetJobQuery>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = (OkObjectResult)await _sut.Get(1, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Value.As<JobResponse>().ErrorMessage.Should().Be("API call failed");
    }
}
