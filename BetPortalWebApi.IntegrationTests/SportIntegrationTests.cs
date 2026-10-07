using System.Net;
using System.Net.Http.Json;
using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

[Collection("API integration tests")]
public sealed class SportIntegrationTests
{
    private readonly ApiFixture _fixture;

    public SportIntegrationTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task List_filters_by_name_and_sorts_descending()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);
        var alphaName = $"{scope.NamePrefix}alpha";
        var betaName = $"{scope.NamePrefix}beta";
        await AddSportAsync(scope, alphaName, TestContext.Current.CancellationToken);
        await AddSportAsync(scope, betaName, TestContext.Current.CancellationToken);

        using var response = await scope.Client.GetAsync(
            $"/api/sport/list?name={Uri.EscapeDataString(scope.NamePrefix)}&sortBy=name&sortOrder=desc", TestContext.Current.CancellationToken);
        var sports = await response.Content.ReadFromJsonAsync<List<SportResponse>>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(sports);
        Assert.Equal(new[] { betaName, alphaName }, sports.Select(sport => sport.Name));
    }

    [Fact]
    public async Task Add_creates_sport_and_persists_values()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);
        var name = $"{scope.NamePrefix}added";

        using var response = await scope.Client.PostAsJsonAsync("/api/sport/add", new { Name = name, Active = true }, TestContext.Current.CancellationToken);
        var created = await response.Content.ReadFromJsonAsync<SportResponse>(TestContext.Current.CancellationToken);
        await using var dbContext = _fixture.CreateDbContext();
        var persisted = await dbContext.Sports.AsNoTracking().SingleOrDefaultAsync(sport => sport.Name == name, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal(name, created.Name);
        Assert.True(created.Active);
        Assert.NotNull(persisted);
        Assert.Equal(created.Id, persisted.Id);
        Assert.Equal(name, persisted.Name);
    }

    [Fact]
    public async Task Add_rejects_blank_name()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);

        using var response = await scope.Client.PostAsJsonAsync("/api/sport/add", new { Name = " \t", Active = true }, TestContext.Current.CancellationToken);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("Sport name is required.", error.Message);
    }

    [Fact]
    public async Task Add_rejects_duplicate_name()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);
        var name = $"{scope.NamePrefix}duplicate";
        await AddSportAsync(scope, name, TestContext.Current.CancellationToken);

        using var duplicateResponse = await scope.Client.PostAsJsonAsync("/api/sport/add", new { Name = name, Active = true }, TestContext.Current.CancellationToken);
        var error = await duplicateResponse.Content.ReadFromJsonAsync<ApiErrorResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("Sport already exists.", error.Message);
    }

    [Fact]
    public async Task Update_returns_not_found_for_missing_sport()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);

        using var response = await scope.Client.PutAsJsonAsync("/api/sport/2147483647", new { Name = $"{scope.NamePrefix}missing", Active = true }, TestContext.Current.CancellationToken);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("Sport not found.", error.Message);
    }

    [Fact]
    public async Task Update_rejects_a_duplicate_name_without_changing_existing_sport()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);
        var originalName = $"{scope.NamePrefix}original";
        var duplicateName = $"{scope.NamePrefix}duplicate";
        var sport = await AddSportAsync(scope, originalName, TestContext.Current.CancellationToken);
        await AddSportAsync(scope, duplicateName, TestContext.Current.CancellationToken);

        using var response = await scope.Client.PutAsJsonAsync(
            $"/api/sport/{sport.Id}",
            new { Name = duplicateName, Active = false }, TestContext.Current.CancellationToken);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(TestContext.Current.CancellationToken);
        await using var dbContext = _fixture.CreateDbContext();
        var persisted = await dbContext.Sports.AsNoTracking().SingleAsync(item => item.Id == sport.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("Another sport with this name already exists.", error.Message);
        Assert.Equal(originalName, persisted.Name);
        Assert.True(persisted.Active);
    }

    [Fact]
    public async Task Update_changes_sport_and_persists_values()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);
        var originalName = $"{scope.NamePrefix}before";
        var updatedName = $"{scope.NamePrefix}after";
        var sport = await AddSportAsync(scope, originalName, TestContext.Current.CancellationToken);

        using var response = await scope.Client.PutAsJsonAsync(
            $"/api/sport/{sport.Id}",
            new { Name = updatedName, Active = false }, TestContext.Current.CancellationToken);
        var updated = await response.Content.ReadFromJsonAsync<SportResponse>(TestContext.Current.CancellationToken);
        await using var dbContext = _fixture.CreateDbContext();
        var persisted = await dbContext.Sports.AsNoTracking().SingleOrDefaultAsync(item => item.Id == sport.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(sport.Id, updated.Id);
        Assert.Equal(updatedName, updated.Name);
        Assert.False(updated.Active);
        Assert.NotNull(persisted);
        Assert.Equal(updatedName, persisted.Name);
        Assert.False(persisted.Active);
    }

    [Fact]
    public async Task Delete_removes_sport_and_returns_not_found_when_repeated()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);
        var sport = await AddSportAsync(scope, $"{scope.NamePrefix}delete", TestContext.Current.CancellationToken);

        using var deleteResponse = await scope.Client.DeleteAsync($"/api/sport/{sport.Id}", TestContext.Current.CancellationToken);
        await using var dbContext = _fixture.CreateDbContext();
        var persisted = await dbContext.Sports.AsNoTracking().SingleOrDefaultAsync(item => item.Id == sport.Id, TestContext.Current.CancellationToken);
        using var repeatedResponse = await scope.Client.DeleteAsync($"/api/sport/{sport.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Null(persisted);
        Assert.Equal(HttpStatusCode.NotFound, repeatedResponse.StatusCode);
    }

    private static async Task<SportResponse> AddSportAsync(ApiTestScope scope, string name, CancellationToken cancellationToken)
    {
        using var response = await scope.Client.PostAsJsonAsync("/api/sport/add", new { Name = name, Active = true }, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sport = await response.Content.ReadFromJsonAsync<SportResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(sport);
        return sport;
    }
}