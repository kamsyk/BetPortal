using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

[Collection("API integration tests")]
public sealed class UserTokenIntegrationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Valid_token_activates_user_consumes_token_and_cannot_be_reused()
    {
        var user = await SeedUserAsync();
        var token = user.UserToken!.UserInitToken;
        try
        {
            await using var scope = fixture.CreateTestScope();
            using var response = await scope.Client.PostAsJsonAsync("/api/user-token", new { token }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            Assert.False(response.Headers.Contains("Set-Cookie"));
            await using var db = fixture.CreateDbContext();
            var activated = await db.BpUsers.AsNoTracking().SingleAsync(u => u.Id == user.Id, TestContext.Current.CancellationToken);
            Assert.True(activated.IsActive);
            Assert.True(activated.ModifiedDate > user.ModifiedDate);
            Assert.False(await db.BpUserTokens.AnyAsync(t => t.UserId == user.Id, TestContext.Current.CancellationToken));
            using var replay = await scope.Client.PostAsJsonAsync("/api/user-token", new { token }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, replay.StatusCode);
        }
        finally
        {
            await DeleteUserAsync(user.Id);
        }
    }

    [Fact]
    public async Task Concurrent_activation_succeeds_only_once()
    {
        var user = await SeedUserAsync();
        try
        {
            await using var first = fixture.CreateTestScope();
            await using var second = fixture.CreateTestScope();
            var requests = await Task.WhenAll(
                first.Client.PostAsJsonAsync("/api/user-token", new { token = user.UserToken!.UserInitToken }, TestContext.Current.CancellationToken),
                second.Client.PostAsJsonAsync("/api/user-token", new { token = user.UserToken!.UserInitToken }, TestContext.Current.CancellationToken));
            try
            {
                Assert.Single(requests, response => response.StatusCode == HttpStatusCode.NoContent);
                Assert.Single(requests, response => response.StatusCode == HttpStatusCode.NotFound);
                await using var db = fixture.CreateDbContext();
                Assert.True(await db.BpUsers.Where(u => u.Id == user.Id).Select(u => u.IsActive).SingleAsync(TestContext.Current.CancellationToken));
                Assert.False(await db.BpUserTokens.AnyAsync(t => t.UserId == user.Id, TestContext.Current.CancellationToken));
            }
            finally
            {
                foreach (var response in requests)
                {
                    response.Dispose();
                }
            }
        }
        finally
        {
            await DeleteUserAsync(user.Id);
        }
    }

    [Theory]
    [InlineData(null, HttpStatusCode.BadRequest)]
    [InlineData("", HttpStatusCode.BadRequest)]
    [InlineData(" ", HttpStatusCode.BadRequest)]
    [InlineData("unknown", HttpStatusCode.NotFound)]
    public async Task Invalid_tokens_do_not_activate_or_consume_another_users_token(string? token, HttpStatusCode expected)
    {
        var user = await SeedUserAsync();
        try
        {
            await using var scope = fixture.CreateTestScope();
            using var response = await scope.Client.PostAsJsonAsync("/api/user-token", new { token }, TestContext.Current.CancellationToken);
            Assert.Equal(expected, response.StatusCode);
            await using var db = fixture.CreateDbContext();
            Assert.False(await db.BpUsers.Where(u => u.Id == user.Id).Select(u => u.IsActive).SingleAsync(TestContext.Current.CancellationToken));
            Assert.True(await db.BpUserTokens.AnyAsync(t => t.UserId == user.Id, TestContext.Current.CancellationToken));
        }
        finally
        {
            await DeleteUserAsync(user.Id);
        }
    }

    [Fact]
    public async Task Token_comparison_is_case_sensitive()
    {
        var user = await SeedUserAsync();
        try
        {
            await using var scope = fixture.CreateTestScope();
            using var response = await scope.Client.PostAsJsonAsync(
                "/api/user-token", new { token = user.UserToken!.UserInitToken.ToLowerInvariant() }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            await using var db = fixture.CreateDbContext();
            Assert.True(await db.BpUserTokens.AnyAsync(t => t.UserId == user.Id, TestContext.Current.CancellationToken));
            Assert.False(await db.BpUsers.Where(u => u.Id == user.Id).Select(u => u.IsActive).SingleAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            await DeleteUserAsync(user.Id);
        }
    }

    private async Task<BpUser> SeedUserAsync()
    {
        var user = new BpUser
        {
            UserName = $"activation-{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@example.test",
            IsActive = false,
            CreatedDate = DateTime.UtcNow.AddYears(-1),
            ModifiedDate = DateTime.UtcNow.AddYears(-1),
            CreatedByUserId = 1,
            ModifiedByUserId = 1,
        };
        user.UserToken = new BpUserToken
        {
            User = user,
            UserInitToken = "A" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            CreatedDate = user.CreatedDate,
        };
        await using var db = fixture.CreateDbContext();
        db.BpUsers.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    private async Task DeleteUserAsync(int userId)
    {
        await using var db = fixture.CreateDbContext();
        await db.BpUserTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync();
        await db.BpUsers.Where(u => u.Id == userId).ExecuteDeleteAsync();
    }
}
