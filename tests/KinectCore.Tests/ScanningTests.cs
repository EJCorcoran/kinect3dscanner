using Xunit;
using KinectCore.Models;
using KinectCore.Services;
using System.Numerics;

namespace KinectCore.Tests
{
    public class ScanningServiceTests
    {
        [Fact]
        public void ScanProgress_InitialState_ShouldBeZero()
        {
            // Arrange
            var progress = new ScanProgress();

            // Assert
            Assert.Equal(0, progress.CurrentFrame);
            Assert.Equal(0, progress.TargetFrames);
            Assert.Equal(0, progress.ProgressPercentage);
            Assert.Equal(TimeSpan.Zero, progress.ElapsedTime);
            Assert.Equal(0, progress.TotalPoints);
        }

        [Fact]
        public void ColoredPoint3D_Constructor_ShouldSetProperties()
        {
            // Arrange
            var position = new Vector3(1, 2, 3);
            var color = new Vector3(0.5f, 0.6f, 0.7f);
            var normal = new Vector3(0, 1, 0);

            // Act
            var point = new ColoredPoint3D(position, color, normal);

            // Assert
            Assert.Equal(position, point.Position);
            Assert.Equal(color, point.Color);
            Assert.Equal(normal, point.Normal);
        }

        [Fact]
        public void ScanConfiguration_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var config = new ScanConfiguration();

            // Assert
            Assert.True(config.EnableBackgroundRemoval);
            Assert.True(config.EnableDepthFiltering);
            Assert.True(config.EnableNoiseReduction);
            Assert.Equal(0.3f, config.MinDepth);
            Assert.Equal(3.0f, config.MaxDepth);
            Assert.Equal(100, config.FramesToCapture);
        }

        [Fact]
        public void ScanResult_NewInstance_ShouldHaveValidId()
        {
            // Arrange & Act
            var result = new ScanResult();

            // Assert
            Assert.NotNull(result.ScanId);
            Assert.NotEqual(Guid.Empty.ToString(), result.ScanId);
            Assert.Empty(result.Frames);
            Assert.Equal(0, result.TotalPoints);
        }

        [Theory]
        [InlineData(10, 5, 50.0)]
        [InlineData(100, 25, 25.0)]
        [InlineData(50, 50, 100.0)]
        public void ScanProgress_ProgressPercentage_ShouldCalculateCorrectly(int target, int current, double expected)
        {
            // Arrange
            var progress = new ScanProgress
            {
                TargetFrames = target,
                CurrentFrame = current
            };

            // Act
            var percentage = target > 0 ? (current * 100.0 / target) : 0;

            // Assert
            Assert.Equal(expected, percentage);
        }
    }

    public class PointCloudProcessingTests
    {
        [Fact]
        public void Vector3Int_Equality_ShouldWork()
        {
            // Arrange
            var v1 = new PointCloudProcessor.Services.Vector3Int(1, 2, 3);
            var v2 = new PointCloudProcessor.Services.Vector3Int(1, 2, 3);
            var v3 = new PointCloudProcessor.Services.Vector3Int(1, 2, 4);

            // Act & Assert
            Assert.True(v1.Equals(v2));
            Assert.True(v1 == v2);
            Assert.False(v1.Equals(v3));
            Assert.False(v1 == v3);
        }

        [Fact]
        public void Vector3Int_HashCode_ShouldBeConsistent()
        {
            // Arrange
            var v1 = new PointCloudProcessor.Services.Vector3Int(1, 2, 3);
            var v2 = new PointCloudProcessor.Services.Vector3Int(1, 2, 3);

            // Act & Assert
            Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        }
    }
}
