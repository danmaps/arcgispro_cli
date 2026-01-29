using System;
using System.Diagnostics;
using System.IO;
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

            var proPath = Environment.GetEnvironmentVariable("ProgramFiles") ?? @"C:\Program Files";
            var scriptsPath = Path.Combine(proPath, "ArcGIS", "Pro", "bin", "Python", "Scripts");
            var activateBat = Path.Combine(scriptsPath, "activate.bat");
            var pythonEnvUtils = Path.Combine(proPath, "ArcGIS", "Pro", "bin", "PythonEnvUtils.exe");

            string args;
            if (File.Exists(activateBat) && File.Exists(pythonEnvUtils))
            {
                // Replicate proenv.bat logic but cd to project folder instead of env folder
                // 1. Get active env from PythonEnvUtils.exe
                // 2. Set CONDA_SKIPCHECK=1 so activate.bat uses fast path
                // 3. Call activate.bat with the env path
                // 4. cd to project folder
                args = $"/k \"set CONDA_SKIPCHECK=1 && for /f \"delims=\" %i in ('\"{pythonEnvUtils}\"') do @call \"{activateBat}\" \"%i\" && cd /d \"{projectDir}\"\"";
            }
            else
            {
                // Fallback: just open terminal at project folder
                args = $"/k cd /d \"{projectDir}\"";
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
    }
}
