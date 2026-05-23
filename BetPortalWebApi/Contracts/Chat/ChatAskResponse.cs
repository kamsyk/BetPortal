namespace BetPortalWebApi.Contracts.Chat
{
    public class ChatAskResponse
    {
        public string Answer { get; set; } = string.Empty;

        public List<string> Sources { get; set; } = [];
    }
}
