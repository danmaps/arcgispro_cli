namespace ProExporter
{
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
                ActiveMapOnly = false
            };
        }

        /// <summary>
        /// Returns true if the given map should be included in the export.
        /// </summary>
        public bool ShouldIncludeMap(string mapName, bool isActiveMap)
        {
            if (ActiveMapOnly)
            {
                return isActiveMap;
            }

            return true;
        }

        /// <summary>
        /// Default options (export everything)
        /// </summary>
        public static ExportOptions Default => new ExportOptions();
    }
}

