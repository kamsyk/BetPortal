namespace BetPortalWebApi.Contracts.GridSettings
{
    public class SaveGridSettingsRequest
    {
        public string PageIdentifier { get; set; } = null!;

        public GridSettingsData Settings { get; set; } = null!;
    }
}
