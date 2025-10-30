using BetPortalWebApi.Controllers;
using BetPortalWebApi.Services;
using BetPortalWebApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BpBaseController
{
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;

    public AuthController(IAuthService authService, ISessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.ValidateCredentialsAsync(request.Email, request.Password);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Create server session (NOT a JWT)
        var sessionId = await _sessionService.CreateSessionAsync(user.Id, TimeSpan.FromDays(7));

        // Set secure HttpOnly cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,                    // No JavaScript access
            Secure = Request.IsHttps,           // HTTPS only (auto-detect)
            SameSite = SameSiteMode.Strict,     // CSRF protection
            MaxAge = TimeSpan.FromDays(7),      // Cookie expiry
            Path = "/",                         // Available to entire app
            IsEssential = true,                  // GDPR compliance
        };

        Response.Cookies.Append("session_id", sessionId, cookieOptions);

        return Ok(new
        {
            success = true,
            user = new { user.Id, user.Email, user.UserName },
            //// Never return session ID in response body
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var sessionId = Request.Cookies["session_id"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null || session.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        // Refresh session on each valid request
        await _sessionService.RefreshSessionAsync(sessionId, TimeSpan.FromDays(7));

        return Ok(new
        {
            success = true,
            user = new
            {
                Id = session.UserId,
                Email = session.Email,
                Name = session.Name,
            },
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionId = Request.Cookies["session_id"];
        if (!string.IsNullOrEmpty(sessionId))
        {
            await _sessionService.DeleteSessionAsync(sessionId);
        }

        // Clear cookie
        Response.Cookies.Delete("session_id", new CookieOptions
        {
            Path = "/",
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
        });

        return Ok(new { success = true });
    }

    [HttpGet("validate-session")]
    public async Task<IActionResult> ValidateSession()
    {
        try
        {
            // Check if user is authenticated via cookie/JWT
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return Ok(new { success = false, user = (object?)null });
            }

            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Ok(new { success = false, user = (object?)null });
            }

            // Fetch user details from database
            ////var user = await _userService.GetUserByIdAsync(userIdClaim);
            var session = await _sessionService.GetSessionAsync(userIdClaim);
            if (session == null)
            {
                return Ok(new { success = false, user = (object?)null });
            }

            // Return success with user data
            return Ok(new
            {
                success = true,
                user = new
                {
                    id = session.UserId,
                    email = session.Email,
                    name = session.Name,
                },
            });
        }
        catch (Exception ex)
        {
           //// _logger.LogError(ex, "Error validating session");
            return Ok(new { success = false, user = (object?)null });
        }
    }
}