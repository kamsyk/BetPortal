namespace BetPortalWebApi.Contracts.User
{
    public class BpUserDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string[] Roles { get; set; } = Array.Empty<string>();
    }
}
