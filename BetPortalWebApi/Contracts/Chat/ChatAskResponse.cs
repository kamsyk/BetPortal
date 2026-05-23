namespace BetPortalWebApi.Contracts.Chat
{
    public class ChatAskResponse
    {
        public bool Success { get; set; } = true;

        public string Answer { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string ErrorCode { get; set; } = string.Empty;

        public List<string> Sources { get; set; } = [];
    }
}
