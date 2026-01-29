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
        /// Create options from current settings
        /// </summary>
        public static ExportOptions FromSettings()
        {
            return new ExportOptions
            {
                ExportImages = Properties.Settings.Default.ExportImages,
                ExportNotebooks = Properties.Settings.Default.ExportNotebooks,
                ExportFields = Properties.Settings.Default.ExportFields
            };
        }

        /// <summary>
        /// Default options (export everything)
        /// </summary>
        public static ExportOptions Default => new ExportOptions();
    }
}
