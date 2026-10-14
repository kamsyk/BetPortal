using System.Text.Json;
using BetPortalWebApi.Services;
using BetPortalWebApi.Services.Interfaces;
using KsAuthentication.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BetPortalWebApi.Tests
{
    public class SessionAuthenticationTests
    {
        [Fact]
        public async Task SessionsAreIsolatedById()
        {
            var sessions = new RedisSessionService(new FakeUsers());
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();

            var sessionA = await sessions.CreateSessionAsync(userA, TimeSpan.FromHours(1));
            var sessionB = await sessions.CreateSessionAsync(userB, TimeSpan.FromHours(1));

            var loadedA = await sessions.GetSessionAsync(sessionA);
            var loadedB = await sessions.GetSessionAsync(sessionB);

            Assert.NotNull(loadedA);
            Assert.NotNull(loadedB);
            Assert.Equal(userA, loadedA.UserId);
            Assert.Equal(userB, loadedB.UserId);
            Assert.NotEqual(loadedA.Email, loadedB.Email);

            await sessions.DeleteSessionAsync(sessionA);

            Assert.Null(await sessions.GetSessionAsync(sessionA));
            Assert.NotNull(await sessions.GetSessionAsync(sessionB));
        }

        [Fact]
        public async Task UnknownOrExpiredSessionIsRejected()
        {
            var sessions = new RedisSessionService(new FakeUsers());
            var expiredId = await sessions.CreateSessionAsync(Guid.NewGuid(), TimeSpan.FromTicks(-1));

            Assert.Null(await sessions.GetSessionAsync("missing"));
            Assert.Null(await sessions.GetSessionAsync(expiredId));
        }

        [Fact]
        public async Task MeRequiresTheMatchingSessionCookie()
        {
            var sessions = new RedisSessionService(new FakeUsers());
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();
            var sessionA = await sessions.CreateSessionAsync(userA, TimeSpan.FromHours(1));
            var sessionB = await sessions.CreateSessionAsync(userB, TimeSpan.FromHours(1));

            var anonymous = await CreateController(sessions).GetCurrentUser();
            Assert.IsType<UnauthorizedResult>(anonymous);

            var forged = CreateController(sessions);
            forged.ControllerContext.HttpContext.Request.Headers.Cookie = "session_id=not-a-session";
            Assert.IsType<UnauthorizedResult>(await forged.GetCurrentUser());

            var asA = CreateController(sessions);
            asA.ControllerContext.HttpContext.Request.Headers.Cookie = $"session_id={sessionA}";
            var ok = Assert.IsType<OkObjectResult>(await asA.GetCurrentUser());
            var json = JsonSerializer.Serialize(ok.Value);
            Assert.Contains(userA.ToString(), json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(sessionB, json, StringComparison.Ordinal);
            Assert.DoesNotContain(userB.ToString(), json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateSessionDoesNotSucceedWithoutASession()
        {
            var sessions = new RedisSessionService(new FakeUsers());
            var result = await CreateController(sessions).ValidateSession();
            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);

            Assert.Contains("\"success\":false", json, StringComparison.Ordinal);
        }

        [Fact]
        public async Task LogoutDeletesOnlyThatSessionAndMatchesTheLoginCookie()
        {
            var sessions = new RedisSessionService(new FakeUsers());
            var sessionA = await sessions.CreateSessionAsync(Guid.NewGuid(), TimeSpan.FromHours(1));
            var sessionB = await sessions.CreateSessionAsync(Guid.NewGuid(), TimeSpan.FromHours(1));

            var controller = CreateController(sessions);
            controller.ControllerContext.HttpContext.Request.Scheme = "https";
            controller.ControllerContext.HttpContext.Request.Headers.Cookie = $"session_id={sessionA}";

            Assert.IsType<OkObjectResult>(await controller.Logout());
            Assert.Null(await sessions.GetSessionAsync(sessionA));
            Assert.NotNull(await sessions.GetSessionAsync(sessionB));

            var setCookie = controller.Response.Headers.SetCookie.ToString();
            Assert.Contains("session_id=", setCookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        }

        private static AuthController CreateController(ISessionService sessions)
        {
            var controller = new AuthController(new DenyAuth(), sessions);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            };
            return controller;
        }

        private sealed class DenyAuth : IAuthService
        {
            public Task<JwtUser?> ValidateCredentialsAsync(string email, string password)
            {
                return Task.FromResult<JwtUser?>(null);
            }
        }

        private sealed class FakeUsers : IUserService
        {
            public Task<JwtUser?> ValidateCredentialsAsync(string email, string password)
            {
                return Task.FromResult<JwtUser?>(null);
            }

            public Task<JwtUser?> GetByIdAsync(Guid userId)
            {
                return Task.FromResult<JwtUser?>(new JwtUser
                {
                    Email = $"{userId}@users.test",
                    UserName = userId.ToString(),
                });
            }
        }
    }
}
