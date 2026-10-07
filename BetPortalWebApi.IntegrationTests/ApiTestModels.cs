internal sealed record LoginResponse(bool Success, string Message, LoginUser User);

internal sealed record LoginUser(string Username, string FullName);

internal sealed record AuthCheckResponse(bool IsAuthenticated, bool? SessionValid, bool? CookieValid, string? Username);

internal sealed record ApiErrorResponse(string Message);

internal sealed record SportResponse(int Id, string Name, bool Active);
internal sealed record ApiSuccessResponse(bool Success, string Message);
