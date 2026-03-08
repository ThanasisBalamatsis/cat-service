using Application.Abstractions;
using Application.Cats.Responses;
using MediatR;

namespace Application.Cats.Fetch;

internal sealed class FetchCatsCommandHandler : IRequestHandler<FetchCatsCommand, FetchCatsResponse>
{
    private readonly IJobRepository _jobRepository;
    private readonly ICatFetchQueue _catFetchQueue;

    public FetchCatsCommandHandler(
        IJobRepository jobRepository,
        ICatFetchQueue catFetchQueue)
    {
        _jobRepository = jobRepository;
        _catFetchQueue = catFetchQueue;
    }

    public async Task<FetchCatsResponse> Handle(FetchCatsCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.CreateAsync(cancellationToken);

        await _catFetchQueue.EnqueueAsync(job.Id, cancellationToken);

        return new FetchCatsResponse
        {
            JobId = job.Id,
            Status = job.Status.ToString()
        };
    }
}
