namespace ProExporter
{
    internal static class RibbonExportFilterState
    {
        public static bool ActiveMapOnly { get; set; } = false;

        public static ExportOptions ApplyTo(ExportOptions options)
        {
            options ??= ExportOptions.Default;
            options.ActiveMapOnly = ActiveMapOnly;
            return options;
        }
    }
}
