using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using BetPortalWebApi.Middleware.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetPortalWebApi.Middleware
{
    public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ISessionServiceTmp _sessionService;

        public SessionAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISessionServiceTmp sessionService)
            : base(options, logger, encoder)
        {
            _sessionService = sessionService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Get session ID from cookie
            var sessionId = Context.Request.Cookies["SessionId"];

            if (string.IsNullOrEmpty(sessionId))
            {
                return AuthenticateResult.NoResult();
            }

            // Validate session
            var session = await _sessionService.GetSessionAsync(sessionId);

            if (session != null)
            {
                // Invalid session, clear cookie
                Context.Response.Cookies.Delete("SessionId");
                return AuthenticateResult.Fail("Invalid session");
            }

            // Create claims from session
            var claims = new List<Claim>
            {
               /* new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                new Claim(ClaimTypes.Name, session.Username),
                new Claim("FullName", session.FullName),
                new Claim("SessionId", session.SessionId)*/
            };

            // Add permission claims
            foreach (var permission in session?.Permissions ?? Enumerable.Empty<string>())
            {
                ////claims.Add(new Claim("Permission", permission));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

            Logger.LogDebug("User {Username} authenticated via session {SessionId}", session?.Username, session?.SessionId);

            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }
    }
}