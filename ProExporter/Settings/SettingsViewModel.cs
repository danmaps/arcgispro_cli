using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace ProExporter.Settings
{
    /// <summary>
    /// ViewModel for the Settings property page
    /// </summary>
    public class SettingsViewModel : Page
    {
        private bool _autoExportEnabled;
        private int _autoExportMaxLayers;
        private bool _autoExportLocalOnly;
        private bool _exportImages;
        private bool _exportNotebooks;
        private bool _exportFields;

        public SettingsViewModel()
        {
            // Load settings
            _autoExportEnabled = Properties.Settings.Default.AutoExportEnabled;
            _autoExportMaxLayers = Properties.Settings.Default.AutoExportMaxLayers;
            _autoExportLocalOnly = Properties.Settings.Default.AutoExportLocalOnly;
            _exportImages = Properties.Settings.Default.ExportImages;
            _exportNotebooks = Properties.Settings.Default.ExportNotebooks;
            _exportFields = Properties.Settings.Default.ExportFields;
        }

        #region Properties

        public bool AutoExportEnabled
        {
            get => _autoExportEnabled;
            set => SetProperty(ref _autoExportEnabled, value);
        }

        public int AutoExportMaxLayers
        {
            get => _autoExportMaxLayers;
            set => SetProperty(ref _autoExportMaxLayers, value);
        }

        public bool AutoExportLocalOnly
        {
            get => _autoExportLocalOnly;
            set => SetProperty(ref _autoExportLocalOnly, value);
        }

        public bool ExportImages
        {
            get => _exportImages;
            set => SetProperty(ref _exportImages, value);
        }

        public bool ExportNotebooks
        {
            get => _exportNotebooks;
            set => SetProperty(ref _exportNotebooks, value);
        }

        public bool ExportFields
        {
            get => _exportFields;
            set => SetProperty(ref _exportFields, value);
        }

        public string LastExportText
        {
            get
            {
                var lastExport = Properties.Settings.Default.LastExportTime;
                if (lastExport == DateTime.MinValue)
                    return "No exports yet";

                var elapsed = DateTime.Now - lastExport;
                if (elapsed.TotalMinutes < 1)
                    return $"Last export: {lastExport:g} (just now)";
                if (elapsed.TotalMinutes < 60)
                    return $"Last export: {lastExport:g} ({(int)elapsed.TotalMinutes} minutes ago)";
                if (elapsed.TotalHours < 24)
                    return $"Last export: {lastExport:g} ({(int)elapsed.TotalHours} hours ago)";
                return $"Last export: {lastExport:g}";
            }
        }

        #endregion

        #region Commands

        public ICommand ExportNowCommand => new RelayCommand(async () =>
        {
            if (!ExportController.CanExport())
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    "Please open a project first.", "ArcGIS Pro CLI",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var options = new ExportOptions
            {
                ExportImages = ExportImages,
                ExportNotebooks = ExportNotebooks,
                ExportFields = ExportFields
            };

            var controller = new ExportController();
            var result = await controller.RunSnapshotAsync(options);

            if (result.Success)
            {
                Properties.Settings.Default.LastExportTime = DateTime.Now;
                Properties.Settings.Default.Save();
                NotifyPropertyChanged(nameof(LastExportText));

                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    $"Export completed!\nFiles: {result.FilesCreated.Count}",
                    "ArcGIS Pro CLI",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    $"Export failed:\n{string.Join("\n", result.Errors)}",
                    "ArcGIS Pro CLI",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        });

        public ICommand OpenFolderCommand => new RelayCommand(() =>
        {
            if (!ExportController.CanExport())
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    "Please open a project first.", "ArcGIS Pro CLI",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            ExportController.OpenOutputFolder();
        });

        #endregion

        #region Page Overrides

        protected override Task CommitAsync()
        {
            // Save settings when OK is clicked
            Properties.Settings.Default.AutoExportEnabled = AutoExportEnabled;
            Properties.Settings.Default.AutoExportMaxLayers = AutoExportMaxLayers;
            Properties.Settings.Default.AutoExportLocalOnly = AutoExportLocalOnly;
            Properties.Settings.Default.ExportImages = ExportImages;
            Properties.Settings.Default.ExportNotebooks = ExportNotebooks;
            Properties.Settings.Default.ExportFields = ExportFields;
            Properties.Settings.Default.Save();

            return Task.CompletedTask;
        }

        protected override Task CancelAsync()
        {
            // Reload settings on cancel
            _autoExportEnabled = Properties.Settings.Default.AutoExportEnabled;
            _autoExportMaxLayers = Properties.Settings.Default.AutoExportMaxLayers;
            _autoExportLocalOnly = Properties.Settings.Default.AutoExportLocalOnly;
            _exportImages = Properties.Settings.Default.ExportImages;
            _exportNotebooks = Properties.Settings.Default.ExportNotebooks;
            _exportFields = Properties.Settings.Default.ExportFields;

            return Task.CompletedTask;
        }

        #endregion
    }
}
