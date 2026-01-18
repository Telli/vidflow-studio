namespace VidFlow.Api.Shared;

/// <summary>
/// Standard API error response format.
/// </summary>
public record ApiErrorResponse(
    string ErrorCode,
    string Message,
    IDictionary<string, string[]>? ValidationErrors = null);
