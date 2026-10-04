namespace BetPortalWebApi.Contracts.Chat
{
    public class ChatMessage
    {
        required public string Role { get; set; }

        required public string Content { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
