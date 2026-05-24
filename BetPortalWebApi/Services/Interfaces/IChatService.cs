using BetPortalWebApi.Contracts.Chat;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IChatService
    {
        Task<string> GetChatResponseAsync(string userMessage, List<ChatMessage> conversationHistory);
    }
}
