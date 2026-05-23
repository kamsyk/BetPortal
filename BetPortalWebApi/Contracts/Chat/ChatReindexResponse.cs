namespace BetPortalWebApi.Contracts.Chat
{
    public class ChatReindexResponse
    {
        public bool Success { get; set; }

        public int IndexedItems { get; set; }

        public string Message { get; set; } = string.Empty;

        public string ErrorCode { get; set; } = string.Empty;
    }
}
