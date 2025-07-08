using Microsoft.Azure.Kinect.Sensor;
using System.Numerics;

namespace KinectCore.Models
{
    /// <summary>
    /// Represents a 3D point with color information
    /// </summary>
    public struct ColoredPoint3D
    {
        public Vector3 Position { get; set; }
        public Vector3 Color { get; set; } // RGB values 0-1
        public Vector3 Normal { get; set; }
        
        public ColoredPoint3D(Vector3 position, Vector3 color, Vector3 normal = default)
        {
            Position = position;
            Color = color;
            Normal = normal;
        }
    }

    /// <summary>
    /// Represents a scan frame containing depth, color, and point cloud data
    /// </summary>
    public class ScanFrame : IDisposable
    {
        public Image? DepthImage { get; set; }
        public Image? ColorImage { get; set; }
        public Image? TransformedColorImage { get; set; }
        public ColoredPoint3D[]? PointCloud { get; set; }
        public DateTime Timestamp { get; set; }
        public Matrix4x4 CameraPose { get; set; }
        
        public ScanFrame()
        {
            Timestamp = DateTime.UtcNow;
            CameraPose = Matrix4x4.Identity;
        }

        public void Dispose()
        {
            DepthImage?.Dispose();
            ColorImage?.Dispose();
            TransformedColorImage?.Dispose();
        }
    }

    /// <summary>
    /// Configuration for scanning operations
    /// </summary>
    public class ScanConfiguration
    {
        public DepthMode DepthMode { get; set; } = DepthMode.NFOV_Unbinned;
        public ColorResolution ColorResolution { get; set; } = ColorResolution.R1080p;
        public FPS FrameRate { get; set; } = FPS.FPS30;
        
        // Background removal settings
        public bool EnableBackgroundRemoval { get; set; } = true;
        public float BackgroundDistanceThreshold { get; set; } = 0.5f; // meters
        public int BackgroundRemovalFrames { get; set; } = 30; // frames to capture background
        
        // Point cloud filtering
        public float MinDepth { get; set; } = 0.3f; // meters
        public float MaxDepth { get; set; } = 3.0f; // meters
        public bool EnableDepthFiltering { get; set; } = true;
        public bool EnableNoiseReduction { get; set; } = true;
        
        // Scanning behavior
        public int FramesToCapture { get; set; } = 100;
        public float AngleStep { get; set; } = 3.6f; // degrees (100 frames = 360 degrees)
        public bool AutomaticRotation { get; set; } = false;
    }

    /// <summary>
    /// Scan session results
    /// </summary>
    public class ScanResult
    {
        public List<ScanFrame> Frames { get; set; } = new();
        public ColoredPoint3D[]? MergedPointCloud { get; set; }
        public ScanConfiguration Configuration { get; set; } = new();
        public DateTime ScanStartTime { get; set; }
        public DateTime ScanEndTime { get; set; }
        public string ScanId { get; set; } = Guid.NewGuid().ToString();
        
        public TimeSpan ScanDuration => ScanEndTime - ScanStartTime;
        public int TotalPoints => MergedPointCloud?.Length ?? 0;
    }
}
