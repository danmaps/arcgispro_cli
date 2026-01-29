using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.KnowledgeGraph;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;

namespace ProExporter
{
    internal class Module1 : Module
    {
        private static Module1 _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static Module1 Current => _this ??= (Module1)FrameworkApplication.FindModule("ProExporter_Module");

        #region Overrides

        /// <summary>
        /// Called when the module is initialized
        /// </summary>
        protected override bool Initialize()
        {
            // Subscribe to project opened event for auto-export
            ProjectOpenedEvent.Subscribe(OnProjectOpened);
            return base.Initialize();
        }

        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            return true;
        }

        /// <summary>
        /// Called when the module is unloaded
        /// </summary>
        protected override void Uninitialize()
        {
            ProjectOpenedEvent.Unsubscribe(OnProjectOpened);
            base.Uninitialize();
        }

        #endregion Overrides

        #region Auto-Export

        /// <summary>
        /// Handle project opened event for auto-export
        /// </summary>
        private async void OnProjectOpened(ProjectEventArgs args)
        {
            var project = Project.Current;
            if (project == null)
                return;

            var projectPath = project.URI;
            if (string.IsNullOrEmpty(projectPath))
                return;

            var projectDir = System.IO.Path.GetDirectoryName(projectPath);
            var arcgisproFolder = System.IO.Path.Combine(projectDir, ".arcgispro");
            
            // Load config
            var config = ExportConfig.Load(arcgisproFolder);
            
            // Check if auto-export is enabled
            if (!config.AutoExportEnabled)
                return;

            // Run safety checks
            if (!ShouldAutoExport(config))
                return;

            // Run export in background
            try
            {
                var options = ExportOptions.FromConfig(config);
                var controller = new ExportController();
                await controller.RunSnapshotAsync(options);
            }
            catch
            {
                // Silently fail - user can manually export
            }
        }

        /// <summary>
        /// Check if auto-export should run based on safety conditions
        /// </summary>
        private bool ShouldAutoExport(ExportConfig config)
        {
            var project = Project.Current;
            if (project == null)
                return false;

            var projectPath = project.URI;
            if (string.IsNullOrEmpty(projectPath))
                return false;

            // Check local only setting
            if (config.AutoExportLocalOnly)
            {
                // Skip network paths (UNC or mapped drives that are network)
                if (projectPath.StartsWith("\\\\"))
                    return false;

                // Check if it's a local drive
                try
                {
                    var root = System.IO.Path.GetPathRoot(projectPath);
                    if (!string.IsNullOrEmpty(root))
                    {
                        var driveInfo = new System.IO.DriveInfo(root);
                        if (driveInfo.DriveType == System.IO.DriveType.Network)
                            return false;
                    }
                }
                catch
                {
                    // If we can't determine drive type, allow export
                }
            }

            // Check layer count
            var maxLayers = config.AutoExportMaxLayers;
            if (maxLayers > 0)
            {
                try
                {
                    var totalLayers = 0;
                    var maps = project.GetItems<MapProjectItem>();
                    foreach (var mapItem in maps)
                    {
                        var map = mapItem.GetMap();
                        if (map != null)
                        {
                            totalLayers += map.Layers.Count;
                        }
                    }

                    if (totalLayers > maxLayers)
                        return false;
                }
                catch
                {
                    // If we can't count layers, allow export
                }
            }

            return true;
        }

        #endregion
    }
}
