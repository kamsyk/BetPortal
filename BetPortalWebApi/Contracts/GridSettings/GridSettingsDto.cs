namespace BetPortalWebApi.Contracts.GridSettings
{
    public class GridSettingsDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string PageIdentifier { get; set; } = null!;

        public string? SettingsJson { get; set; }

        public DateTime ModifiedDate { get; set; }
    }
}
