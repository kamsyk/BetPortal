namespace BetPortalWebApi.Services.Interfaces
{
    public interface IDbQueryService
    {
        Task<string> GetDatabaseContextAsync(string userMessage);
    }
}
