using Application.Abstractions;
using System.Threading.Channels;

namespace Infrastructure.BackgroundJobs;

internal sealed class CatFetchQueue : ICatFetchQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(
        new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public ValueTask EnqueueAsync(int jobId, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(jobId, cancellationToken);
    }

    public ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
