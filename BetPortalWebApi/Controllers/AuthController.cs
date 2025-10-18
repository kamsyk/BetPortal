using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BetPortalWebApi.Contracts.User;
using BetPortalWebApi.Controllers;
using BetPortalWebApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BpBaseController
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
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequestDto request)
    {
        var user = await _users.ValidateCredentialsAsync(request.Email, request.Password);
        if (user is null)
        {
            return Unauthorized();
        }

        var token = CreateJwt(user);
        return Ok(new AuthResponse
        {
            AccessToken = token.Token,
            ExpiresAt = token.ExpiresAt,
            TokenType = "Bearer",
        });
    }

    private (string Token, DateTime ExpiresAt) CreateJwt(BpUserDto user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.UserName),
        };
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

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