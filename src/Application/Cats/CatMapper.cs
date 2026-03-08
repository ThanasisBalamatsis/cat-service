using Application.Cats.Responses;
using Domain.Cats;
using System.Diagnostics.CodeAnalysis;

namespace Application.Cats;

[ExcludeFromCodeCoverage]
public static class CatMapper
{
    public static CatResponse ToResponse(this Cat cat)
    {
        return new CatResponse
        {
            Id = cat.Id,
            CreatedAt = cat.CreatedAt,
            CatId = cat.CatId,
            Width = cat.Width,
            Height = cat.Height,
            ImageUrl = cat.ImageUrl,
            Tags = cat.Tags.Select(t => t.Name).ToList()
        };
    }

    public static CatsResponse ToResponse(this IEnumerable<Cat> cats, int page, int pageSize, int total)
    {
        return new CatsResponse
        {
            Cats = cats.Select(ToResponse),
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }
}
