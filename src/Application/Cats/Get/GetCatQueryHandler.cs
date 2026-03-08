using Application.Abstractions;
using Application.Cats.Responses;
using MediatR;

namespace Application.Cats.Get;

internal sealed class GetCatQueryHandler : IRequestHandler<GetCatQuery, CatResponse?>
{
    private readonly ICatRepository _catRepository;

    public GetCatQueryHandler(ICatRepository catRepository)
    {
        _catRepository = catRepository;
    }

    public async Task<CatResponse?> Handle(GetCatQuery request, CancellationToken cancellationToken)
    {
        var cat = await _catRepository.GetAsync(request.Id, cancellationToken);

        if (cat is null)
        {
            return null;
        }

        var response = cat.ToResponse();
        return response;
    }
}
