using System.Security.Claims;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
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

        public AuthController(BpDbContext dbContext, ISessionService sessionService)
        {
            _dbContext = dbContext;
            _sessionService = sessionService;
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

                // Get client IP and User Agent
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

                // Create session
                var sessionId = await _sessionService.CreateSessionAsync(user.Id, ipAddress, userAgent);

                // Set session cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = HttpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                };

                HttpContext.Response.Cookies.Append("SessionId", sessionId, cookieOptions);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim("FullName", user.FirstName + " " + user.Surname),
                    new Claim("Permission", "AdminAccess"),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(claimsIdentity);

                var authProps = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    AllowRefresh = true,
                };

                await HttpContext.SignInAsync("CookieAuth", principal, authProps);

                ////_logger.LogInformation("User {Username} logged in successfully with session {SessionId}", user.UserName, sessionId);

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
                // Log the error but still return success to prevent information disclosure
                // _logger.LogError(ex, "Error during logout");
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

            if (string.IsNullOrEmpty(sessionId))
            {
                return Ok(new { isAuthenticated = false });
            }

            var session = await _sessionService.GetSessionAsync(sessionId);

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