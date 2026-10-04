using System.Text.Json.Serialization;

namespace BetPortalWebApi.Contracts.Chat
{
    public class ClaudeChatServiceResponse
    {
        [JsonPropertyName("content")]
        required public string Content { get; set; }
    }
}
