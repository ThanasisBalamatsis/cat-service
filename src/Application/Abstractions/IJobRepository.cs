using Domain.Jobs;

namespace Application.Abstractions;

public interface IJobRepository
{
    Task<CatFetchJob> CreateAsync(CancellationToken cancellationToken);
    Task<CatFetchJob?> GetAsync(int id, CancellationToken cancellationToken);
    Task UpdateAsync(CatFetchJob job, CancellationToken cancellationToken);
}
