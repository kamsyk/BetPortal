namespace BetPortalWebApi.Contracts.Chat
{
    public class ChatMessage
    {
        public required string Role { get; set; }
        public required string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ChatRequest
    {
        public required string Message { get; set; }
        public List<ChatMessage> ConversationHistory { get; set; } = new();
    }

    public class ChatResponse
    {
        public required string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
