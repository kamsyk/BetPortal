using System.Diagnostics;

namespace BetPortalWebApi.ErrorHandling
{
    public static class ErrorReference
    {
        /// <summary>
        /// Returns the reference shown to users. It equals the W3C trace id stored in the <c>TraceId</c> column of <c>BpAppLogs</c>.
        /// </summary>
        public static string Get(HttpContext httpContext)
        {
            var activity = Activity.Current;
            return activity != null && activity.TraceId != default
                ? activity.TraceId.ToHexString()
                : httpContext.TraceIdentifier;
        }
    }
}
