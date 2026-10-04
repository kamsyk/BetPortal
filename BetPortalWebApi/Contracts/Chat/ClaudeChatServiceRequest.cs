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
}
