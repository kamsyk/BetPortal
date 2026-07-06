namespace BetPortalWebApi.Contracts.Chat
{
    public class ClaudeChatServiceRequest
    {
        required public string SystemPrompt { get; set; }

        public List<ClaudeChatServiceMessage> Messages { get; set; } = [];

        public string? Model { get; set; }

        public int? MaxTokens { get; set; }
    }

    public class ClaudeChatServiceMessage
    {
        required public string Role { get; set; }

        required public string Content { get; set; }
    }

    public class ClaudeChatServiceResponse
    {
        required public string Content { get; set; }
    }
}
