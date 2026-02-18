using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProExporter
{
    internal static class RibbonExportFilterState
    {
        public static bool ActiveMapOnly { get; set; } = false;

        public static MapSelectionMode MapSelectionMode { get; set; } = MapSelectionMode.All;

        public static string SelectedMapName { get; set; }

        public static ExportOptions ApplyTo(ExportOptions options)
        {
            options ??= ExportOptions.Default;
            options.ActiveMapOnly = ActiveMapOnly;
            options.MapSelectionMode = MapSelectionMode;
            options.SelectedMapName = SelectedMapName;
            return options;
        }

        public static List<string> GetProjectMapNames()
        {
            return QueuedTask.Run(() =>
            {
                var project = Project.Current;
                if (project == null)
                    return new List<string>();

                return project
                    .GetItems<MapProjectItem>()
                    .Select(m => m.Name)
                    .OrderBy(name => name)
                    .ToList();
            }).Result;
        }
    }
}
