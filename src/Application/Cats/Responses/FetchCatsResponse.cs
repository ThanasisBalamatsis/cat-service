using System.Diagnostics.CodeAnalysis;

namespace Application.Cats.Responses;

[ExcludeFromCodeCoverage]
public sealed class FetchCatsResponse
{
    public required int JobId { get; init; }
    public required string Status { get; init; }
}
