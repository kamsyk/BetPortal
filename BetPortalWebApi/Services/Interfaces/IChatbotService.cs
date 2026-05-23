using BetPortalWebApi.Contracts.Chat;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IChatbotService
    {
        Task<ChatAskResponse> AskAsync(string question, CancellationToken cancellationToken);

        Task<ChatReindexResponse> ReindexAsync(CancellationToken cancellationToken);
    }
}
