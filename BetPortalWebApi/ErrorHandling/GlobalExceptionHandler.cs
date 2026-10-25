using BetPortalWebApi.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Serilog.Context;

namespace BetPortalWebApi.ErrorHandling
{
    /// <summary>
    /// Logs unhandled exceptions and converts them into responses without technical details. Users receive an error
    /// reference that matches the <c>TraceId</c> column of the log table.
    /// </summary>
    public sealed class GlobalExceptionHandler(IConfiguration configuration, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public const string Title = "An unexpected error occurred";

        public const string Detail = "Please try again later. If the problem persists, contact support and quote the error reference.";

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var errorId = ErrorReference.Get(httpContext);
            using (LogContext.PushProperty(UserLogContextExtensions.UserIdProperty, UserLogContextExtensions.GetUserId(httpContext)))
            {
                logger.LogError(
                    exception,
                    "Unhandled exception while processing {RequestMethod} {RequestPath}. ErrorId: {ErrorId}",
                    httpContext.Request.Method,
                    httpContext.Request.Path.Value,
                    errorId);
            }

            // External sign-in endpoints are browser navigations, so return the user to the portal instead of rendering JSON.
            if (HttpMethods.IsGet(httpContext.Request.Method)
                && ExternalAuthErrorRedirect.TryGetProvider(httpContext.Request.Path, out var provider)
                && ExternalAuthErrorRedirect.TryCreate(
                    configuration, provider, ExternalAuthErrorCodes.SignInFailed, errorId, null, out var url))
            {
                httpContext.Response.Redirect(url);
                return true;
            }

            await ProblemDetailsResponse.WriteAsync(httpContext, StatusCodes.Status500InternalServerError, Title, Detail);
            return true;
        }
    }
}
