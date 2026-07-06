using System.Text.Json.Serialization;

namespace BetPortalWebApi.Contracts.Chat
{
    public class ClaudeChatServiceRequest
    {
        [JsonPropertyName("system_prompt")]
        required public string SystemPrompt { get; set; }

        [JsonPropertyName("messages")]
        public List<ClaudeChatServiceMessage> Messages { get; set; } = [];

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }
    }

    public class ClaudeChatServiceMessage
    {
        [JsonPropertyName("role")]
        required public string Role { get; set; }

        [JsonPropertyName("content")]
        required public string Content { get; set; }
    }

    public class ClaudeChatServiceResponse
    {
        [JsonPropertyName("content")]
        required public string Content { get; set; }
    }
}
