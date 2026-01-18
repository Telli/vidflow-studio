using Microsoft.AspNetCore.Diagnostics;
using VidFlow.Api.Domain.Exceptions;

namespace VidFlow.Api.Shared;

/// <summary>
/// Global exception handler that maps domain exceptions to HTTP status codes.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, response) = exception switch
        {
            DomainException de => (StatusCodes.Status400BadRequest, new ApiErrorResponse(de.ErrorCode, de.Message)),
            KeyNotFoundException => (StatusCodes.Status404NotFound, new ApiErrorResponse("NOT_FOUND", "Resource not found")),
            ArgumentException ae => (StatusCodes.Status400BadRequest, new ApiErrorResponse("INVALID_ARGUMENT", ae.Message)),
            _ => (StatusCodes.Status500InternalServerError, new ApiErrorResponse("INTERNAL_ERROR", "An unexpected error occurred"))
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {ErrorCode}", response.ErrorCode);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
