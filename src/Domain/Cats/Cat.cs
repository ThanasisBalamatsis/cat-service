using Domain.Tags;

namespace Domain.Cats;

public class Cat : BaseEntity
{
    public required string CatId { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string ImageUrl { get; init; }
    public ICollection<Tag> Tags { get; init; } = new List<Tag>();
}
