using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;

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
                var options = RibbonExportFilterState.ApplyTo(ExportOptions.Default);
                var result = await controller.RunSnapshotAsync(options);

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
                var options = RibbonExportFilterState.ApplyTo(ExportOptions.Default);
                var result = await _controller.RunContextExportAsync(options);

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
                var options = RibbonExportFilterState.ApplyTo(ExportOptions.Default);
                var result = await _controller.RunImageExportAsync(options);

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

            // Ensure the Python startup hook exists so any python process launched from this terminal
            // self-heals stale ArcGISProTemp* workspace paths.
            EnsurePythonStartupHook(projectDir);

            string args;
            if (File.Exists(activateBat) && File.Exists(pythonEnvUtils))
            {
                // Set ARCGISPRO_PID so arcpy connects to this running Pro session
                // Set ARCGISPRO_PROJECT_DIR and add .arcgispro/ to PYTHONPATH so Python auto-imports sitecustomize.py
                // This prevents "Directory does not exist" errors with stale temp paths.
                args = $"/k \"set ARCGISPRO_PID={proProcessId} && " +
                       $"set ARCGISPRO_PROJECT_DIR=\"{projectDir}\" && " +
                       $"set PYTHONPATH=\"{Path.Combine(projectDir, ".arcgispro")}\";%PYTHONPATH% && " +
                       $"set CONDA_SKIPCHECK=1 && " +
                       $"for /f \"delims=\" %i in ('\"{pythonEnvUtils}\"') do @call \"{activateBat}\" \"%i\" && " +
                       $"cd /d \"{projectDir}\"\"";
            }
            else
            {
                // Fallback: set PID and open at project folder
                args = $"/k \"set ARCGISPRO_PID={proProcessId} && " +
                       $"set ARCGISPRO_PROJECT_DIR=\"{projectDir}\" && " +
                       $"set PYTHONPATH=\"{Path.Combine(projectDir, ".arcgispro")}\";%PYTHONPATH% && " +
                       $"cd /d \"{projectDir}\"\"";
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

        /// <summary>
        /// Writes a Python sitecustomize.py into .arcgispro so any python process started from the Terminal
        /// can automatically repair stale workspace paths (ArcGISProTemp*) and prefer a stable project scratch.gdb.
        /// </summary>
        private void EnsurePythonStartupHook(string projectDir)
        {
            try
            {
                var arcgisproFolder = Path.Combine(projectDir, ".arcgispro");
                Directory.CreateDirectory(arcgisproFolder);

                var hookPath = Path.Combine(arcgisproFolder, "sitecustomize.py");

                // Keep this intentionally small, defensive, and silent.
                // - If arcpy isn't available, do nothing.
                // - If scratch/workspace points at a missing ArcGISProTemp* path, clear it.
                // - If we can, set scratch/workspace to <project>/scratch.gdb (create if missing).
                var code = """
# Auto-run hook for ArcGIS Pro Terminal sessions.
# Loaded automatically by Python if this folder is on PYTHONPATH.

import os


def _exists(p: str) -> bool:
    try:
        return bool(p) and os.path.exists(p)
    except Exception:
        return False


def _looks_like_stale_pro_temp(p: str) -> bool:
    try:
        if not p:
            return False
        s = str(p)
        return ("ArcGISProTemp" in s) and (not os.path.exists(s))
    except Exception:
        return False


def _main() -> None:
    try:
        import arcpy  # type: ignore
    except Exception:
        return

    project_dir = (os.environ.get("ARCGISPRO_PROJECT_DIR") or "").strip().strip('"')

    # 1) Clear stale ArcGISProTemp* references if present
    try:
        sw = getattr(arcpy.env, "scratchWorkspace", None)
        if _looks_like_stale_pro_temp(sw):
            arcpy.ClearEnvironment("scratchWorkspace")
    except Exception:
        pass

    try:
        ws = getattr(arcpy.env, "workspace", None)
        if _looks_like_stale_pro_temp(ws):
            arcpy.ClearEnvironment("workspace")
    except Exception:
        pass

    # 2) Prefer a stable, project-local scratch.gdb
    if project_dir and _exists(project_dir):
        scratch_gdb = os.path.join(project_dir, "scratch.gdb")
        try:
            if not arcpy.Exists(scratch_gdb):
                arcpy.management.CreateFileGDB(project_dir, "scratch.gdb")
            arcpy.env.scratchWorkspace = scratch_gdb
            arcpy.env.workspace = scratch_gdb
        except Exception:
            # Don't block user scripts if we can't create/use scratch.gdb.
            pass


try:
    _main()
except Exception:
    # Never crash Python startup.
    pass
""";

                File.WriteAllText(hookPath, code);
            }
            catch
            {
                // Non-critical - continue even if hook write fails
            }
        }
    }

    public class ActiveMapOnlyCheckBox : CheckBox
    {
        public ActiveMapOnlyCheckBox()
        {
            IsChecked = RibbonExportFilterState.ActiveMapOnly;
        }

        protected override void OnClick()
        {
            var next = !(IsChecked ?? false);
            IsChecked = next;
            RibbonExportFilterState.ActiveMapOnly = next;
        }

        protected override void OnUpdate()
        {
            var hasActiveMap = MapView.Active?.Map != null;
            Enabled = hasActiveMap;
            if (!hasActiveMap)
            {
                IsChecked = false;
                RibbonExportFilterState.ActiveMapOnly = false;
            }
        }
    }
}
