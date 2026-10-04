namespace BetPortalWebApi.Contracts.GridSettings
{
    public class ColumnState
    {
        public string ColId { get; set; } = null!;

        public bool? Hide { get; set; }

        public int? Width { get; set; }

        public int? Flex { get; set; }

        public string? Sort { get; set; }

        public int? SortIndex { get; set; }

        public string? AggFunc { get; set; }

        public bool? RowGroup { get; set; }

        public int? RowGroupIndex { get; set; }

        public bool? Pivot { get; set; }

        public int? PivotIndex { get; set; }

        public string? Pinned { get; set; }
    }
}
