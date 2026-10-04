using System.Text.Json.Serialization;

namespace BetPortalWebApi.Contracts.Chat
{
    public class ClaudeChatServiceMessage
    {
        [JsonPropertyName("role")]
        required public string Role { get; set; }

        [JsonPropertyName("content")]
        required public string Content { get; set; }
    }
}
