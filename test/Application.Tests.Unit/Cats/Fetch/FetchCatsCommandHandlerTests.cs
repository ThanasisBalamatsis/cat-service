using Application.Abstractions;
using Application.Cats.Fetch;
using Domain.Jobs;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Cats.Fetch;

public class FetchCatsCommandHandlerTests
{
    private readonly FetchCatsCommandHandler _sut;
    private readonly IJobRepository _jobRepository = Substitute.For<IJobRepository>();
    private readonly ICatFetchQueue _catFetchQueue = Substitute.For<ICatFetchQueue>();

    public FetchCatsCommandHandlerTests()
    {
        _sut = new FetchCatsCommandHandler(_jobRepository, _catFetchQueue);
    }

    [Fact]
    public async Task Handle_ShouldCreateJob()
    {
        // Arrange
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        _jobRepository
            .CreateAsync(Arg.Any<CancellationToken>())
            .Returns(job);

        var command = new FetchCatsCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _jobRepository.Received(1).CreateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldEnqueueJobId()
    {
        // Arrange
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        _jobRepository
            .CreateAsync(Arg.Any<CancellationToken>())
            .Returns(job);

        var command = new FetchCatsCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _catFetchQueue.Received(1).EnqueueAsync(job.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFetchCatsResponse_WithPendingStatus()
    {
        // Arrange
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        _jobRepository
            .CreateAsync(Arg.Any<CancellationToken>())
            .Returns(job);

        var command = new FetchCatsCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.JobId.Should().Be(job.Id);
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_ShouldEnqueueAfterCreatingJob()
    {
        // Arrange
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        _jobRepository
            .CreateAsync(Arg.Any<CancellationToken>())
            .Returns(job);

        var command = new FetchCatsCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            _jobRepository.CreateAsync(Arg.Any<CancellationToken>());
            _catFetchQueue.EnqueueAsync(job.Id, Arg.Any<CancellationToken>());
        });
    }
}
