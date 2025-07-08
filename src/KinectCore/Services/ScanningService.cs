using KinectCore.Models;
using System.Numerics;

namespace KinectCore.Services
{
    /// <summary>
    /// Service for managing scanning sessions and combining multiple frames
    /// </summary>
    public class ScanningService
    {
        private readonly KinectCameraService _kinectService;
        private ScanResult? _currentScan;
        private readonly object _lockObject = new();

        public event EventHandler<ScanFrame>? FrameProcessed;
        public event EventHandler<ScanResult>? ScanCompleted;
        public event EventHandler<string>? StatusUpdated;

        public ScanningService(KinectCameraService kinectService)
        {
            _kinectService = kinectService;
            _kinectService.FrameCaptured += OnFrameCaptured;
        }

        /// <summary>
        /// Start a new scanning session
        /// </summary>
        public async Task<bool> StartScanAsync(ScanConfiguration config)
        {
            try
            {
                StatusUpdated?.Invoke(this, "Initializing scan...");

                lock (_lockObject)
                {
                    _currentScan = new ScanResult
                    {
                        Configuration = config,
                        ScanStartTime = DateTime.UtcNow
                    };
                }

                // Initialize camera if not already done
                if (!_kinectService.IsConnected)
                {
                    StatusUpdated?.Invoke(this, "Connecting to camera...");
                    if (!await _kinectService.InitializeAsync(config))
                    {
                        StatusUpdated?.Invoke(this, "Failed to connect to camera");
                        return false;
                    }
                }

                // Capture background if enabled
                if (config.EnableBackgroundRemoval)
                {
                    StatusUpdated?.Invoke(this, "Capturing background...");
                    await _kinectService.CaptureBackgroundAsync(config.BackgroundRemovalFrames);
                }

                StatusUpdated?.Invoke(this, "Starting capture...");
                _kinectService.StartCapture();

                return true;
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke(this, $"Scan initialization failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop current scanning session
        /// </summary>
        public async Task<ScanResult?> StopScanAsync()
        {
            _kinectService.StopCapture();

            if (_currentScan == null)
                return null;

            StatusUpdated?.Invoke(this, "Processing scan data...");

            lock (_lockObject)
            {
                _currentScan.ScanEndTime = DateTime.UtcNow;
            }

            // Merge all point clouds
            await Task.Run(() => MergePointClouds());

            var result = _currentScan;
            _currentScan = null;

            StatusUpdated?.Invoke(this, "Scan completed");
            ScanCompleted?.Invoke(this, result);

            return result;
        }

        /// <summary>
        /// Capture a single frame manually
        /// </summary>
        public async Task<ScanFrame?> CaptureFrameAsync()
        {
            if (!_kinectService.IsConnected)
                return null;

            var capture = await _kinectService.CaptureFrameAsync();
            if (capture == null)
                return null;

            try
            {
                // Process capture and create frame with copied data
                var scanFrame = _kinectService.ProcessCapture(capture, _currentScan?.Configuration.EnableBackgroundRemoval ?? true);
                return scanFrame;
            }
            finally
            {
                // Always dispose the capture after processing
                capture.Dispose();
            }
        }

        /// <summary>
        /// Get current scan progress
        /// </summary>
        public ScanProgress GetScanProgress()
        {
            if (_currentScan == null)
                return new ScanProgress();

            lock (_lockObject)
            {
                var targetFrames = _currentScan.Configuration.FramesToCapture;
                var currentFrames = _currentScan.Frames.Count;
                
                return new ScanProgress
                {
                    CurrentFrame = currentFrames,
                    TargetFrames = targetFrames,
                    ProgressPercentage = targetFrames > 0 ? (currentFrames * 100.0 / targetFrames) : 0,
                    ElapsedTime = DateTime.UtcNow - _currentScan.ScanStartTime,
                    TotalPoints = _currentScan.Frames.Sum(f => f.PointCloud?.Length ?? 0)
                };
            }
        }

        private void OnFrameCaptured(object? sender, ScanFrame frame)
        {
            if (_currentScan == null)
                return;

            lock (_lockObject)
            {
                // Add frame to current scan
                _currentScan.Frames.Add(frame);

                // Check if we should automatically stop
                if (_currentScan.Frames.Count >= _currentScan.Configuration.FramesToCapture)
                {
                    Task.Run(async () => await StopScanAsync());
                }
            }

            FrameProcessed?.Invoke(this, frame);
        }

        /// <summary>
        /// Merge all captured point clouds into a single cloud
        /// </summary>
        private void MergePointClouds()
        {
            if (_currentScan?.Frames == null || _currentScan.Frames.Count == 0)
                return;

            var allPoints = new List<ColoredPoint3D>();

            foreach (var frame in _currentScan.Frames)
            {
                if (frame.PointCloud != null)
                {
                    // Apply any transformations if needed (rotation, translation)
                    var transformedPoints = ApplyFrameTransformation(frame.PointCloud, frame.CameraPose);
                    allPoints.AddRange(transformedPoints);
                }
            }

            // Remove duplicate points and apply filtering
            var filteredPoints = FilterAndCleanPointCloud(allPoints);

            _currentScan.MergedPointCloud = filteredPoints.ToArray();
        }

        /// <summary>
        /// Apply transformation matrix to point cloud
        /// </summary>
        private ColoredPoint3D[] ApplyFrameTransformation(ColoredPoint3D[] points, Matrix4x4 transform)
        {
            if (transform == Matrix4x4.Identity)
                return points;

            var transformedPoints = new ColoredPoint3D[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                var transformed = Vector3.Transform(points[i].Position, transform);
                transformedPoints[i] = new ColoredPoint3D(transformed, points[i].Color, points[i].Normal);
            }

            return transformedPoints;
        }

        /// <summary>
        /// Filter and clean merged point cloud
        /// </summary>
        private List<ColoredPoint3D> FilterAndCleanPointCloud(List<ColoredPoint3D> points)
        {
            if (points.Count == 0)
                return points;

            // Remove duplicates (points within 1mm of each other)
            var filteredPoints = new List<ColoredPoint3D>();
            const float duplicateThreshold = 0.001f; // 1mm

            foreach (var point in points)
            {
                bool isDuplicate = filteredPoints.Any(existing => 
                    Vector3.Distance(existing.Position, point.Position) < duplicateThreshold);

                if (!isDuplicate)
                {
                    filteredPoints.Add(point);
                }
            }

            // Apply statistical outlier removal
            filteredPoints = RemoveOutliers(filteredPoints);

            return filteredPoints;
        }

        /// <summary>
        /// Remove statistical outliers from point cloud
        /// </summary>
        private List<ColoredPoint3D> RemoveOutliers(List<ColoredPoint3D> points)
        {
            if (points.Count < 10)
                return points;

            // Calculate mean distance to neighbors for each point
            var distances = new List<float>();
            const int neighborCount = 10;

            foreach (var point in points)
            {
                var nearestDistances = points
                    .Where(p => p.Position != point.Position)
                    .Select(p => Vector3.Distance(p.Position, point.Position))
                    .OrderBy(d => d)
                    .Take(neighborCount)
                    .ToList();

                if (nearestDistances.Count > 0)
                {
                    distances.Add(nearestDistances.Average());
                }
            }

            // Calculate statistics
            var meanDistance = distances.Average();
            var variance = distances.Select(d => Math.Pow(d - meanDistance, 2)).Average();
            var stdDev = Math.Sqrt(variance);
            var threshold = meanDistance + 2 * stdDev; // 2 standard deviations

            // Filter points
            var filteredPoints = new List<ColoredPoint3D>();
            for (int i = 0; i < points.Count && i < distances.Count; i++)
            {
                if (distances[i] <= threshold)
                {
                    filteredPoints.Add(points[i]);
                }
            }

            return filteredPoints;
        }
    }

    /// <summary>
    /// Progress information for active scan
    /// </summary>
    public class ScanProgress
    {
        public int CurrentFrame { get; set; }
        public int TargetFrames { get; set; }
        public double ProgressPercentage { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public int TotalPoints { get; set; }
    }
}
