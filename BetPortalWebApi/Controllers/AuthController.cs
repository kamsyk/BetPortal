using System.Security.Claims;
using BetPortalWebApi.ErrorHandling;
using BetPortalWebApi.Services;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.Models;
using BpDbLayer.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GoogleCallback(CancellationToken cancellationToken)
        {
            return await CompleteExternalAsync("Google", _googleAccountService, cancellationToken);
        }

        [HttpGet("facebook")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> FacebookCallback(CancellationToken cancellationToken)
        {
            return await CompleteExternalAsync("Facebook", _facebookAccountService, cancellationToken);
        }

        /// <summary>Starts Microsoft sign-in for personal and work/school accounts.</summary>
        [HttpGet("microsoft")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password login.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Sign-in is temporarily unavailable. Please try again later.",
                    errorId = ErrorReference.Get(HttpContext),
                });
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Session cleanup failed during logout.");
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
            string? destination = null;
            if (external.Properties?.Items.TryGetValue("returnUrl", out var returnUrl) == true
                && TryGetExternalReturnUrl(provider, returnUrl, out var validatedReturnUrl))
            {
                destination = validatedReturnUrl;
            }

            if (!external.Succeeded
                || destination == null
                || string.IsNullOrWhiteSpace(external.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)))
            {
                _logger.LogWarning("{Provider} sign-in rejected an invalid or expired external authentication ticket.", provider);
                return ExternalErrorRedirect(provider, ExternalAuthErrorCodes.SignInFailed, destination);
            }

            var email = external.Principal?.FindFirstValue(ClaimTypes.Email);
            if ((provider == "Google"
                    && (!bool.TryParse(external.Principal?.FindFirstValue("google:email_verified"), out var verified) || !verified))
                || string.IsNullOrWhiteSpace(email)
                || email.Length > 150)
            {
                _logger.LogWarning("{Provider} sign-in rejected because the provider did not supply a usable verified email address.", provider);
                return ExternalErrorRedirect(provider, ExternalAuthErrorCodes.EmailUnavailable, destination);
            }

            if (external.Properties!.Items.TryGetValue($"{provider.ToLowerInvariant()}Flow", out var flow) && flow == "register")
            {
                var (created, emailSent) = await accountService.RegisterAsync(
                    email,
                    external.Principal?.FindFirstValue(ClaimTypes.GivenName),
                    external.Principal?.FindFirstValue(ClaimTypes.Surname),
                    cancellationToken);
                if (!created)
                {
                    return ExternalErrorRedirect(provider, ExternalAuthErrorCodes.AccountExists, destination, includeErrorId: false);
                }

                if (!emailSent)
                {
                    return ExternalErrorRedirect(provider, ExternalAuthErrorCodes.ActivationEmailFailed, destination);
                }

                TryGetExternalReturnUrl(provider, "/sign-up-confirmation", out var confirmationUrl);
                return Redirect(confirmationUrl);
            }

            var (user, status) = await accountService.FindExistingAsync(email, cancellationToken);
            if (user == null || status != ExternalAccountStatus.Found)
            {
                return status switch
                {
                    ExternalAccountStatus.NotFound => ExternalErrorRedirect(
                        provider, ExternalAuthErrorCodes.AccountRequired, destination, includeErrorId: false),
                    ExternalAccountStatus.NotLinked => ExternalErrorRedirect(provider, ExternalAuthErrorCodes.AccountNotLinked, destination),
                    ExternalAccountStatus.Inactive => ExternalErrorRedirect(provider, ExternalAuthErrorCodes.AccountInactive, destination),
                    _ => ExternalErrorRedirect(provider, ExternalAuthErrorCodes.AccountUnavailable, destination),
                };
            }

            await SignInUserAsync(user, cancellationToken);
            return Redirect(destination);
        }

        private bool TryGetExternalReturnUrl(string provider, string? returnUrl, out string destination)
        {
            destination = string.Empty;
            if (!ExternalAuthErrorRedirect.TryGetFrontendOrigin(_configuration, provider, out var origin)
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
                return ExternalErrorRedirect(provider, ExternalAuthErrorCodes.NotConfigured, null);
            }

            if (!TryGetExternalReturnUrl(provider, returnUrl ?? "/", out var destination))
            {
                _logger.LogWarning("{Provider} sign-in rejected an invalid return URL.", provider);
                return ExternalErrorRedirect(provider, ExternalAuthErrorCodes.SignInFailed, null);
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

        /// <summary>
        /// Returns the user to the portal login dialog with an error code. External sign-in is a browser navigation, so a
        /// JSON response would be shown to the user as raw text. Problem Details are used only when no portal is configured.
        /// </summary>
        private IActionResult ExternalErrorRedirect(string provider, string errorCode, string? destination, bool includeErrorId = true)
        {
            var errorId = ErrorReference.Get(HttpContext);
            if (ExternalAuthErrorRedirect.TryCreate(
                _configuration, provider, errorCode, includeErrorId ? errorId : null, destination, out var url))
            {
                return Redirect(url);
            }

            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: $"{provider} sign-in is currently unavailable",
                detail: "Please try again later or use another sign-in method.");
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