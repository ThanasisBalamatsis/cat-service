using System.Diagnostics.CodeAnalysis;

namespace Application.Cats.Responses;

[ExcludeFromCodeCoverage]
public sealed class CatResponse
{
    public required int Id { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string CatId { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string ImageUrl { get; init; }
    public required List<string> Tags { get; init; } = new();
}
