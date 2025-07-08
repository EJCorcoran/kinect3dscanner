using Microsoft.Azure.Kinect.Sensor;
using KinectCore.Models;
using System.Numerics;

namespace KinectCore.Services
{
    /// <summary>
    /// Core service for Azure Kinect camera operations
    /// </summary>
    public class KinectCameraService : IDisposable
    {
        private Device? _device;
        private Transformation? _transformation;
        private Image? _backgroundDepthImage;
        private bool _isRunning;
        private readonly object _lockObject = new();

        public event EventHandler<ScanFrame>? FrameCaptured;
        public event EventHandler<string>? ErrorOccurred;
        
#pragma warning disable CS0067 // Event is never used - part of public interface
        public event EventHandler? DeviceDisconnected;
#pragma warning restore CS0067

        public bool IsConnected => _device != null;
        public bool IsCapturing => _isRunning;

        /// <summary>
        /// Initialize and connect to Azure Kinect device
        /// </summary>
        public Task<bool> InitializeAsync(ScanConfiguration config)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Check for available devices
                    var deviceCount = Device.GetInstalledCount();
                    if (deviceCount == 0)
                    {
                        ErrorOccurred?.Invoke(this, "No Azure Kinect devices found");
                        return false;
                    }

                    // Open the first device
                    _device = Device.Open(0);
                
                // Configure device
                var deviceConfig = new DeviceConfiguration
                {
                    DepthMode = config.DepthMode,
                    ColorFormat = ImageFormat.ColorBGRA32,
                    ColorResolution = config.ColorResolution,
                    CameraFPS = config.FrameRate,
                    SynchronizedImagesOnly = true
                };

                // Start cameras
                _device.StartCameras(deviceConfig);
                
