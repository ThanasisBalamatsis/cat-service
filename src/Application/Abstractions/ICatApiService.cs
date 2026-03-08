namespace Application.Abstractions;

public interface ICatApiService
{
    Task<IReadOnlyList<CatApiImage>> FetchCatImagesAsync(int limit, CancellationToken cancellationToken);
}

public sealed class CatApiImage
{
    public required string Id { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string Url { get; init; }
    public IReadOnlyList<CatApiBreed> Breeds { get; init; } = [];
}

public sealed class CatApiBreed
{
    public string? Temperament { get; init; }
}
