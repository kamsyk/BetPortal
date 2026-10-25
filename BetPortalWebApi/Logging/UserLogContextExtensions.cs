using System.Security.Claims;
using Serilog.Context;

namespace BetPortalWebApi.Logging
{
    public static class UserLogContextExtensions
    {
        public const string UserIdProperty = "UserId";

        /// <summary>Adds the authenticated user's id to log events written during the request.</summary>
        public static IApplicationBuilder UseUserLogContext(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                using (LogContext.PushProperty(UserIdProperty, GetUserId(context)))
                {
                    await next(context);
                }
            });
        }

        public static int? GetUserId(HttpContext context)
        {
            return int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
        }
    }
}
