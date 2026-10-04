namespace BetPortalWebApi.Contracts.GridSettings
{
    public class GridSettingsData
    {
        public int? PageSize { get; set; }

        public object? FilterModel { get; set; }

        public List<ColumnState>? ColumnState { get; set; }
    }
}
