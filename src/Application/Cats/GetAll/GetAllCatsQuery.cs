using Application.Cats.Responses;
using MediatR;

namespace Application.Cats.GetAll;

public class GetAllCatsQuery : IRequest<CatsResponse>
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public string? Tag { get; init; }
}
