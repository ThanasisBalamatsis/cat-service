using Application.Jobs.Responses;
using MediatR;

namespace Application.Jobs.Get;

public sealed class GetJobQuery : IRequest<JobResponse?>
{
    public required int Id { get; init; }
}
