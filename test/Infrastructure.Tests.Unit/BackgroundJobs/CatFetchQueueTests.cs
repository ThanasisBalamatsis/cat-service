using FluentAssertions;
using Infrastructure.BackgroundJobs;

namespace Infrastructure.Tests.Unit.BackgroundJobs;

public class CatFetchQueueTests
{
    private readonly CatFetchQueue _sut = new();

    [Fact]
    public async Task EnqueueAsync_ThenDequeueAsync_ShouldReturnSameJobId()
    {
        // Arrange
        var jobId = 42;

        // Act
        await _sut.EnqueueAsync(jobId, CancellationToken.None);
        var result = await _sut.DequeueAsync(CancellationToken.None);

        // Assert
        result.Should().Be(jobId);
    }

    [Fact]
    public async Task MultipleEnqueue_ShouldDequeueInOrder()
    {
        // Arrange & Act
        await _sut.EnqueueAsync(1, CancellationToken.None);
        await _sut.EnqueueAsync(2, CancellationToken.None);
        await _sut.EnqueueAsync(3, CancellationToken.None);

        var first = await _sut.DequeueAsync(CancellationToken.None);
        var second = await _sut.DequeueAsync(CancellationToken.None);
        var third = await _sut.DequeueAsync(CancellationToken.None);

        // Assert
        first.Should().Be(1);
        second.Should().Be(2);
        third.Should().Be(3);
    }

    [Fact]
    public async Task DequeueAsync_ShouldBlockUntilItemAvailable()
    {
        // Arrange
        var dequeuedJobId = 0;
        var dequeueTask = Task.Run(async () =>
        {
            dequeuedJobId = await _sut.DequeueAsync(CancellationToken.None);
        });

        // Give the dequeue task time to start waiting
        await Task.Delay(100);

        // Act
        await _sut.EnqueueAsync(99, CancellationToken.None);
        await dequeueTask;

        // Assert
        dequeuedJobId.Should().Be(99);
    }

    [Fact]
    public async Task DequeueAsync_ShouldThrow_WhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _sut.DequeueAsync(cts.Token).AsTask();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task EnqueueAsync_ShouldThrow_WhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _sut.EnqueueAsync(1, cts.Token).AsTask();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
