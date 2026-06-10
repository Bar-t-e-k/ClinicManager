using ClinicManager.Web.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClinicManager.Tests;

public class GlobalExceptionFilterTests
{
    private readonly Mock<ILogger<GlobalExceptionFilter>> _mockLogger;
    private readonly GlobalExceptionFilter _filter;

    public GlobalExceptionFilterTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionFilter>>();
        _filter = new GlobalExceptionFilter(_mockLogger.Object);
    }

    [Fact]
    public void OnException_WhenRequestIsApi_ShouldSetProblemDetailsAndMarkHandled()
    {
        // Arrange
        var exception = new Exception("Testowy wyjątek API");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/visits";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var exceptionContext = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };

        // Act
        _filter.OnException(exceptionContext);

        // Assert
        Assert.True(exceptionContext.ExceptionHandled);
        var objectResult = Assert.IsType<ObjectResult>(exceptionContext.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("Wystąpił wewnętrzny błąd serwera.", problemDetails.Title);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Nieobsłużony wyjątek")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnException_WhenRequestIsNotApi_ShouldOnlyLogAndNotModifyResult()
    {
        // Arrange
        var exception = new Exception("Testowy wyjątek MVC");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/Home/Index"; 

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var exceptionContext = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };

        // Act
        _filter.OnException(exceptionContext);

        // Assert
        Assert.False(exceptionContext.ExceptionHandled);
        Assert.Null(exceptionContext.Result);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Nieobsłużony wyjątek")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}