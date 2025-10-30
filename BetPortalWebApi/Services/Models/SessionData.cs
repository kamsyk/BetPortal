namespace BetPortalWebApi.Services.Models
{
    public class SessionData
    {
        public Guid UserId { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
