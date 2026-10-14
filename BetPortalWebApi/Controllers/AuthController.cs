using BetPortalWebApi.Controllers;
using BetPortalWebApi.Services.Interfaces;
using BetPortalWebApi.Services.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BpBaseController
{
    private const string SessionCookieName = "session_id";

    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

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

        var sessionId = await _sessionService.CreateSessionAsync(user.Id, SessionLifetime);
        Response.Cookies.Append(SessionCookieName, sessionId, CreateSessionCookieOptions());

        return Ok(new
        {
            success = true,
            user = new { user.Id, user.Email, user.UserName },
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var session = await GetActiveSessionAsync();
        if (session == null)
        {
            return Unauthorized();
        }

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
        if (Request.Cookies.TryGetValue(SessionCookieName, out var sessionId) && !string.IsNullOrEmpty(sessionId))
        {
            await _sessionService.DeleteSessionAsync(sessionId);
        }

        Response.Cookies.Delete(SessionCookieName, CreateSessionCookieOptions());

        return Ok(new { success = true });
    }

    [HttpGet("validate-session")]
    public async Task<IActionResult> ValidateSession()
    {
        var session = await GetActiveSessionAsync();
        if (session == null)
        {
            return Ok(new { success = false, user = (object?)null });
        }

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

    private static CookieOptions CreateSessionCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = SessionLifetime,
            Path = "/",
        };
    }

    private async Task<SessionData?> GetActiveSessionAsync()
    {
        if (!Request.Cookies.TryGetValue(SessionCookieName, out var sessionId) || string.IsNullOrEmpty(sessionId))
        {
            return null;
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null || session.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        await _sessionService.RefreshSessionAsync(sessionId, SessionLifetime);
        return session;
    }
}
