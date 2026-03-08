using Application.Cats.Responses;
using MediatR;

namespace Application.Cats.Fetch;

public sealed class FetchCatsCommand : IRequest<FetchCatsResponse>;
