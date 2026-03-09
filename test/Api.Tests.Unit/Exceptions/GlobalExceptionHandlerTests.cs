using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Presentation.Exceptions;

namespace Api.Tests.Unit.Exceptions;

public class GlobalExceptionHandlerTests
{
    private readonly IProblemDetailsService _problemDetailsService = Substitute.For<IProblemDetailsService>();
    private readonly ILogger<GlobalExceptionHandler> _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
    private readonly GlobalExceptionHandler _sut;

    public GlobalExceptionHandlerTests()
    {
        _sut = new GlobalExceptionHandler(_problemDetailsService, _logger);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetStatusCode500_ForGeneralException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var exception = new InvalidOperationException("Something went wrong");

        _problemDetailsService
            .TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(true);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetStatusCode400_ForApplicationException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var exception = new ApplicationException("Bad request");

        _problemDetailsService
            .TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(true);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnTrue_WhenExceptionHandled()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var exception = new Exception("Test error");

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

        var exception = new InvalidOperationException("Something went wrong");

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

        var exception = new InvalidOperationException("Detailed error message");

        _problemDetailsService
            .TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(false);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        responseBody.Should().Contain("An exception occurred");
        responseBody.Should().Contain("Detailed error message");
        httpContext.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldWriteProblemDetails_WithExceptionDetails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var exception = new InvalidOperationException("Detailed error message");

        ProblemDetailsContext? capturedContext = null;
        _problemDetailsService
            .TryWriteAsync(Arg.Do<ProblemDetailsContext>(ctx => capturedContext = ctx))
            .Returns(true);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.ProblemDetails.Title.Should().Be("An exception occurred");
        capturedContext.ProblemDetails.Detail.Should().Be("Detailed error message");
        capturedContext.ProblemDetails.Type.Should().Be("InvalidOperationException");
    }
}
