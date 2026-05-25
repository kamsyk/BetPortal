namespace BetPortalWebApi.Contracts.Chat
{
    public class ChatMessage
    {
        required public string Role { get; set; }

        required public string Content { get; set; }

        public DateTime Timestamp { get; set; }
    }

    public class ChatRequest
    {
        required public string Message { get; set; }

        public List<ChatMessage> ConversationHistory { get; set; } = [];
    }

    public class ChatResponse
    {
        required public string Content { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
