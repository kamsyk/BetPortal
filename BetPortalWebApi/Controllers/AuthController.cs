using System.Security.Claims;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.Models;
using BpDbLayer.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BpBaseController
    {
        private readonly BpDbContext _dbContext;
        private readonly ISessionService _sessionService;
        private readonly IGoogleAccountService _googleAccountService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            BpDbContext dbContext,
            ISessionService sessionService,
            IGoogleAccountService googleAccountService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _dbContext = dbContext;
            _sessionService = sessionService;
            _googleAccountService = googleAccountService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>Starts Google sign-in and returns to the configured portal after authentication.</summary>
        [HttpGet("google")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Google(
            [FromQuery] string? returnUrl,
            [FromServices] IAuthenticationSchemeProvider schemes,
            CancellationToken cancellationToken)
        {
            return await StartGoogleAsync(returnUrl, schemes, false, cancellationToken);
        }

        [HttpGet("google/register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GoogleRegister(
            [FromQuery] string? returnUrl,
            [FromServices] IAuthenticationSchemeProvider schemes,
            CancellationToken cancellationToken)
        {
            return await StartGoogleAsync(returnUrl, schemes, true, cancellationToken);
        }

        /// <summary>Completes Google sign-in using the temporary, verified external authentication ticket.</summary>
        [HttpGet("google/callback")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GoogleCallback(CancellationToken cancellationToken)
        {
            var external = await HttpContext.AuthenticateAsync("GoogleExternal");
            await HttpContext.SignOutAsync("GoogleExternal");
            var email = external.Principal?.FindFirstValue(ClaimTypes.Email);
            if (!external.Succeeded
                || !bool.TryParse(external.Principal?.FindFirstValue("google:email_verified"), out var verified)
                || !verified
                || string.IsNullOrWhiteSpace(email)
                || email.Length > 200
                || external.Properties == null
                || !external.Properties.Items.TryGetValue("returnUrl", out var returnUrl)
                || !TryGetGoogleReturnUrl(returnUrl, out var destination))
            {
                _logger.LogWarning("Google sign-in rejected an invalid external authentication ticket.");
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Google sign-in could not be verified");
            }

            if (external.Properties.Items.TryGetValue("googleFlow", out var flow) && flow == "register")
            {
                var created = await _googleAccountService.RegisterAsync(
                    email,
                    external.Principal?.FindFirstValue(ClaimTypes.GivenName),
                    external.Principal?.FindFirstValue(ClaimTypes.Surname),
                    cancellationToken);
                if (!created)
                {
                    return GoogleLoginRedirect("account-exists", destination);
                }

                TryGetGoogleReturnUrl("/sign-up-confirmation", out var confirmationUrl);
                return Redirect(confirmationUrl);
            }

            var (user, accountExists) = await _googleAccountService.FindExistingAsync(email, cancellationToken);
            if (!accountExists)
            {
                return GoogleLoginRedirect("account-required", destination);
            }

            if (user == null)
            {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "This account cannot sign in");
            }

            await SignInUserAsync(user, cancellationToken);
            return Redirect(destination);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validate user against database
                var user = await _dbContext.BpUsers.FirstOrDefaultAsync(u => u.UserName == request.Username && u.IsActive);

                if (user == null)
                {
                    return Unauthorized(new { success = false, message = "Invalid credentials" });
                }

                if (string.IsNullOrEmpty(user.Password) || !PasswordHasher.VerifyPassword(request.Password, user.Password))
                {
                    return Unauthorized(new { success = false, message = "Invalid credentials" });
                }

                await SignInUserAsync(user, HttpContext.RequestAborted);

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    user = new { username = user.UserName, fullName = user.FirstName + " " + user.Surname },
                });
            }
            catch (Exception)
            {
                // _logger.LogError(ex, "Error during login for user {Username}", request.Username);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }

            /*if (_dbContext is null)
            {
                return StatusCode(500, new { success = false, message = "Database context is not available." });
            }*/

            /* var user = await _dbContext.BpUsers.FirstOrDefaultAsync(u => u.UserName == request.Username && u.IsActive);
             //// In a real application, you would validate against a database
             //// if (request.Username == "demo@demo" && request.Password == "password")
             if (user != null && !string.IsNullOrEmpty(user.Password) && PasswordHasher.VerifyPassword(request.Password, user.Password))
             {
                 var claims = new List<Claim>
                 {
                     new Claim(ClaimTypes.Name, request.Username),
                     new Claim(ClaimTypes.NameIdentifier, "1"),
                     new Claim("FullName", "Demo User"),
                     new Claim("SessionId", Guid.NewGuid().ToString()),
                     ////TODO: KSY remove it
                     new Claim("Permission", "AdminAccess"), // <- add this
                 };

                 var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                 var principal = new ClaimsPrincipal(claimsIdentity);

                 var authProps = new AuthenticationProperties
                 {
                     IsPersistent = true,
                     ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7), // match ExpireTimeSpan
                     AllowRefresh = true,
                 };

                 await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

                 return Ok(new
                 {
                     success = true,
                     message = "Login successful",
                     user = new { username = request.Username, fullName = "Demo User" },
                 });
             }

             return Unauthorized(new { success = false, message = "Invalid credentials" });*/
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Get the session ID from the cookie
                var sessionId = HttpContext.Request.Cookies["SessionId"];

                // Revoke the session if it exists
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await _sessionService.RevokeSessionAsync(sessionId);
                }

                // Delete the SessionId cookie by setting it to expire
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = HttpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(-1),
                };

                HttpContext.Response.Cookies.Append("SessionId", string.Empty, cookieOptions);
                await HttpContext.SignOutAsync("CookieAuth");

                return Ok(new { success = true, message = "Logout successful" });
            }
            catch (Exception)
            {
                return Ok(new { success = true, message = "Logout successful" });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var username = User.Identity?.Name;
            var fullName = User.FindFirst("FullName")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Ok(new
            {
                username = username,
                fullName = fullName,
                userId = userId,
                isAuthenticated = true,
            });
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckAuth()
        {
            var sessionId = HttpContext.Request.Cookies["SessionId"];

            var session = string.IsNullOrEmpty(sessionId) ? null : await _sessionService.GetSessionAsync(sessionId);

            if (session == null)
            {
                var cookieAuth = await HttpContext.AuthenticateAsync("CookieAuth");
                if (!cookieAuth.Succeeded || cookieAuth.Principal == null)
                {
                    return Ok(new { isAuthenticated = false });
                }

                var username = cookieAuth.Principal.Identity?.Name;
                return Ok(new
                {
                    isAuthenticated = true,
                    username = username,
                    sessionValid = false,
                    cookieValid = true,
                });
            }

            var sessionClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                new Claim(ClaimTypes.Name, session.Username),
                new Claim("FullName", session.FullName),
            };

            foreach (var permission in session.Permissions ?? [])
            {
                sessionClaims.Add(new Claim("Permission", permission));
            }

            var sessionIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var sessionPrincipal = new ClaimsPrincipal(sessionIdentity);
            var sessionAuthProps = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                AllowRefresh = true,
            };
            await HttpContext.SignInAsync("CookieAuth", sessionPrincipal, sessionAuthProps);

            return Ok(new
            {
                isAuthenticated = true,
                username = session.Username,
                sessionValid = true,
            });
        }

        private bool TryGetGoogleReturnUrl(string? returnUrl, out string destination)
        {
            destination = string.Empty;
            if (!Uri.TryCreate(_configuration["Authentication:Google:FrontendOrigin"], UriKind.Absolute, out var origin)
                || (origin.Scheme != Uri.UriSchemeHttps && origin.Scheme != Uri.UriSchemeHttp)
                || !string.IsNullOrEmpty(origin.UserInfo)
                || origin.AbsolutePath != "/"
                || !string.IsNullOrEmpty(origin.Query)
                || !string.IsNullOrEmpty(origin.Fragment)
                || string.IsNullOrWhiteSpace(returnUrl))
            {
                return false;
            }

            if (returnUrl.StartsWith('/') && !Url.IsLocalUrl(returnUrl))
            {
                return false;
            }

            if (!Uri.TryCreate(origin, returnUrl, out var target)
                || target.GetLeftPart(UriPartial.Authority) != origin.GetLeftPart(UriPartial.Authority)
                || !string.IsNullOrEmpty(target.UserInfo))
            {
                return false;
            }

            destination = target.AbsoluteUri;
            return true;
        }

        private async Task<IActionResult> StartGoogleAsync(
            string? returnUrl,
            IAuthenticationSchemeProvider schemes,
            bool register,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await schemes.GetSchemeAsync("Google") == null || !TryGetGoogleReturnUrl("/", out _))
            {
                _logger.LogWarning("Google sign-in is not configured.");
                return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Google sign-in is not configured");
            }

            if (!TryGetGoogleReturnUrl(returnUrl ?? "/", out var destination))
            {
                _logger.LogWarning("Google sign-in rejected an invalid return URL.");
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid return URL");
            }

            await HttpContext.SignOutAsync("GoogleExternal");
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback)),
            };
            properties.Items["returnUrl"] = destination;
            properties.Items["googleFlow"] = register ? "register" : "login";
            return Challenge(properties, "Google");
        }

        private IActionResult GoogleLoginRedirect(string error, string destination)
        {
            TryGetGoogleReturnUrl("/login", out var loginUrl);
            return Redirect(QueryHelpers.AddQueryString(loginUrl, new Dictionary<string, string?>
            {
                ["googleError"] = error,
                ["returnUrl"] = new Uri(destination).PathAndQuery + new Uri(destination).Fragment,
            }));
        }

        private async Task SignInUserAsync(BpUser user, CancellationToken cancellationToken)
        {
            var sessionId = await _sessionService.CreateSessionAsync(
                user.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                HttpContext.Request.Headers.UserAgent.ToString(),
                cancellationToken);
            var permissions = await _sessionService.GetUserPermissionsAsync(user.Id, cancellationToken);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FullName", user.FirstName + " " + user.Surname),
            };
            claims.AddRange(permissions.Select(permission => new Claim("Permission", permission)));

            await HttpContext.SignInAsync(
                "CookieAuth",
                new ClaimsPrincipal(new ClaimsIdentity(claims, "CookieAuth")),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    AllowRefresh = true,
                });
            HttpContext.Response.Cookies.Append("SessionId", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
            });
        }

        /* [HttpGet("check")]
         public async Task<IActionResult> CheckAuth()
         {
             var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
             if (!result.Succeeded || result.Principal is null)
             {
                 return Unauthorized(new { authenticated = false });
             }

             var p = result.Principal;
             return Ok(new
             {
                 isAuthenticated = true,
                 authenticated = true,
                 name = p.Identity?.Name,
                 userId = p.FindFirst(ClaimTypes.NameIdentifier)?.Value,
             });
         }*/

        /* [HttpGet("auth-status")]
         [AllowAnonymous]
         public async Task<IActionResult> AuthStatus()
         {
             // Ask the authentication system to validate the cookie
             var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

             if (!result.Succeeded || result.Principal is null)
             {
                 return Unauthorized(new
                 {
                     authenticated = false,
                 });
             }

             var principal = result.Principal;

             return Ok(new
             {
                 authenticated = true,
                 authScheme = result.Ticket?.AuthenticationScheme,
                 name = principal.Identity?.Name,
                 userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                 fullName = principal.FindFirst("FullName")?.Value,
                 claims = principal.Claims.Select(c => new { c.Type, c.Value }),
             });
         }*/
    }
}