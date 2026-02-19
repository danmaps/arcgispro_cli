using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace ProExporter
{
    /// <summary>
    /// Orchestrates all export operations with cancellation and error handling
    /// </summary>
    public class ExportController
    {
        private readonly SemaphoreSlim _exportLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _currentExportCts;

        /// <summary>
        /// Get the output folder path for the current project
        /// </summary>
        public static string GetOutputFolder()
        {
            var project = Project.Current;
            if (project == null)
                return null;

            // Get project directory
            var projectPath = project.URI;
            if (string.IsNullOrEmpty(projectPath))
                return null;

            var projectDir = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDir))
                return null;

            return Path.Combine(projectDir, ".arcgispro");
        }

        /// <summary>
        /// Run a full snapshot export (context + images)
        /// </summary>
        public async Task<ExportResult> RunSnapshotAsync(ExportOptions options = null)
        {
            options ??= ExportOptions.Default;
            return await RunExportAsync(exportContext: true, exportImages: options.ExportImages, options: options, cleanSnapshotOutput: true);
        }

        /// <summary>
        /// Run context-only export (JSON + Markdown, no images)
        /// </summary>
        public async Task<ExportResult> RunContextExportAsync(ExportOptions options = null)
        {
            options ??= ExportOptions.Default;
            return await RunExportAsync(exportContext: true, exportImages: false, options: options, cleanSnapshotOutput: false);
        }

        /// <summary>
        /// Run images-only export
        /// </summary>
        public async Task<ExportResult> RunImageExportAsync(ExportOptions options = null)
        {
            options ??= ExportOptions.Default;
            return await RunExportAsync(exportContext: false, exportImages: true, options: options, cleanSnapshotOutput: false);
        }

        /// <summary>
        /// Core export method with options
        /// </summary>
        private async Task<ExportResult> RunExportAsync(bool exportContext, bool exportImages, ExportOptions options, bool cleanSnapshotOutput)
        {
            var result = new ExportResult();
            var stopwatch = Stopwatch.StartNew();

            // Try to acquire the lock (prevent concurrent exports)
            if (!await _exportLock.WaitAsync(TimeSpan.FromSeconds(1)))
            {
                result.Success = false;
                result.Errors.Add("Another export is already in progress");
                return result;
            }

            try
            {
                // Cancel any previous export
                _currentExportCts?.Cancel();
                _currentExportCts = new CancellationTokenSource();
                var ct = _currentExportCts.Token;

                // Get output folder
                var outputFolder = GetOutputFolder();
                if (string.IsNullOrEmpty(outputFolder))
                {
                    result.Success = false;
                    result.Errors.Add("No project is currently open");
                    return result;
                }

                result.OutputPath = outputFolder;

                if (cleanSnapshotOutput)
                {
                    try
                    {
                        CleanupSnapshotOutput(outputFolder);
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.Errors.Add($"Failed to clear previous snapshot output: {ex.Message}");
                        return result;
                    }
                }

                // Create output folder structure
                Directory.CreateDirectory(outputFolder);

                // Export context (JSON + Markdown)
                if (exportContext)
                {
                    try
                    {
                        var context = await ContextCollector.CollectAsync(options, ct);
                        var contextFiles = await Serializer.WriteContextAsync(context, outputFolder);
                        result.FilesCreated.AddRange(contextFiles);
                    }
                    catch (OperationCanceledException)
                    {
                        result.Warnings.Add("Context export was cancelled");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Context export failed: {ex.Message}");
                    }
                }

                // Export images
                if (exportImages)
                {
                    try
                    {
                        var imageFiles = await ImageExporter.ExportAllAsync(outputFolder, options, ct);
                        result.FilesCreated.AddRange(imageFiles);

                        if (imageFiles.Count == 0)
                        {
                            result.Warnings.Add("No images were exported (no active map view?)");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        result.Warnings.Add("Image export was cancelled");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Image export failed: {ex.Message}");
                    }
                }

                result.Success = result.Errors.Count == 0;
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                _exportLock.Release();
            }

            return result;
        }

        /// <summary>
        /// Cancel any running export
        /// </summary>
        public void CancelExport()
        {
            _currentExportCts?.Cancel();
        }

        /// <summary>
        /// Remove all previous snapshot output so next snapshot is a full replacement.
        /// </summary>
        private static void CleanupSnapshotOutput(string outputFolder)
        {
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, recursive: true);
            }

            var projectRoot = Directory.GetParent(outputFolder)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                return;

            var agentsUpperPath = Path.Combine(projectRoot, "AGENTS.md");
            if (File.Exists(agentsUpperPath))
            {
                File.Delete(agentsUpperPath);
            }

            var agentsLowerPath = Path.Combine(projectRoot, "agents.md");
            if (File.Exists(agentsLowerPath))
            {
                File.Delete(agentsLowerPath);
            }
        }

        /// <summary>
        /// Open the output folder in Windows Explorer
        /// </summary>
        public static void OpenOutputFolder()
        {
            var outputFolder = GetOutputFolder();
            if (string.IsNullOrEmpty(outputFolder))
                return;

            // Create the folder if it doesn't exist
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // Open in Explorer
            Process.Start(new ProcessStartInfo
            {
                FileName = outputFolder,
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Check if there's an active project that can be exported
        /// </summary>
        public static bool CanExport()
        {
            return Project.Current != null && !string.IsNullOrEmpty(Project.Current.URI);
        }
    }
}
