using Microsoft.AspNetCore.Mvc;

namespace BetPortalWebApi.ErrorHandling
{
    public static class ProblemDetailsResponse
    {
        /// <summary>
        /// Writes sanitized Problem Details. Falls back to JSON when content negotiation rejects the problem writer
        /// (for example, a browser navigation that accepts only HTML).
        /// </summary>
        public static async Task WriteAsync(HttpContext httpContext, int statusCode, string title, string? detail)
        {
            httpContext.Response.StatusCode = statusCode;
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
            };
            var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
            if (await problemDetailsService.TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem }))
            {
                return;
            }

            problem.Extensions["errorId"] = ErrorReference.Get(httpContext);
            await httpContext.Response.WriteAsJsonAsync(
                problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", httpContext.RequestAborted);
        }
    }
}
