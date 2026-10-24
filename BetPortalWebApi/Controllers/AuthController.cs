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
        private readonly IFacebookAccountService _facebookAccountService;
        private readonly IMicrosoftAccountService _microsoftAccountService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            BpDbContext dbContext,
            ISessionService sessionService,
            IGoogleAccountService googleAccountService,
            IFacebookAccountService facebookAccountService,
            IMicrosoftAccountService microsoftAccountService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _dbContext = dbContext;
            _sessionService = sessionService;
            _googleAccountService = googleAccountService;
            _facebookAccountService = facebookAccountService;
            _microsoftAccountService = microsoftAccountService;
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
            return await StartExternalAsync("Google", returnUrl, schemes, false, cancellationToken);
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
            return await StartExternalAsync("Google", returnUrl, schemes, true, cancellationToken);
        }

        /// <summary>Completes Google sign-in using the temporary, verified external authentication ticket.</summary>
        [HttpGet("google/callback")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GoogleCallback(CancellationToken cancellationToken)
        {
            return await CompleteExternalAsync("Google", _googleAccountService, cancellationToken);
        }

        [HttpGet("facebook")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Facebook(
            [FromQuery] string? returnUrl,
            [FromServices] IAuthenticationSchemeProvider schemes,
            CancellationToken cancellationToken)
        {
            return await StartExternalAsync("Facebook", returnUrl, schemes, false, cancellationToken);
        }

        [HttpGet("facebook/register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> FacebookRegister(
            [FromQuery] string? returnUrl,
            [FromServices] IAuthenticationSchemeProvider schemes,
            CancellationToken cancellationToken)
        {
            return await StartExternalAsync("Facebook", returnUrl, schemes, true, cancellationToken);
        }

        [HttpGet("facebook/callback")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> FacebookCallback(CancellationToken cancellationToken)
        {
            return await CompleteExternalAsync("Facebook", _facebookAccountService, cancellationToken);
        }

        /// <summary>Starts Microsoft sign-in for personal and work/school accounts.</summary>
        [HttpGet("microsoft")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Microsoft(
            [FromQuery] string? returnUrl,
            [FromServices] IAuthenticationSchemeProvider schemes,
            CancellationToken cancellationToken)
        {
            return await StartExternalAsync("Microsoft", returnUrl, schemes, false, cancellationToken);
        }

        /// <summary>Starts Microsoft registration; accounts require email activation before login.</summary>
        [HttpGet("microsoft/register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> MicrosoftRegister(
            [FromQuery] string? returnUrl,
            [FromServices] IAuthenticationSchemeProvider schemes,
            CancellationToken cancellationToken)
        {
            return await StartExternalAsync("Microsoft", returnUrl, schemes, true, cancellationToken);
        }

        /// <summary>Completes Microsoft login or registration using the protected external ticket.</summary>
        [HttpGet("microsoft/callback")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> MicrosoftCallback(CancellationToken cancellationToken)
        {
            return await CompleteExternalAsync("Microsoft", _microsoftAccountService, cancellationToken);
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

        private async Task<IActionResult> CompleteExternalAsync(
            string provider, IExternalAccountService accountService, CancellationToken cancellationToken)
        {
            var external = await HttpContext.AuthenticateAsync($"{provider}External");
            await HttpContext.SignOutAsync($"{provider}External");
            var email = external.Principal?.FindFirstValue(ClaimTypes.Email);
            if (!external.Succeeded
                || (provider == "Google"
                    && (!bool.TryParse(external.Principal?.FindFirstValue("google:email_verified"), out var verified) || !verified))
                || string.IsNullOrWhiteSpace(external.Principal?.FindFirstValue(ClaimTypes.NameIdentifier))
                || string.IsNullOrWhiteSpace(email)
                || email.Length > 150
                || external.Properties == null
                || !external.Properties.Items.TryGetValue("returnUrl", out var returnUrl)
                || !TryGetExternalReturnUrl(provider, returnUrl, out var destination))
            {
                _logger.LogWarning("{Provider} sign-in rejected an invalid external authentication ticket or missing email.", provider);
                var detail = provider switch
                {
                    "Facebook" => "Sign in again and allow access to your Facebook email. An email address is required.",
                    "Microsoft" => "Sign in with a Microsoft account that supplies a mailbox email address and allow access to your profile.",
                    _ => null,
                };
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: $"{provider} sign-in could not be verified",
                    detail: detail);
            }

            if (external.Properties.Items.TryGetValue($"{provider.ToLowerInvariant()}Flow", out var flow) && flow == "register")
            {
                var (created, emailSent) = await accountService.RegisterAsync(
                    email,
                    external.Principal?.FindFirstValue(ClaimTypes.GivenName),
                    external.Principal?.FindFirstValue(ClaimTypes.Surname),
                    cancellationToken);
                if (!created)
                {
                    return ExternalLoginRedirect(provider, "account-exists", destination);
                }

                if (!emailSent)
                {
                    return Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "Your account was registered, but the welcome email could not be sent",
                        detail: "Your account is inactive. Please contact support to receive an activation link.");
                }

                TryGetExternalReturnUrl(provider, "/sign-up-confirmation", out var confirmationUrl);
                return Redirect(confirmationUrl);
            }

            var (user, accountExists) = await accountService.FindExistingAsync(email, cancellationToken);
            if (!accountExists)
            {
                return ExternalLoginRedirect(provider, "account-required", destination);
            }

            if (user == null)
            {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "This account cannot sign in");
            }

            await SignInUserAsync(user, cancellationToken);
            return Redirect(destination);
        }

        private bool TryGetExternalReturnUrl(string provider, string? returnUrl, out string destination)
        {
            destination = string.Empty;
            if (!Uri.TryCreate(_configuration[$"Authentication:{provider}:FrontendOrigin"], UriKind.Absolute, out var origin)
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

        private async Task<IActionResult> StartExternalAsync(
            string provider,
            string? returnUrl,
            IAuthenticationSchemeProvider schemes,
            bool register,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await schemes.GetSchemeAsync(provider) == null || !TryGetExternalReturnUrl(provider, "/", out _))
            {
                _logger.LogWarning("{Provider} sign-in is not configured.", provider);
                return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: $"{provider} sign-in is not configured");
            }

            if (!TryGetExternalReturnUrl(provider, returnUrl ?? "/", out var destination))
            {
                _logger.LogWarning("{Provider} sign-in rejected an invalid return URL.", provider);
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid return URL");
            }

            await HttpContext.SignOutAsync($"{provider}External");
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action($"{provider}Callback"),
            };
            properties.Items["returnUrl"] = destination;
            properties.Items[$"{provider.ToLowerInvariant()}Flow"] = register ? "register" : "login";
            return Challenge(properties, provider);
        }

        private IActionResult ExternalLoginRedirect(string provider, string error, string destination)
        {
            TryGetExternalReturnUrl(provider, "/login", out var loginUrl);
            return Redirect(QueryHelpers.AddQueryString(loginUrl, new Dictionary<string, string?>
            {
                [$"{provider.ToLowerInvariant()}Error"] = error,
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