using Application.Cats.GetAll;
using Application.Cats.Validators;
using FluentAssertions;

namespace Application.Tests.Unit.Cats.Validators;

public class GetAllCatsQueryValidatorTests
{
    private readonly GetAllCatsQueryValidator _sut = new();

    [Fact]
    public void Validate_ShouldPass_WhenPageAndPageSizeAreValid()
    {
        // Arrange
        var query = new GetAllCatsQuery { Page = 1, PageSize = 10 };

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenPageIsZero()
    {
        // Arrange
        var query = new GetAllCatsQuery { Page = 0, PageSize = 10 };

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPageIsNegative()
    {
        // Arrange
        var query = new GetAllCatsQuery { Page = -1, PageSize = 10 };

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPageSizeIsZero()
    {
        // Arrange
        var query = new GetAllCatsQuery { Page = 1, PageSize = 0 };

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPageSizeIsNegative()
    {
        // Arrange
        var query = new GetAllCatsQuery { Page = 1, PageSize = -5 };

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPageSizeExceeds100()
    {
        // Arrange
        var query = new GetAllCatsQuery { Page = 1, PageSize = 101 };

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "PageSize" &&
            e.ErrorMessage == "Page size must be less than or equal to 100");
    }

    [Fact]
    public void Validate_ShouldPass_WhenPageSizeIs100()
    {
        // Arrange
        var query = new GetAllCatsQuery { Page = 1, PageSize = 100 };

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenPageSizeIs1()
    {
        // Arrange
        var query = new GetAllCatsQuery { Page = 1, PageSize = 1 };

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenTagIsNull()
    {
        // Arrange
        var query = new GetAllCatsQuery { Page = 1, PageSize = 10, Tag = null };

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenTagIsProvided()
    {
        // Arrange
        var query = new GetAllCatsQuery { Page = 1, PageSize = 10, Tag = "playful" };

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
