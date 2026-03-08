using Application.Abstractions;
using Infrastructure.BackgroundJobs;
using Infrastructure.ExternalServices;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Net;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddCatsApiHttpClient(configuration);

        services.AddScoped<ICatRepository, CatRepository>();
        services.AddScoped<IJobRepository, JobRepository>();

        services.AddSingleton<ICatFetchQueue, CatFetchQueue>();
        services.AddHostedService<CatFetchBackgroundService>();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }

    private static IServiceCollection AddCatsApiHttpClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddHttpClient()
            .AddHttpClient(CatApiService.HttpClientName, client =>
            {
                var baseUrl = configuration["CatsApiBaseUrl"]!;
                var xApiKey = configuration["xApiKey"]!;

                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("x-api-key", xApiKey);
            })
            .AddResilienceHandler($"{CatApiService.HttpClientName}-Pipeline", builder =>
            {
            builder.AddTimeout(TimeSpan.FromSeconds(30));

            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(response =>
                     response.StatusCode == HttpStatusCode.RequestTimeout
                     || response.StatusCode == HttpStatusCode.TooManyRequests)
            });
        });

        services.AddScoped<ICatApiService, CatApiService>();

        return services;
    }
}
