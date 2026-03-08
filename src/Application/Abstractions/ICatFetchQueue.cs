namespace Application.Abstractions;

public interface ICatFetchQueue
{
    ValueTask EnqueueAsync(int jobId, CancellationToken cancellationToken);
    ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
}
