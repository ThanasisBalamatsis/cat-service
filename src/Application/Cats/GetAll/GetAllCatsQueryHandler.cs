using Application.Abstractions;
using Application.Cats.Responses;
using MediatR;

namespace Application.Cats.GetAll;

internal sealed class GetAllCatsQueryHandler : IRequestHandler<GetAllCatsQuery, CatsResponse>
{
    private readonly ICatRepository _catRepository;

    public GetAllCatsQueryHandler(ICatRepository catRepository)
    {
        _catRepository = catRepository;
    }

    public async Task<CatsResponse> Handle(GetAllCatsQuery request, CancellationToken cancellationToken)
    {
        var normalisedTag = request.Tag?.Trim().ToLowerInvariant();

        var result = await _catRepository.GetAllAsync(
            tag: normalisedTag,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken
        );

        return result.Cats.ToResponse(request.Page, request.PageSize, result.Total);
    }
}
