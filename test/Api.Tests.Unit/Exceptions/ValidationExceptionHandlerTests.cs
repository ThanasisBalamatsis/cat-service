using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Presentation.Exceptions;

namespace Api.Tests.Unit.Exceptions;

public class ValidationExceptionHandlerTests
{
    private readonly IProblemDetailsService _problemDetailsService = Substitute.For<IProblemDetailsService>();
    private readonly ILogger<ValidationExceptionHandler> _logger = Substitute.For<ILogger<ValidationExceptionHandler>>();
    private readonly ValidationExceptionHandler _sut;

    public ValidationExceptionHandlerTests()
    {
        _sut = new ValidationExceptionHandler(_problemDetailsService, _logger);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnFalse_WhenExceptionIsNotValidationException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Not a validation exception");

        // Act
        var result = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetStatusCode400_WhenValidationExceptionThrown()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var failures = new List<ValidationFailure>
        {
            new("Page", "Page must be greater than 0")
        };
        var exception = new ValidationException(failures);

        _problemDetailsService
            .TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(true);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnTrue_WhenValidationExceptionHandled()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var failures = new List<ValidationFailure>
        {
            new("PageSize", "Page size must be greater than 0")
        };
        var exception = new ValidationException(failures);

        _problemDetailsService
            .TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(true);

        // Act
        var result = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnTrue_WhenTryWriteAsyncReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var failures = new List<ValidationFailure>
        {
            new("Page", "Page must be greater than 0")
        };
        var exception = new ValidationException(failures);

        _problemDetailsService
            .TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(false);

        // Act
        var result = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_ShouldWriteJsonFallback_WhenTryWriteAsyncReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var failures = new List<ValidationFailure>
        {
            new("Page", "Page must be greater than 0")
        };
        var exception = new ValidationException(failures);

        _problemDetailsService
            .TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(false);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        responseBody.Should().Contain("One or more validation errors occurred.");
        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldGroupErrorsByPropertyName()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var failures = new List<ValidationFailure>
        {
            new("Page", "Page must be greater than 0"),
            new("PageSize", "Page size must be greater than 0"),
            new("PageSize", "Page size must be less than or equal to 100")
        };
        var exception = new ValidationException(failures);

        ProblemDetailsContext? capturedContext = null;
        _problemDetailsService
            .TryWriteAsync(Arg.Do<ProblemDetailsContext>(ctx => capturedContext = ctx))
            .Returns(true);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.ProblemDetails.Extensions.Should().ContainKey("errors");
        capturedContext.ProblemDetails.Status.Should().Be(400);
        capturedContext.ProblemDetails.Detail.Should().Be("One or more validation errors occurred.");
    }
}
