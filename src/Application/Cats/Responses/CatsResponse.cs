using System.Diagnostics.CodeAnalysis;

namespace Application.Cats.Responses;

[ExcludeFromCodeCoverage]
public sealed class CatsResponse
{
    public required IEnumerable<CatResponse> Cats { get; init; }
        = Enumerable.Empty<CatResponse>();

    public required int PageSize { get; init; }
    public required int Page { get; init; }
    public required int Total { get; init; }
    public bool HasNextPage => Total > (Page * PageSize);
}
