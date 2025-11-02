using BetPortalWebApi.Contracts.User;
using BetPortalWebApi.Controllers;
using BetPortalWebApi.Services.Interfaces;
using KsAuthentication.Contracts;
using KsAuthentication.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BetPortalWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IUserService users, IOptions<JwtOptions> jwtOptions) : BpBaseController
    {
        private readonly IUserService _users = users;
        private readonly JwtOptions _jwt = jwtOptions.Value;

        private JwtToken JwtToken => new (_jwt);

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequestDto request)
        {
            var user = await _users.ValidateCredentialsAsync(request.Email, request.Password);
            if (user is null)
            {
                return Unauthorized();
            }

            // Set secure HttpOnly cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,                    // No JavaScript access
                Secure = false,           // HTTPS only (auto-detect)
                SameSite = SameSiteMode.None,     // CSRF protection
                MaxAge = TimeSpan.FromDays(7),      // Cookie expiry
                Path = "/",                         // Available to entire app
                                                    //// IsEssential = true,                  // GDPR compliance
            };

            (string token, DateTime expiresAt) = JwtToken.CreateJwt(user);

            Response.Cookies.Append("auth_token", token, cookieOptions);

            return Ok(new AuthResponse
            {
                AccessToken = token,
                ExpiresAt = expiresAt,
                TokenType = "Bearer",
            });
        }

        //// TODO: KSY - temp code for testing cookie retrieval
        [HttpGet("session-from-cookie")]
        [AllowAnonymous]
        public IActionResult SessionFromCookie([FromQuery] string name = "auth_token")
        {
            if (Request.Cookies.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                // Use the cookie value in your logic (e.g., lookup session, etc.)
                return Ok(new { success = true, name, value });
            }

            return Unauthorized(new { success = false, message = $"Cookie '{name}' not found or empty." });
        }

        // Read the Authorization header directly
        [HttpGet("header-token")]
        [AllowAnonymous]
        public IActionResult ReadAuthorizationHeader([FromHeader(Name = "Authorization")] string? authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization))
            {
                return Unauthorized(new { message = "Missing Authorization header." });
            }

            const string bearer = "Bearer ";
            if (authorization.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
            {
                var token = authorization[bearer.Length..].Trim();
                return Ok(new { scheme = "Bearer", token });
            }

            return BadRequest(new { message = "Unsupported Authorization scheme." });
        }
    }
}