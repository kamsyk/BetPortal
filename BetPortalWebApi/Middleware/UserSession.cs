namespace BetPortalWebApi.Middleware
{
    public class UserSession
    {
        required public string SessionId { get; set; }

        required public int UserId { get; set; }

        required public string Username { get; set; }

        required public string FullName { get; set; }

        public string? Email { get; set; }

        public string[] Roles { get; set; } = Array.Empty<string>();

        public List<string> Permissions { get; set; } = [];

        required public string IpAddress { get; set; }

        required public string UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? LastAccessedAt { get; set; }

        public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

        public bool IsValid => !IsExpired && UserId > 0 && !string.IsNullOrEmpty(SessionId);

        public void UpdateLastAccessed()
        {
            LastAccessedAt = DateTime.UtcNow;
        }

        public void ExtendExpiration(TimeSpan extension)
        {
            ExpiresAt = DateTime.UtcNow.Add(extension);
        }

        public bool HasPermission(string permission)
        {
            return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        public bool HasRole(string role)
        {
            return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }
    }
}