using Application.Abstractions;
using Domain.Cats;
using Domain.Jobs;
using Domain.Tags;
using FluentAssertions;
using Infrastructure.BackgroundJobs;
using Infrastructure.Tests.Unit.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Infrastructure.Tests.Unit.BackgroundJobs;

public class CatFetchBackgroundServiceTests
{
    private readonly ICatFetchQueue _queue = Substitute.For<ICatFetchQueue>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IJobRepository _jobRepository = Substitute.For<IJobRepository>();
    private readonly ICatRepository _catRepository = Substitute.For<ICatRepository>();
    private readonly ICatApiService _catApiService = Substitute.For<ICatApiService>();
    private readonly FakeLogger<CatFetchBackgroundService> _logger;
    private readonly CatFetchBackgroundService _sut;

    public CatFetchBackgroundServiceTests()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IJobRepository)).Returns(_jobRepository);
        serviceProvider.GetService(typeof(ICatRepository)).Returns(_catRepository);
        serviceProvider.GetService(typeof(ICatApiService)).Returns(_catApiService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        _scopeFactory.CreateScope().Returns(scope);
        _logger = new FakeLogger<CatFetchBackgroundService>();

        _sut = new CatFetchBackgroundService(_queue, _scopeFactory, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessJob_WhenJobIsDequeued()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        var callCount = 0;
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount > 1)
                {
                    cts.Cancel();
                    throw new OperationCanceledException();
                }
                return new ValueTask<int>(1);
            });

        _jobRepository.GetAsync(1, Arg.Any<CancellationToken>()).Returns(job);

        var images = new List<CatApiImage>
        {
            new()
            {
                Id = "abc123",
                Width = 800,
                Height = 600,
                Url = "https://cdn2.thecatapi.com/images/abc123.jpg",
                Breeds = new List<CatApiBreed>
                {
                    new() { Temperament = "Playful, Active" }
                }
            }
        };

        _catApiService
            .FetchCatImagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(images);

        _catRepository
            .GetExistingCatIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());

        _catRepository
            .GetOrCreateTagsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Tag>
            {
                ["playful"] = new Tag { Name = "playful", CreatedAt = DateTime.UtcNow },
                ["active"] = new Tag { Name = "active", CreatedAt = DateTime.UtcNow }
            });

        var statuses = new List<CatFetchJobStatus>();

        _jobRepository
            .When(x => x.UpdateAsync(Arg.Any<CatFetchJob>(), Arg.Any<CancellationToken>()))
            .Do(ci =>
            {
                var jobArg = ci.Arg<CatFetchJob>();
                statuses.Add(jobArg.Status);
            });

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(500);
        await _sut.StopAsync(CancellationToken.None);

        // Assert
        statuses.Should().Contain(CatFetchJobStatus.Running);

        await _catRepository.Received().AddCatAsync(
            Arg.Any<Cat>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipJob_WhenJobNotFoundInDatabase()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var callCount = 0;
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount > 1)
                {
                    cts.Cancel();
                    throw new OperationCanceledException();
                }
                return new ValueTask<int>(999);
            });

        _jobRepository.GetAsync(999, Arg.Any<CancellationToken>())
            .Returns((CatFetchJob?)null);

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(500);
        await _sut.StopAsync(CancellationToken.None);

        // Assert
        await _catApiService.DidNotReceive()
            .FetchCatImagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetJobToFailed_WhenUnrecoverableErrorOccurs()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        var callCount = 0;
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount > 1)
                {
                    cts.Cancel();
                    throw new OperationCanceledException();
                }
                return new ValueTask<int>(1);
            });

        _jobRepository.GetAsync(1, Arg.Any<CancellationToken>()).Returns(job);

        _catApiService
            .FetchCatImagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API is down"));

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(500);
        await _sut.StopAsync(CancellationToken.None);

        // Assert
        await _jobRepository.Received().UpdateAsync(
            Arg.Is<CatFetchJob>(j => j.Status == CatFetchJobStatus.Failed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopGracefully_WhenCancellationRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                cts.Cancel();
                return ValueTask.FromException<int>(new OperationCanceledException());
            });

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(200);

        // Assert - should not throw
        var act = () => _sut.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipDuplicateCats()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        var callCount = 0;
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns((Func<NSubstitute.Core.CallInfo, ValueTask<int>>)(callInfo =>
            {
                callCount++;
                if (callCount > 1)
                {
                    cts.Cancel();
                    throw new OperationCanceledException();
                }
                return new ValueTask<int>(1);
            }));

        _jobRepository.GetAsync(1, Arg.Any<CancellationToken>()).Returns(job);

        var images = new List<CatApiImage>
        {
            new()
            {
                Id = "existing-cat",
                Width = 800,
                Height = 600,
                Url = "https://cdn2.thecatapi.com/images/existing.jpg",
                Breeds = new List<CatApiBreed>()
            }
        };

        _catApiService
            .FetchCatImagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(images);

        _catRepository
            .GetExistingCatIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "existing-cat" });

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(500);
        await _sut.StopAsync(CancellationToken.None);

        // Assert
        await _catRepository.DidNotReceive()
            .AddCatAsync(Arg.Any<Cat>(), Arg.Any<CancellationToken>());
    }
}
