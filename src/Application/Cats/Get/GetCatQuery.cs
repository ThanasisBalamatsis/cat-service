using Application.Cats.Responses;
using MediatR;

namespace Application.Cats.Get;

public class GetCatQuery : IRequest<CatResponse>
{
    public required int Id { get; init; }
}
