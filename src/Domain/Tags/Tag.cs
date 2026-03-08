using Domain.Cats;

namespace Domain.Tags;

public class Tag : BaseEntity
{
    public required string Name { get; init; }
    public ICollection<Cat> Cats { get; init; } = new List<Cat>();
}
