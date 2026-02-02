using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;

namespace ProExporter
{
    /// <summary>
    /// Exports map views and layouts as PNG images
    /// </summary>
    public static class ImageExporter
    {
        /// <summary>
        /// Default export settings
        /// </summary>
        public static int DefaultMapWidth { get; set; } = 1920;
        public static int DefaultMapHeight { get; set; } = 1080;
        public static int DefaultDpi { get; set; } = 96;
        public static int DefaultLayoutDpi { get; set; } = 150;

        /// <summary>
        /// Export all map views and layouts to images
        /// </summary>
        public static async Task<List<string>> ExportAllAsync(string outputFolder, CancellationToken cancellationToken = default)
        {
            var exportedFiles = new List<string>();
            var imagesFolder = Path.Combine(outputFolder, "images");
            Directory.CreateDirectory(imagesFolder);

            // Export active map view
            var mapFiles = await ExportMapViewsAsync(imagesFolder, cancellationToken);
            exportedFiles.AddRange(mapFiles);

            // Export layouts
            var layoutFiles = await ExportLayoutsAsync(imagesFolder, cancellationToken);
            exportedFiles.AddRange(layoutFiles);

            return exportedFiles;
        }

        /// <summary>
        /// Export the active map view to PNG
        /// </summary>
        public static async Task<List<string>> ExportMapViewsAsync(string outputFolder, CancellationToken cancellationToken = default)
        {
            var exportedFiles = new List<string>();
            
            // Ensure output directory exists
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView == null)
                    return;

                var map = mapView.Map;
                if (map == null)
                    return;

                // Create safe filename from map name
                var safeName = SanitizeFileName(map.Name);
                var outputPath = Path.Combine(outputFolder, $"map_{safeName}.png");

                try
                {
                    // Use PNG export format
                    var pngFormat = new PNGFormat
                    {
                        OutputFileName = outputPath,
                        Resolution = DefaultDpi,
                        Width = DefaultMapWidth,
                        Height = DefaultMapHeight,
                        HasTransparentBackground = false
                    };

                    mapView.Export(pngFormat);
                    exportedFiles.Add(outputPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to export map {map.Name}: {ex.Message}");
                }
            });

            return exportedFiles;
        }

        /// <summary>
        /// Export all layouts to PNG
        /// </summary>
        public static async Task<List<string>> ExportLayoutsAsync(string outputFolder, CancellationToken cancellationToken = default)
        {
            var exportedFiles = new List<string>();
            var project = Project.Current;
            if (project == null)
                return exportedFiles;

            // Ensure output directory exists
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var layoutItems = project.GetItems<LayoutProjectItem>();

            foreach (var layoutItem in layoutItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await QueuedTask.Run(() =>
                {
                    var layout = layoutItem.GetLayout();
                    if (layout == null)
                        return;

                    var safeName = SanitizeFileName(layout.Name);
                    var outputPath = Path.Combine(outputFolder, $"layout_{safeName}.png");

                    try
                    {
                        // Create PNG export format
                        var pngFormat = new PNGFormat
                        {
                            OutputFileName = outputPath,
                            Resolution = DefaultLayoutDpi,
                            HasTransparentBackground = false
                        };

                        // Export the layout
                        layout.Export(pngFormat);
                        exportedFiles.Add(outputPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to export layout {layout.Name}: {ex.Message}");
                    }
                });
            }

            return exportedFiles;
        }

        /// <summary>
        /// Sanitize a string to be safe for use as a filename
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unnamed";

            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = name;
            
            foreach (var c in invalid)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            // Also replace spaces with underscores for consistency
            sanitized = sanitized.Replace(' ', '_');

            // Limit length
            if (sanitized.Length > 50)
                sanitized = sanitized.Substring(0, 50);

            return sanitized;
        }
    }
}
