using System.Diagnostics.CodeAnalysis;

namespace Application.Jobs.Responses;

[ExcludeFromCodeCoverage]
public sealed class JobResponse
{
    public required int Id { get; init; }
    public required string Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int CatsFetched { get; init; }
}
