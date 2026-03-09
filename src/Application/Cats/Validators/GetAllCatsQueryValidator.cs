using Application.Cats.GetAll;
using FluentValidation;

namespace Application.Cats.Validators;

public sealed class GetAllCatsQueryValidator : AbstractValidator<GetAllCatsQuery>
{
    public GetAllCatsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be less than or equal to 100");
    }
}
