using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly JwtOptions _jwt;

    public AuthController(IUserService users, IOptions<JwtOptions> jwtOptions)
    {
        _users = users;
        _jwt = jwtOptions.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _users.ValidateCredentialsAsync(request.Email, request.Password);
        if (user is null) return Unauthorized();

        var token = CreateJwt(user);
        return Ok(new AuthResponse
        {
            AccessToken = token.Token,
            ExpiresAt = token.ExpiresAt,
            TokenType = "Bearer"
        });
    }

    private (string Token, DateTime ExpiresAt) CreateJwt(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.UserName)
        };
        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpireMinutes);

        var jwt = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}

public sealed class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
}

// Demo user model and service — replace with your real user/identity store.
public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public interface IUserService
{
    Task<AppUser?> ValidateCredentialsAsync(string email, string password);
}

public sealed class InMemoryUserService : IUserService
{
    // demo user: email test@local, password Pass@123
    private static readonly AppUser Demo = new AppUser
    {
        UserName = "test",
        Email = "test@local",
        Roles = new[] { "User" }
    };

    public Task<AppUser?> ValidateCredentialsAsync(string email, string password)
    {
        if (email.Equals("test@local", StringComparison.OrdinalIgnoreCase) && password == "Pass@123")
            return Task.FromResult<AppUser?>(Demo);
        return Task.FromResult<AppUser?>(null);
    }
}