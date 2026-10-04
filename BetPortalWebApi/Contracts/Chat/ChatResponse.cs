namespace BetPortalWebApi.Contracts.Chat
{
    public class ChatResponse
    {
        required public string Content { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
