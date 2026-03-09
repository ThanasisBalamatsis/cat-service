using Application.Abstractions;
using Application.Jobs.Get;
using Domain.Jobs;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Application.Tests.Unit.Jobs.Get;

public class GetJobQueryHandlerTests
{
    private readonly GetJobQueryHandler _sut;
    private readonly IJobRepository _jobRepository = Substitute.For<IJobRepository>();

    public GetJobQueryHandlerTests()
    {
        _sut = new GetJobQueryHandler(_jobRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnJobResponse_WhenJobExists()
    {
        // Arrange
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow,
            CatsFetched = 25
        };

        _jobRepository
            .GetAsync(1, Arg.Any<CancellationToken>())
            .Returns(job);

        var query = new GetJobQuery { Id = 1 };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Completed");
        result.CatsFetched.Should().Be(25);
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenJobDoesNotExist()
    {
        // Arrange
        _jobRepository
            .GetAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var query = new GetJobQuery { Id = 999 };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapAllFields_WhenJobExists()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddMinutes(-10);
        var completedAt = DateTime.UtcNow;

        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Failed,
            ErrorMessage = "API timeout",
            CreatedAt = createdAt,
            CompletedAt = completedAt,
            CatsFetched = 5
        };

        _jobRepository
            .GetAsync(1, Arg.Any<CancellationToken>())
            .Returns(job);

        var query = new GetJobQuery { Id = 1 };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Be("API timeout");
        result.CreatedAt.Should().Be(createdAt);
        result.CompletedAt.Should().Be(completedAt);
        result.CatsFetched.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ShouldReturnPendingStatus_ForNewJob()
    {
        // Arrange
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        _jobRepository
            .GetAsync(1, Arg.Any<CancellationToken>())
            .Returns(job);

        var query = new GetJobQuery { Id = 1 };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Pending");
        result.CompletedAt.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.CatsFetched.Should().Be(0);
    }
}
