using Application.Abstractions;
using Application.Jobs.Responses;
using MediatR;

namespace Application.Jobs.Get;

internal sealed class GetJobQueryHandler : IRequestHandler<GetJobQuery, JobResponse?>
{
    private readonly IJobRepository _jobRepository;

    public GetJobQueryHandler(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<JobResponse?> Handle(GetJobQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetAsync(request.Id, cancellationToken);

        if (job is null)
        {
            return null;
        }

        return new JobResponse
        {
            Id = job.Id,
            Status = job.Status.ToString(),
            ErrorMessage = job.ErrorMessage,
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            CatsFetched = job.CatsFetched
        };
    }
}
