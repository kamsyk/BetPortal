using BetPortalWebApi.Utils;
using Microsoft.AspNetCore.Authentication;

namespace BetPortalWebApi.ErrorHandling
{
    public static class ExternalAuthRemoteFailureHandler
    {
        /// <summary>
        /// Handles OAuth failures (cancellation, denied consent, invalid state) at the provider callback path by logging a
        /// sanitized diagnostic code and redirecting to the portal login dialog.
        /// </summary>
        public static async Task HandleAsync(RemoteFailureContext context, string provider)
        {
            var httpContext = context.HttpContext;
            var diagnosticCode = MicrosoftAuthenticationDiagnostics.GetFailureCode(context.Failure);
            httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger($"{provider}Authentication")
                .LogWarning("{Provider} authentication failed. DiagnosticCode: {DiagnosticCode}", provider, diagnosticCode);
            context.HandleResponse();

            string? destination = null;
            context.Properties?.Items.TryGetValue("returnUrl", out destination);
            var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
            if (ExternalAuthErrorRedirect.TryCreate(
                configuration, provider, ExternalAuthErrorCodes.SignInFailed, ErrorReference.Get(httpContext), destination, out var url))
            {
                httpContext.Response.Redirect(url);
                return;
            }

            await ProblemDetailsResponse.WriteAsync(
                httpContext,
                StatusCodes.Status401Unauthorized,
                $"{provider} sign-in could not be completed",
                $"{provider} sign-in was cancelled or could not be verified. Please try again.");
        }
    }
}
