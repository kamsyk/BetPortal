namespace BetPortalWebApi.Contracts.Chat
{
    public class ChatRequest
    {
        required public string Message { get; set; }

        public List<ChatMessage> ConversationHistory { get; set; } = [];
    }
}
