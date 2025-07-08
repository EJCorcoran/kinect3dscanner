using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KinectCore.Services;
using KinectCore.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System;
using System.IO;

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

        [ObservableProperty]
        private ImageSource? _colorImageSource;

        [ObservableProperty]
        private ImageSource? _depthImageSource;

        private bool _isPreviewRunning;

        public MainWindowViewModel(KinectCameraService kinectService, ScanningService scanningService)
        {
            _kinectService = kinectService;
            _scanningService = scanningService;

            // Subscribe to events
            _kinectService.ErrorOccurred += OnErrorOccurred;
            _kinectService.FrameCaptured += OnFrameCaptured;
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
                // Start live preview
                await StartLivePreviewAsync();
            }
            else
            {
                StatusMessage = "Failed to connect to Azure Kinect";
            }
        }

        private async Task StartLivePreviewAsync()
        {
            if (_isPreviewRunning) return;
            
            _isPreviewRunning = true;
            StatusMessage = "Starting live preview...";
            
            try
            {
                await _kinectService.StartLivePreviewAsync();
                StatusMessage = "Live preview active";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Preview error: {ex.Message}";
                _isPreviewRunning = false;
            }
        }

        private void StopLivePreview()
        {
            if (!_isPreviewRunning) return;
            
            _isPreviewRunning = false;
            _kinectService.StopLivePreview();
            
            // Clear preview images
            ColorImageSource = null;
            DepthImageSource = null;
            StatusMessage = "Live preview stopped";
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
        private void DisconnectCamera()
        {
            if (!IsConnected) return;

            StopLivePreview();
            _kinectService.Dispose();
            IsConnected = false;
            StatusMessage = "Camera disconnected";
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow()
            {
                Owner = Application.Current.MainWindow
            };
            
            var result = settingsWindow.ShowDialog();
            if (result == true && settingsWindow.SettingsChanged)
            {
                StatusMessage = "Settings updated";
                // TODO: Apply new settings to scanner configuration
            }
        }

        private void OnFrameCaptured(object? sender, ScanFrame frame)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Update color image
                    if (frame.ColorImage != null)
                    {
                        ColorImageSource = ConvertToImageSource(frame.ColorImage, false);
                    }

                    // Update depth image  
                    if (frame.DepthImage != null)
                    {
                        DepthImageSource = ConvertToImageSource(frame.DepthImage, true);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Frame display error: {ex.Message}";
                }
                finally
                {
                    // Dispose frame to clean up copied images
                    frame?.Dispose();
                }
            });
        }

        private ImageSource? ConvertToImageSource(Microsoft.Azure.Kinect.Sensor.Image kinectImage, bool isDepth)
        {
            try
            {
                var width = kinectImage.WidthPixels;
                var height = kinectImage.HeightPixels;
                
                // Copy image data immediately to avoid disposal issues
                var sourceData = kinectImage.Memory.ToArray();
                
                if (isDepth)
                {
                    // Convert depth image (16-bit) to 8-bit grayscale
                    var pixelData = new byte[width * height];
                    
                    for (int i = 0; i < sourceData.Length; i += 2)
                    {
                        if (i + 1 < sourceData.Length)
                        {
                            var depth = BitConverter.ToUInt16(sourceData, i);
                            // Scale depth to 0-255 range (assuming max depth of 5000mm)
                            var scaled = Math.Min(255, (depth * 255) / 5000);
                            pixelData[i / 2] = (byte)scaled;
                        }
                    }
                    
                    var bitmap = BitmapSource.Create(width, height, 96, 96, 
                        PixelFormats.Gray8, null, pixelData, width);
                    bitmap.Freeze(); // Allow cross-thread access
                    return bitmap;
                }
                else
                {
                    // Convert color image (BGRA32)
                    var bitmap = BitmapSource.Create(width, height, 96, 96,
                        PixelFormats.Bgra32, null, sourceData, width * 4);
                    bitmap.Freeze(); // Allow cross-thread access
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash UI
                System.Diagnostics.Debug.WriteLine($"Image conversion error: {ex.Message}");
                return null;
            }
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
