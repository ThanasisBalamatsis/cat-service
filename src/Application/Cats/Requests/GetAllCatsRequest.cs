namespace Application.Cats.Requests;

public class GetAllCatsRequest : PagedRequest
{
    public string? Tag { get; init; }
}