                // Create transformation for depth-to-color alignment
                var calibration = _device.GetCalibration(config.DepthMode, config.ColorResolution);
                _transformation = calibration.CreateTransformation();

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Failed to initialize Kinect: {ex.Message}");
                return false;
            }
            });
        }

        /// <summary>
        /// Capture background for removal
        /// </summary>
        public async Task CaptureBackgroundAsync(int frameCount = 30)
        {
            if (_device == null)
                throw new InvalidOperationException("Device not initialized");

            var depthImages = new List<Image>();
            
            for (int i = 0; i < frameCount; i++)
            {
                using var capture = await CaptureFrameAsync();
                if (capture?.Depth != null)
                {
                    depthImages.Add(capture.Depth.Reference());
                }
                await Task.Delay(33); // ~30 FPS
            }

            // Create median background image
            _backgroundDepthImage = CreateMedianDepthImage(depthImages);
            
            // Cleanup
            foreach (var img in depthImages)
                img.Dispose();
        }

        /// <summary>
        /// Start continuous frame capture
        /// </summary>
        public void StartCapture()
        {
            if (_device == null)
                throw new InvalidOperationException("Device not initialized");

            _isRunning = true;
            Task.Run(CaptureLoop);
        }

        /// <summary>
        /// Stop frame capture
        /// </summary>
        public void StopCapture()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Capture a single frame
        /// </summary>
        public async Task<Capture?> CaptureFrameAsync()
        {
            if (_device == null)
                return null;

            try
            {
                // Return the capture without disposing it - caller is responsible for disposal
                var capture = await Task.Run(() => _device.GetCapture());
                return capture;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Frame capture failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert capture to scan frame with point cloud
        /// </summary>
        public ScanFrame ProcessCapture(Capture capture, bool removeBackground = true)
        {
            if (_transformation == null)
                throw new InvalidOperationException("Transformation not initialized");

            var scanFrame = new ScanFrame();

            try
            {
                // Create independent copies of image data to avoid disposal issues
                if (capture.Depth != null)
                {
                    scanFrame.DepthImage = CreateImageCopy(capture.Depth);
                }

                if (capture.Color != null)
                {
                    scanFrame.ColorImage = CreateImageCopy(capture.Color);
                }

                if (capture.Depth != null && capture.Color != null)
                {
                    // Transform color to depth camera space
                    using var transformedColor = _transformation.ColorImageToDepthCamera(capture);
                    scanFrame.TransformedColorImage = CreateImageCopy(transformedColor);

                    // Generate point cloud
                    scanFrame.PointCloud = GeneratePointCloud(
                        capture.Depth, 
                        transformedColor, 
                        removeBackground);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Frame processing failed: {ex.Message}");
            }

            return scanFrame;
        }

        /// <summary>
        /// Create an independent copy of a Kinect image to avoid disposal issues
        /// </summary>
        private Microsoft.Azure.Kinect.Sensor.Image CreateImageCopy(Microsoft.Azure.Kinect.Sensor.Image source)
        {
            try
            {
                var copy = new Microsoft.Azure.Kinect.Sensor.Image(
                    source.Format,
                    source.WidthPixels,
                    source.HeightPixels,
                    source.StrideBytes);

                source.Memory.CopyTo(copy.Memory);
                return copy;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Image copy failed: {ex.Message}");
                // Return a dummy image to prevent crashes
                return new Microsoft.Azure.Kinect.Sensor.Image(
                    source.Format,
                    source.WidthPixels,
                    source.HeightPixels,
                    source.StrideBytes);
            }
        }

        /// <summary>
        /// Generate colored point cloud from depth and color images
        /// </summary>
        private ColoredPoint3D[] GeneratePointCloud(Image depthImage, Image colorImage, bool removeBackground)
        {
            var points = new List<ColoredPoint3D>();
            var depthSpan = depthImage.GetPixels<ushort>().Span;
            var colorSpan = colorImage.GetPixels<byte>().Span;

            int width = depthImage.WidthPixels;
            int height = depthImage.HeightPixels;

            // Camera intrinsics (simplified - should be from calibration)
            float fx = 504.0f; // focal length x
            float fy = 504.0f; // focal length y
            float cx = width / 2.0f; // principal point x
            float cy = height / 2.0f; // principal point y

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = y * width + x;
                    ushort depth = depthSpan[pixelIndex];

                    if (depth == 0) continue;

                    // Convert depth to meters
                    float z = depth / 1000.0f;

                    // Skip if background removal is enabled and point is part of background
                    if (removeBackground && _backgroundDepthImage != null)
                    {
                        var bgDepthSpan = _backgroundDepthImage.GetPixels<ushort>().Span;
                        ushort bgDepth = bgDepthSpan[pixelIndex];
                        if (bgDepth > 0 && Math.Abs(depth - bgDepth) < 50) // 5cm threshold
                            continue;
                    }

                    // Calculate 3D position
                    float worldX = (x - cx) * z / fx;
                    float worldY = (y - cy) * z / fy;

                    var position = new Vector3(worldX, worldY, z);

                    // Get color
                    int colorIndex = pixelIndex * 4; // BGRA format
                    if (colorIndex + 3 < colorSpan.Length)
                    {
                        byte b = colorSpan[colorIndex];
                        byte g = colorSpan[colorIndex + 1];
                        byte r = colorSpan[colorIndex + 2];
                        
                        var color = new Vector3(r / 255.0f, g / 255.0f, b / 255.0f);
                        
                        points.Add(new ColoredPoint3D(position, color));
                    }
                }
            }

            return points.ToArray();
        }

        /// <summary>
        /// Create median depth image from multiple frames for background removal
        /// </summary>
        private Image CreateMedianDepthImage(List<Image> depthImages)
        {
            if (depthImages.Count == 0)
                throw new ArgumentException("No depth images provided");

            var firstImage = depthImages[0];
            int width = firstImage.WidthPixels;
            int height = firstImage.HeightPixels;
            
            var medianImage = new ushort[width * height];
            var pixelValues = new List<ushort>();

            for (int i = 0; i < width * height; i++)
            {
                pixelValues.Clear();
                
                foreach (var image in depthImages)
                {
                    var span = image.GetPixels<ushort>().Span;
                    if (span[i] > 0) // Only consider valid depth values
                        pixelValues.Add(span[i]);
                }

                if (pixelValues.Count > 0)
                {
                    pixelValues.Sort();
                    medianImage[i] = pixelValues[pixelValues.Count / 2];
                }
            }

            // Create new Image with median data
            // For now, return the original image - proper median filtering requires
            // more complex memory management with Azure Kinect SDK
            return firstImage;
        }

        /// <summary>
        /// Continuous capture loop
        /// </summary>
        private async Task CaptureLoop()
        {
            while (_isRunning && _device != null)
            {
                try
                {
                    var capture = await CaptureFrameAsync();
                    if (capture != null)
                    {
                        // Process capture and create frame with copied data before disposal
                        var scanFrame = ProcessCapture(capture);
                        
                        // Dispose capture immediately after processing
                        capture.Dispose();
                        
                        // Fire event with the frame containing copied data
                        FrameCaptured?.Invoke(this, scanFrame);
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"Capture loop error: {ex.Message}");
                    await Task.Delay(100); // Brief pause before retry
                }
            }
        }

        /// <summary>
        /// Start live preview for UI display
        /// </summary>
        public async Task StartLivePreviewAsync()
        {
            if (_device == null)
            {
                throw new InvalidOperationException("Device not initialized");
            }

            if (_isRunning)
            {
                return; // Already running
            }

            _isRunning = true;
            await Task.Run(async () => await CaptureLoop());
        }

        /// <summary>
        /// Stop live preview
        /// </summary>
        public void StopLivePreview()
        {
            _isRunning = false;
        }

        public void Dispose()
        {
            StopCapture();
            _backgroundDepthImage?.Dispose();
            _transformation?.Dispose();
            _device?.StopCameras();
            _device?.Dispose();
        }
    }
}
