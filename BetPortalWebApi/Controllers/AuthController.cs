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
            Console.WriteLine("=== COOKIE AUTHENTICATION DEBUG ===");
            Console.WriteLine($"Request Scheme: {Request.Scheme}");
            Console.WriteLine($"Request Host: {Request.Host}");
            Console.WriteLine($"Request Path: {Request.Path}");

            // Log all cookies
            Console.WriteLine("All Cookies:");
            foreach (var cookie in Request.Cookies)
            {
                Console.WriteLine($"  {cookie.Key}: {cookie.Value?.Substring(0, Math.Min(50, cookie.Value.Length))}...");
            }

            // Log all headers
            Console.WriteLine("Cookie Header:");
            if (Request.Headers.ContainsKey("Cookie"))
            {
                var cookieHeader = Request.Headers["Cookie"].ToString();
                Console.WriteLine($"  Raw: {cookieHeader?.Substring(0, Math.Min(100, cookieHeader.Length))}...");
            }
            else
            {
                Console.WriteLine("  No Cookie header found");
            }

            // Try authentication and log detailed result
            Console.WriteLine("\n=== AUTHENTICATION ATTEMPT ===");
            var result1 = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            Console.WriteLine($"Authentication Succeeded: {result1.Succeeded}");
            if (result1.Failure != null)
            {
                Console.WriteLine($"Authentication Failure: {result1.Failure.Message}");
                Console.WriteLine($"Failure StackTrace: {result1.Failure.StackTrace}");
            }

            /* Request.Cookies.TryGetValue("AuthToken", out string? authToken);
             if (!string.IsNullOrEmpty(authToken))
             {
                 //// TODO: Implement your token validation logic here
                 //// For example, lookup in database, validate JWT, etc.
                 //var isValid = await ValidateAuthToken(authToken);

                 ////if (isValid)
                 {
                     // Token is valid - you can extract user info from it
                     var userId = ExtractUserIdFromToken(authToken);
                     var userName = ExtractUserNameFromToken(authToken);

                     return Ok(new
                     {
                         isAuthenticated = true,
                         authenticated = true,
                         userId = userId,
                         name = userName,
                     });
                 }
             }*/


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

            /*return Ok(new
            {
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                username = User.Identity?.Name,
            });*/

            /*if (User.Identity?.IsAuthenticated ?? false)
            {
                return Ok(new
                {
                    isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    username = User.Identity?.Name,
                });
            }
            else
            {
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

        [HttpGet("auth-status")]
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
        }

    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}