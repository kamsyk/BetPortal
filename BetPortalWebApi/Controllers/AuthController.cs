using System.Security.Claims;
using BetPortalWebApi.ErrorHandling;
using BetPortalWebApi.Services;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.Models;
using BpDbLayer.Utils;
using Microsoft.AspNetCore.Authentication;
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
        private readonly IExternalAuthErrorDetailsService _externalAuthErrorDetailsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            BpDbContext dbContext,
            ISessionService sessionService,
            IGoogleAccountService googleAccountService,
            IFacebookAccountService facebookAccountService,
            IMicrosoftAccountService microsoftAccountService,
            IExternalAuthErrorDetailsService externalAuthErrorDetailsService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _dbContext = dbContext;
            _sessionService = sessionService;
            _googleAccountService = googleAccountService;
            _facebookAccountService = facebookAccountService;
            _microsoftAccountService = microsoftAccountService;
            _externalAuthErrorDetailsService = externalAuthErrorDetailsService;
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
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var sessionId = User.FindFirstValue("SessionId");
            if (!string.IsNullOrEmpty(sessionId))
            {
                await _sessionService.RevokeSessionAsync(sessionId, HttpContext.RequestAborted);
            }

            HttpContext.Response.Cookies.Delete("SessionId", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
            });

            return Ok(new { success = true, message = "Logout successful" });
        }

        /// <summary>Returns the masked email and available sign-in methods for an <c>account-not-linked</c> error token.</summary>
        [HttpGet("external-error-details")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ExternalAuthErrorDetails), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult ExternalErrorDetails([FromQuery] string? token)
        {
            Response.Headers.CacheControl = "no-store";
            var details = _externalAuthErrorDetailsService.Read(token);
            if (details == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "The sign-in details are no longer available",
                    detail: "Please try signing in again.");
            }

            return Ok(details);
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
        public IActionResult CheckAuth()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            return Ok(new
            {
                isAuthenticated,
                username = isAuthenticated ? User.Identity?.Name : null,
                sessionValid = isAuthenticated,
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
            if (status == ExternalAccountStatus.NotLinked)
            {
                var errorDetails = await _externalAuthErrorDetailsService.CreateTokenAsync(email, cancellationToken);
                return ExternalErrorRedirect(provider, ExternalAuthErrorCodes.AccountNotLinked, destination, errorDetails: errorDetails);
            }

            if (user == null || status != ExternalAccountStatus.Found)
            {
                return status switch
                {
                    ExternalAccountStatus.NotFound => ExternalErrorRedirect(
                        provider, ExternalAuthErrorCodes.AccountRequired, destination, includeErrorId: false),
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
        private IActionResult ExternalErrorRedirect(
            string provider, string errorCode, string? destination, bool includeErrorId = true, string? errorDetails = null)
        {
            var errorId = ErrorReference.Get(HttpContext);
            if (ExternalAuthErrorRedirect.TryCreate(
                _configuration, provider, errorCode, includeErrorId ? errorId : null, errorDetails, destination, out var url))
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
            HttpContext.Response.Cookies.Append("SessionId", sessionId, new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.Add(SessionService.SessionLifetime),
            });
        }
    }
}