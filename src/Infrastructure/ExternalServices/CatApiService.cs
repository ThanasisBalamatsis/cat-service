using Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.ExternalServices;

internal sealed class CatApiService : ICatApiService
{
    public const string HttpClientName = "CatsApi-Client";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CatApiService> _logger;

    public CatApiService(IHttpClientFactory httpClientFactory, ILogger<CatApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CatApiImage>> FetchCatImagesAsync(int limit, CancellationToken cancellationToken)
    {
        var url = $"v1/images/search?limit={limit}&has_breeds=1&order=RANDOM";

        _logger.LogInformation("Fetching {Limit} cat images from The Cat API", limit);

        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);

        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var apiImages = await response.Content
            .ReadFromJsonAsync<List<CatApiRawImage>>(cancellationToken) ?? [];

        return apiImages.Select(MapToDto).ToList();
    }

    private static CatApiImage MapToDto(CatApiRawImage raw)
    {
        var breeds = raw.Breeds?
            .Select(b => new CatApiBreed { Temperament = b.Temperament })
            .ToList() ?? [];

        return new CatApiImage
        {
            Id = raw.Id,
            Width = raw.Width,
            Height = raw.Height,
            Url = raw.Url,
            Breeds = breeds
        };
    }

    private sealed class CatApiRawImage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("breeds")]
        public List<CatApiRawBreed>? Breeds { get; set; }
    }

    private sealed class CatApiRawBreed
    {
        [JsonPropertyName("temperament")]
        public string? Temperament { get; set; }
    }
}
