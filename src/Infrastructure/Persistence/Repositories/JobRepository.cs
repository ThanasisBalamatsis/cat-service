using Application.Abstractions;
using Domain.Jobs;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Persistence.Repositories;

[ExcludeFromCodeCoverage]
internal sealed class JobRepository : IJobRepository
{
    private readonly ApplicationDbContext _dbContext;

    public JobRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CatFetchJob> CreateAsync(CancellationToken cancellationToken)
    {
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        _dbContext.CatFetchJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return job;
    }

    public async Task<CatFetchJob?> GetAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.CatFetchJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(CatFetchJob job, CancellationToken cancellationToken)
    {
        _dbContext.CatFetchJobs.Update(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
