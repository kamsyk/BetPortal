using BetPortalWebApi.Contracts.User;
using BetPortalWebApi.Controllers;
using BetPortalWebApi.Services.Interfaces;
using KsAuthentication.Contracts;
using KsAuthentication.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BetPortalWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController() : BpBaseController
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // In a real application, you would validate against a database
            if (request.Username == "demo@demo" && request.Password == "password")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, request.Username),
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim("FullName", "Demo User"),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

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
        public IActionResult CheckAuth()
        {
            return Ok(new
            {
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                username = User.Identity?.Name,
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}