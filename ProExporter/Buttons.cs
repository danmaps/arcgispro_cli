using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;

namespace ProExporter
{
    /// <summary>
    /// Button: Export full snapshot (context + images)
    /// </summary>
    public class SnapshotButton : Button
    {
        protected override async void OnClick()
        {
            if (!ExportController.CanExport())
            {
                MessageBox.Show("Please open a project first.", "ArcGIS Pro CLI", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var controller = new ExportController();
                var result = await controller.RunSnapshotAsync();

                if (result.Success)
                {
                    var msg = $"Snapshot exported successfully!\n\n" +
                              $"Location: {result.OutputPath}\n" +
                              $"Files created: {result.FilesCreated.Count}\n" +
                              $"Duration: {result.Duration.TotalSeconds:F1}s";
                    
                    if (result.Warnings.Count > 0)
                    {
                        msg += $"\n\nWarnings:\n• {string.Join("\n• ", result.Warnings)}";
                    }

                    MessageBox.Show(msg, "ArcGIS Pro CLI", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var msg = $"Export failed:\n• {string.Join("\n• ", result.Errors)}";
                    MessageBox.Show(msg, "ArcGIS Pro CLI", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "ArcGIS Pro CLI", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Button: Export context only (JSON + Markdown)
    /// </summary>
    public class DumpContextButton : Button
    {
        private static readonly ExportController _controller = new ExportController();

        protected override async void OnClick()
        {
            if (!ExportController.CanExport())
            {
                MessageBox.Show("Please open a project first.", "ArcGIS Pro CLI", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var result = await _controller.RunContextExportAsync();

                if (result.Success)
                {
                    MessageBox.Show(
                        $"Context exported to:\n{result.OutputPath}\n\n" +
                        $"Files: {result.FilesCreated.Count} | Duration: {result.Duration.TotalSeconds:F1}s",
                        "ArcGIS Pro CLI", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Export failed:\n• {string.Join("\n• ", result.Errors)}", 
                        "ArcGIS Pro CLI", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "ArcGIS Pro CLI", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Button: Export images only
    /// </summary>
    public class ExportImagesButton : Button
    {
        private static readonly ExportController _controller = new ExportController();

        protected override async void OnClick()
        {
            if (!ExportController.CanExport())
            {
                MessageBox.Show("Please open a project first.", "ArcGIS Pro CLI", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var result = await _controller.RunImageExportAsync();

                if (result.Success)
                {
                    var msg = $"Images exported to:\n{result.OutputPath}\\images\n\n" +
                              $"Files: {result.FilesCreated.Count} | Duration: {result.Duration.TotalSeconds:F1}s";
                    
                    if (result.Warnings.Count > 0)
                    {
                        msg += $"\n\nWarnings:\n• {string.Join("\n• ", result.Warnings)}";
                    }

                    MessageBox.Show(msg, "ArcGIS Pro CLI", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Export failed:\n• {string.Join("\n• ", result.Errors)}", 
                        "ArcGIS Pro CLI", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "ArcGIS Pro CLI", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Button: Open output folder in Explorer
    /// </summary>
    public class OpenFolderButton : Button
    {
        protected override void OnClick()
        {
            if (!ExportController.CanExport())
            {
                MessageBox.Show("Please open a project first.", "ArcGIS Pro CLI", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ExportController.OpenOutputFolder();
        }
    }

    /// <summary>
    /// Button: Open terminal at project folder with ArcGIS Pro Python env activated
    /// </summary>
    public class OpenTerminalButton : Button
    {
        protected override void OnClick()
        {
            if (!ExportController.CanExport())
            {
                MessageBox.Show("Please open a project first.", "ArcGIS Pro CLI", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OpenTerminalWithProEnv();
        }

        private void OpenTerminalWithProEnv()
        {
            var project = Project.Current;
            if (project == null) return;

            var projectDir = Path.GetDirectoryName(project.URI);
            if (string.IsNullOrEmpty(projectDir)) return;

            // Export session info for scripts to use
            ExportSessionInfo(projectDir);

            var proPath = Environment.GetEnvironmentVariable("ProgramFiles") ?? @"C:\Program Files";
            var scriptsPath = Path.Combine(proPath, "ArcGIS", "Pro", "bin", "Python", "Scripts");
            var activateBat = Path.Combine(scriptsPath, "activate.bat");
            var pythonEnvUtils = Path.Combine(proPath, "ArcGIS", "Pro", "bin", "PythonEnvUtils.exe");

            // Get current ArcGIS Pro process ID for arcpy to connect to
            var proProcessId = Process.GetCurrentProcess().Id;

            string args;
            if (File.Exists(activateBat) && File.Exists(pythonEnvUtils))
            {
                // Set ARCGISPRO_PID so arcpy connects to this running Pro session
                // This prevents "Directory does not exist" errors with stale temp paths
                args = $"/k \"set ARCGISPRO_PID={proProcessId} && " +
                       $"set CONDA_SKIPCHECK=1 && " +
                       $"for /f \"delims=\" %i in ('\"{pythonEnvUtils}\"') do @call \"{activateBat}\" \"%i\" && " +
                       $"cd /d \"{projectDir}\"\"";
            }
            else
            {
                // Fallback: set PID and open at project folder
                args = $"/k \"set ARCGISPRO_PID={proProcessId} && cd /d \"{projectDir}\"\"";
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = args,
                    UseShellExecute = true,
                    WorkingDirectory = projectDir
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open terminal: {ex.Message}", "ArcGIS Pro CLI",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export current Pro session info (PID, temp paths) for CLI scripts to use.
        /// This helps Python scripts connect to the running Pro session and avoid stale workspace errors.
        /// </summary>
        private void ExportSessionInfo(string projectDir)
        {
            try
            {
                var arcgisproFolder = Path.Combine(projectDir, ".arcgispro");
                Directory.CreateDirectory(arcgisproFolder);

                var processId = Process.GetCurrentProcess().Id;
                var sessionInfo = new
                {
                    processId = processId,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    tempPath = Path.GetTempPath(),
                    proTempPath = Path.Combine(Path.GetTempPath(), $"ArcGISProTemp{processId}")
                };

                var json = JsonSerializer.Serialize(sessionInfo, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(Path.Combine(arcgisproFolder, "session.json"), json);
            }
            catch
            {
                // Non-critical - continue even if session export fails
            }
        }
    }
}
