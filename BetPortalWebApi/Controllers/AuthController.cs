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

            (string token, DateTime expiresAt) = JwtToken.CreateJwt(user);
            return Ok(new AuthResponse
            {
                AccessToken = token,
                ExpiresAt = expiresAt,
                TokenType = "Bearer",
            });
        }
    }
}