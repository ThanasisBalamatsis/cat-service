using Application.Behaviors;
using Application.Cats.GetAll;
using Application.Cats.Responses;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;

namespace Application.Tests.Unit.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidatorsRegistered()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<GetAllCatsQuery>>();
        var behavior = new ValidationBehavior<GetAllCatsQuery, CatsResponse>(validators);

        var expectedResponse = new CatsResponse
        {
            Cats = Enumerable.Empty<CatResponse>(),
            Page = 1,
            PageSize = 10,
            Total = 0
        };

        var next = Substitute.For<RequestHandlerDelegate<CatsResponse>>();
        next().Returns(expectedResponse);

        var request = new GetAllCatsQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await next.Received(1)();
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenValidationPasses()
    {
        // Arrange
        var validator = Substitute.For<IValidator<GetAllCatsQuery>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<GetAllCatsQuery>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var validators = new List<IValidator<GetAllCatsQuery>> { validator };
        var behavior = new ValidationBehavior<GetAllCatsQuery, CatsResponse>(validators);

        var expectedResponse = new CatsResponse
        {
            Cats = Enumerable.Empty<CatResponse>(),
            Page = 1,
            PageSize = 10,
            Total = 0
        };

        var next = Substitute.For<RequestHandlerDelegate<CatsResponse>>();
        next().Returns(expectedResponse);

        var request = new GetAllCatsQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await next.Received(1)();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Page", "Page must be greater than 0")
        };

        var validator = Substitute.For<IValidator<GetAllCatsQuery>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<GetAllCatsQuery>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var validators = new List<IValidator<GetAllCatsQuery>> { validator };
        var behavior = new ValidationBehavior<GetAllCatsQuery, CatsResponse>(validators);

        var next = Substitute.For<RequestHandlerDelegate<CatsResponse>>();

        var request = new GetAllCatsQuery { Page = 0, PageSize = 10 };

        // Act
        var act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.PropertyName == "Page"));

        await next.DidNotReceive()();
    }

    [Fact]
    public async Task Handle_ShouldAggregateErrors_FromMultipleValidators()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<GetAllCatsQuery>>();
        validator1
            .ValidateAsync(Arg.Any<ValidationContext<GetAllCatsQuery>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Page", "Page error") }));

        var validator2 = Substitute.For<IValidator<GetAllCatsQuery>>();
        validator2
            .ValidateAsync(Arg.Any<ValidationContext<GetAllCatsQuery>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("PageSize", "PageSize error") }));

        var validators = new List<IValidator<GetAllCatsQuery>> { validator1, validator2 };
        var behavior = new ValidationBehavior<GetAllCatsQuery, CatsResponse>(validators);

        var next = Substitute.For<RequestHandlerDelegate<CatsResponse>>();
        var request = new GetAllCatsQuery { Page = 0, PageSize = 0 };

        // Act
        var act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCount(2);
    }
}
