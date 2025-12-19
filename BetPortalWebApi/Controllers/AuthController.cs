using System.Security.Claims;
using BpDbLayer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController() : BpBaseController
    {
        private readonly BpDbContext? _dbContext;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _dbContext?.BpUsers
                    .FirstOrDefaultAsync(u => u.UserName == request.Username && u.IsActive);
            // In a real application, you would validate against a database
            if (request.Username == "demo@demo" && request.Password == "password")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, request.Username),
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim("FullName", "Demo User"),
                    new Claim("SessionId", Guid.NewGuid().ToString()),
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

            return Unauthorized(new { success = false, message = "Invalid credentials" });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { success = true, message = "Logout successful" });
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
        }

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

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}