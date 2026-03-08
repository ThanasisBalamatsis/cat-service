using Application.Abstractions;
using Domain.Cats;
using Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

internal sealed class CatFetchBackgroundService : BackgroundService
{
    private const int FetchLimit = 25;
    private const int ApiFetchBatchSize = 25;
    private const int MaxApiAttempts = 5;

    private readonly ICatFetchQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CatFetchBackgroundService> _logger;

    public CatFetchBackgroundService(
        ICatFetchQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<CatFetchBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CatFetchBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await _queue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CatFetchBackgroundService loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("CatFetchBackgroundService stopped");
    }

    private async Task ProcessJobAsync(int jobId, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var catRepository = scope.ServiceProvider.GetRequiredService<ICatRepository>();
        var catApiService = scope.ServiceProvider.GetRequiredService<ICatApiService>();

        var job = await jobRepository.GetAsync(jobId, stoppingToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found, skipping", jobId);
            return;
        }

        try
        {
            job.Status = CatFetchJobStatus.Running;
            await jobRepository.UpdateAsync(job, stoppingToken);

            var savedCount = 0;
            var apiAttempts = 0;

            while (savedCount < FetchLimit && apiAttempts < MaxApiAttempts)
            {
                apiAttempts++;
                var remaining = FetchLimit - savedCount;

                _logger.LogInformation(
                    "Job {JobId}: API attempt {Attempt}/{MaxAttempts}, need {Remaining} more cats",
                    jobId, apiAttempts, MaxApiAttempts, remaining);

                var images = await catApiService.FetchCatImagesAsync(ApiFetchBatchSize, stoppingToken);

                if (images.Count == 0)
                {
                    _logger.LogWarning("Job {JobId}: Cat API returned 0 images", jobId);
                    break;
                }

                // Filter out images already in our database
                var apiCatIds = images.Select(i => i.Id).ToList();
                var existingCatIds = await catRepository.GetExistingCatIdsAsync(apiCatIds, stoppingToken);
                var newImages = images
                    .Where(i => !existingCatIds.Contains(i.Id))
                    .Take(remaining)
                    .ToList();

                if (newImages.Count == 0)
                {
                    _logger.LogInformation("Job {JobId}: All {Count} images from this batch were duplicates", jobId, images.Count);
                    continue;
                }

                // Pre-create/reuse tags for this batch
                var allTagNames = newImages
                    .SelectMany(ParseTemperamentTags)
                    .Distinct()
                    .ToList();

                var tagLookup = await catRepository.GetOrCreateTagsAsync(allTagNames, stoppingToken);

                // Process each cat individually for resilience
                foreach (var image in newImages)
                {
                    Cat? cat = null;
                    try
                    {
                        var catTagNames = ParseTemperamentTags(image);
                        var catTags = catTagNames
                            .Where(t => tagLookup.ContainsKey(t))
                            .Select(t => tagLookup[t])
                            .ToList();

                        cat = new Cat
                        {
                            CatId = image.Id,
                            Width = image.Width,
                            Height = image.Height,
                            ImageUrl = image.Url,
                            CreatedAt = DateTime.UtcNow,
                            Tags = catTags
                        };

                        await catRepository.AddCatAsync(cat, stoppingToken);
                        savedCount++;

                        _logger.LogInformation(
                            "Job {JobId}: Saved cat {CatId} ({Saved}/{Target})",
                            jobId, image.Id, savedCount, FetchLimit);
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        if (cat is not null) catRepository.DetachEntity(cat);
                        _logger.LogWarning(
                            "Job {JobId}: Cat {CatId} was already inserted (concurrent duplicate), skipping",
                            jobId, image.Id);
                    }
                    catch (Exception ex)
                    {
                        if (cat is not null) catRepository.DetachEntity(cat);
                        _logger.LogError(ex,
                            "Job {JobId}: Failed to process cat {CatId}, continuing with next",
                            jobId, image.Id);
                    }
                }

                // Update progress
                job.CatsFetched = savedCount;
                await jobRepository.UpdateAsync(job, stoppingToken);
            }

            job.Status = CatFetchJobStatus.Completed;
            job.CatsFetched = savedCount;
            job.CompletedAt = DateTime.UtcNow;
            await jobRepository.UpdateAsync(job, stoppingToken);

            _logger.LogInformation("Job {JobId}: Completed. {Saved} cats saved", jobId, savedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId}: Failed with unrecoverable error", jobId);

            try
            {
                job.Status = CatFetchJobStatus.Failed;
                job.ErrorMessage = ex.Message.Length > 500
                    ? ex.Message[..500]
                    : ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                await jobRepository.UpdateAsync(job, stoppingToken);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Job {JobId}: Also failed to update job status to Failed", jobId);
            }
        }
    }

    private static IEnumerable<string> ParseTemperamentTags(CatApiImage image)
    {
        if (image.Breeds is null || image.Breeds.Count == 0)
            return [];

        return image.Breeds
            .Where(b => !string.IsNullOrWhiteSpace(b.Temperament))
            .SelectMany(b => b.Temperament!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message?.Contains("2601") == true
            || ex.InnerException?.Message?.Contains("2627") == true;
    }
}
