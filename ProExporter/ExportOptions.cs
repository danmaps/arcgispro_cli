namespace ProExporter
{
    public enum MapSelectionMode
    {
        All = 0,
        IncludeOnly = 1,
        Exclude = 2
    }

    /// <summary>
    /// Options that control what gets exported
    /// </summary>
    public class ExportOptions
    {
        /// <summary>
        /// Include map and layout screenshots
        /// </summary>
        public bool ExportImages { get; set; } = true;

        /// <summary>
        /// Include notebook metadata
        /// </summary>
        public bool ExportNotebooks { get; set; } = true;

        /// <summary>
        /// Include field schemas for layers
        /// </summary>
        public bool ExportFields { get; set; } = true;

        /// <summary>
        /// Number of sample rows to export per layer/table (0 = none)
        /// </summary>
        public int SampleRowCount { get; set; } = 10;

        /// <summary>
        /// If true, only export content for the active map.
        /// </summary>
        public bool ActiveMapOnly { get; set; } = false;

        /// <summary>
        /// Map selection mode for include/exclude filtering.
        /// </summary>
        public MapSelectionMode MapSelectionMode { get; set; } = MapSelectionMode.All;

        /// <summary>
        /// Map name used by include/exclude filtering modes.
        /// </summary>
        public string SelectedMapName { get; set; }

        /// <summary>
        /// Create options from config
        /// </summary>
        public static ExportOptions FromConfig(ExportConfig config)
        {
            return new ExportOptions
            {
                ExportImages = config.ExportImages,
                ExportNotebooks = config.ExportNotebooks,
                ExportFields = config.ExportFields,
                SampleRowCount = config.SampleRowCount,
                ActiveMapOnly = false,
                MapSelectionMode = MapSelectionMode.All,
                SelectedMapName = null
            };
        }

        public bool ShouldIncludeMap(string mapName, bool isActiveMap)
        {
            if (ActiveMapOnly)
            {
                return isActiveMap;
            }

            if (string.IsNullOrWhiteSpace(mapName))
            {
                return true;
            }

            var selected = SelectedMapName ?? string.Empty;

            switch (MapSelectionMode)
            {
                case MapSelectionMode.IncludeOnly:
                    if (string.IsNullOrWhiteSpace(selected))
                        return true;
                    return string.Equals(mapName, selected, System.StringComparison.OrdinalIgnoreCase);

                case MapSelectionMode.Exclude:
                    if (string.IsNullOrWhiteSpace(selected))
                        return true;
                    return !string.Equals(mapName, selected, System.StringComparison.OrdinalIgnoreCase);

                default:
                    return true;
            }
        }

        /// <summary>
        /// Default options (export everything)
        /// </summary>
        public static ExportOptions Default => new ExportOptions();
    }
}
