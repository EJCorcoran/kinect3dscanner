using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KinectCore.Services;
using KinectCore.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading.Tasks;

namespace Scanner.UI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly KinectCameraService _kinectService;
        private readonly ScanningService _scanningService;

        [ObservableProperty]
        private bool _isConnected;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private ScanConfiguration _scanConfig = new();

        [ObservableProperty]
        private ScanProgress _scanProgress = new();

        [ObservableProperty]
        private ObservableCollection<ScanResult> _scanHistory = new();

        public MainWindowViewModel(KinectCameraService kinectService, ScanningService scanningService)
        {
            _kinectService = kinectService;
            _scanningService = scanningService;

            // Subscribe to events
            _kinectService.ErrorOccurred += OnErrorOccurred;
            _scanningService.StatusUpdated += OnStatusUpdated;
            _scanningService.ScanCompleted += OnScanCompleted;
            _scanningService.FrameProcessed += OnFrameProcessed;
        }

        [RelayCommand]
        private async Task ConnectCameraAsync()
        {
            StatusMessage = "Connecting to Azure Kinect...";
            
            IsConnected = await _kinectService.InitializeAsync(ScanConfig);
            
            if (IsConnected)
            {
                StatusMessage = "Azure Kinect connected successfully";
            }
            else
            {
                StatusMessage = "Failed to connect to Azure Kinect";
            }
        }

        [RelayCommand]
        private async Task StartScanAsync()
        {
            if (!IsConnected)
            {
                await ConnectCameraAsync();
                if (!IsConnected) return;
            }

            IsScanning = await _scanningService.StartScanAsync(ScanConfig);
        }

        [RelayCommand]
        private async Task StopScanAsync()
        {
            if (!IsScanning) return;

            var result = await _scanningService.StopScanAsync();
            if (result != null)
            {
                ScanHistory.Add(result);
            }
            
            IsScanning = false;
        }

        [RelayCommand]
        private async Task CaptureBackgroundAsync()
        {
            if (!IsConnected) return;

            StatusMessage = "Capturing background...";
            await _kinectService.CaptureBackgroundAsync(ScanConfig.BackgroundRemovalFrames);
            StatusMessage = "Background captured";
        }

        [RelayCommand]
        private void OpenSettings()
        {
            // TODO: Implement settings dialog
            StatusMessage = "Settings dialog not yet implemented";
            // var settingsWindow = new Views.SettingsWindow();
            // settingsWindow.ShowDialog();
        }

        [RelayCommand]
        private void PreviewScan(ScanResult scanResult)
        {
            // TODO: Implement preview window
            StatusMessage = "Preview window not yet implemented";
            // var previewWindow = new Views.PreviewWindow(scanResult);
            // previewWindow.Show();
        }

        [RelayCommand]
        private void ExportScan(ScanResult scanResult)
        {
            // TODO: Implement export dialog
            StatusMessage = "Export dialog not yet implemented";
            // var exportDialog = new Views.ExportDialog(scanResult);
            // exportDialog.ShowDialog();
        }

        private void OnErrorOccurred(object? sender, string error)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"Error: {error}";
            });
        }

        private void OnStatusUpdated(object? sender, string status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = status;
            });
        }

        private void OnScanCompleted(object? sender, ScanResult result)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsScanning = false;
                StatusMessage = $"Scan completed: {result.TotalPoints:N0} points";
            });
        }

        private void OnFrameProcessed(object? sender, ScanFrame frame)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ScanProgress = _scanningService.GetScanProgress();
            });
        }
    }
}
