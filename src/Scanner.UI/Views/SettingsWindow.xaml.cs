using Microsoft.Win32;
using System;
using System.Windows;

namespace Scanner.UI.Views
{
    public partial class SettingsWindow : Window
    {
        public bool SettingsChanged { get; private set; }

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            // Load current settings from configuration or defaults
            // This is a simplified implementation
            
            // Set default values if needed
            ExportPathTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\3DScans";
        }

        private void BrowseExportPath_Click(object sender, RoutedEventArgs e)
        {
            // Simple folder selection using SaveFileDialog as a workaround
            var dialog = new SaveFileDialog()
            {
                Title = "Select Export Folder",
                FileName = "folder", // Default filename
                DefaultExt = "", // No extension
                Filter = "Folder|*.", // Filter that shows all files
                CheckFileExists = false,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                // Get the directory from the selected path
                var folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    ExportPathTextBox.Text = folderPath;
                }
            }
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            // Reset all controls to default values
            DepthModeCombo.SelectedIndex = 0;
            ColorResolutionCombo.SelectedIndex = 1;
            FrameRateCombo.SelectedIndex = 2;
            
            EnableBackgroundRemovalCheck.IsChecked = true;
            EnableNoiseReductionCheck.IsChecked = true;
            EnableMeshSmoothingCheck.IsChecked = true;
            
            MaxDistanceSlider.Value = 3.0;
            FramesToCaptureSlider.Value = 100;
            
            ExportPathTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\3DScans";
            AutoExportCheck.IsChecked = true;
            ExportPlyCheck.IsChecked = true;
            ExportStlCheck.IsChecked = true;
            ExportObjCheck.IsChecked = false;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // Save settings here
            // This would typically save to a configuration file or registry
            SettingsChanged = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SettingsChanged = false;
            DialogResult = false;
            Close();
        }
    }
}
