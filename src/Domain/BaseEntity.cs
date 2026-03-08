namespace Domain;

public class BaseEntity
{
    public int Id { get; private set; }
    public required DateTime CreatedAt { get; init; }
}
