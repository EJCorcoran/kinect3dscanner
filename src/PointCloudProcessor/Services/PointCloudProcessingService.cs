using PointCloudProcessor.Models;
using System.Numerics;

namespace PointCloudProcessor.Services
{
    /// <summary>
    /// Service for advanced point cloud processing operations
    /// </summary>
    public class PointCloudProcessingService
    {
        /// <summary>
        /// Downsample point cloud using voxel grid filtering
        /// </summary>
        public ColoredPoint3D[] VoxelGridFilter(ColoredPoint3D[] points, float voxelSize = 0.005f)
        {
            if (points.Length == 0)
                return points;

            var voxelMap = new Dictionary<Vector3Int, List<ColoredPoint3D>>();

            // Group points into voxels
            foreach (var point in points)
            {
                var voxelCoord = new Vector3Int(
                    (int)Math.Floor(point.Position.X / voxelSize),
                    (int)Math.Floor(point.Position.Y / voxelSize),
                    (int)Math.Floor(point.Position.Z / voxelSize)
                );

                if (!voxelMap.ContainsKey(voxelCoord))
                    voxelMap[voxelCoord] = new List<ColoredPoint3D>();

                voxelMap[voxelCoord].Add(point);
            }

            // Average points in each voxel
            var filteredPoints = new List<ColoredPoint3D>();
            foreach (var voxelPoints in voxelMap.Values)
            {
                var avgPosition = Vector3.Zero;
                var avgColor = Vector3.Zero;
                var avgNormal = Vector3.Zero;

                foreach (var point in voxelPoints)
                {
                    avgPosition += point.Position;
                    avgColor += point.Color;
                    avgNormal += point.Normal;
                }

                avgPosition /= voxelPoints.Count;
                avgColor /= voxelPoints.Count;
                avgNormal /= voxelPoints.Count;

                if (avgNormal.Length() > 0)
                    avgNormal = Vector3.Normalize(avgNormal);

                filteredPoints.Add(new ColoredPoint3D(avgPosition, avgColor, avgNormal));
            }

            return filteredPoints.ToArray();
        }

        /// <summary>
        /// Estimate surface normals for point cloud
        /// </summary>
        public ColoredPoint3D[] EstimateNormals(ColoredPoint3D[] points, int neighborhoodSize = 20)
        {
            if (points.Length < 3)
                return points;

            var pointsWithNormals = new ColoredPoint3D[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                var point = points[i];
                var neighbors = FindNearestNeighbors(points, point.Position, neighborhoodSize);

                if (neighbors.Count >= 3)
                {
                    var normal = EstimateNormalFromNeighbors(neighbors);
                    pointsWithNormals[i] = new ColoredPoint3D(point.Position, point.Color, normal);
                }
                else
                {
                    pointsWithNormals[i] = point;
                }
            }

            return pointsWithNormals;
        }

        /// <summary>
        /// Remove statistical outliers from point cloud
        /// </summary>
        public ColoredPoint3D[] RemoveStatisticalOutliers(ColoredPoint3D[] points, int meanK = 20, double stdRatio = 2.0)
        {
            if (points.Length < meanK)
                return points;

            var distances = new double[points.Length];

            // Calculate mean distance to k-nearest neighbors for each point
            for (int i = 0; i < points.Length; i++)
            {
                var neighbors = FindNearestNeighbors(points, points[i].Position, meanK);
                var avgDistance = neighbors.Average(n => Vector3.Distance(points[i].Position, n));
                distances[i] = avgDistance;
            }

            // Calculate statistics
            var meanDistance = distances.Average();
            var variance = distances.Select(d => Math.Pow(d - meanDistance, 2)).Average();
            var stdDev = Math.Sqrt(variance);
            var threshold = meanDistance + stdRatio * stdDev;

            // Filter points
            var filteredPoints = new List<ColoredPoint3D>();
            for (int i = 0; i < points.Length; i++)
            {
                if (distances[i] <= threshold)
                {
                    filteredPoints.Add(points[i]);
                }
            }

            return filteredPoints.ToArray();
        }

        /// <summary>
        /// Smooth point cloud using Laplacian smoothing
        /// </summary>
        public ColoredPoint3D[] LaplacianSmoothing(ColoredPoint3D[] points, int iterations = 3, float lambda = 0.5f)
        {
            var smoothedPoints = new ColoredPoint3D[points.Length];
            Array.Copy(points, smoothedPoints, points.Length);

            for (int iter = 0; iter < iterations; iter++)
            {
                var newPositions = new Vector3[points.Length];

                for (int i = 0; i < smoothedPoints.Length; i++)
                {
                    var neighbors = FindNearestNeighbors(smoothedPoints, smoothedPoints[i].Position, 20);
                    
                    if (neighbors.Count > 0)
                    {
                        var avgPosition = Vector3.Zero;
                        foreach (var neighbor in neighbors)
                            avgPosition += neighbor;
                        avgPosition /= neighbors.Count;

                        // Apply smoothing
                        newPositions[i] = Vector3.Lerp(smoothedPoints[i].Position, avgPosition, lambda);
                    }
                    else
                    {
                        newPositions[i] = smoothedPoints[i].Position;
                    }
                }

                // Update positions
                for (int i = 0; i < smoothedPoints.Length; i++)
                {
                    smoothedPoints[i] = new ColoredPoint3D(
                        newPositions[i], 
                        smoothedPoints[i].Color, 
                        smoothedPoints[i].Normal);
                }
            }

            return smoothedPoints;
        }

        /// <summary>
        /// Merge multiple point clouds with ICP alignment
        /// </summary>
        public ColoredPoint3D[] MergePointClouds(List<ColoredPoint3D[]> pointClouds, bool useICP = true)
        {
            if (pointClouds.Count == 0)
                return Array.Empty<ColoredPoint3D>();

            if (pointClouds.Count == 1)
                return pointClouds[0];

            var mergedCloud = new List<ColoredPoint3D>(pointClouds[0]);

            for (int i = 1; i < pointClouds.Count; i++)
            {
                var currentCloud = pointClouds[i];

                if (useICP && mergedCloud.Count > 0 && currentCloud.Length > 0)
                {
                    // Apply ICP alignment
                    var transformation = PerformICP(
                        mergedCloud.Select(p => p.Position).ToArray(),
                        currentCloud.Select(p => p.Position).ToArray());

                    // Transform current cloud
                    currentCloud = TransformPointCloud(currentCloud, transformation);
                }

                mergedCloud.AddRange(currentCloud);
            }

            return mergedCloud.ToArray();
        }

        /// <summary>
        /// Find K-nearest neighbors for a given point
        /// </summary>
        private List<Vector3> FindNearestNeighbors(ColoredPoint3D[] points, Vector3 queryPoint, int k)
        {
            var distances = points
                .Select((p, i) => new { Point = p.Position, Distance = Vector3.Distance(queryPoint, p.Position), Index = i })
                .Where(x => x.Distance > 0) // Exclude the point itself
                .OrderBy(x => x.Distance)
                .Take(k)
                .Select(x => x.Point)
                .ToList();

            return distances;
        }

        /// <summary>
        /// Estimate normal vector from neighboring points
        /// </summary>
        private Vector3 EstimateNormalFromNeighbors(List<Vector3> neighbors)
        {
            if (neighbors.Count < 3)
                return Vector3.UnitY; // Default normal

            // Calculate centroid
            var centroid = Vector3.Zero;
            foreach (var neighbor in neighbors)
                centroid += neighbor;
            centroid /= neighbors.Count;

            // Build covariance matrix
            var covariance = new float[3, 3];
            foreach (var neighbor in neighbors)
            {
                var diff = neighbor - centroid;
                covariance[0, 0] += diff.X * diff.X;
                covariance[0, 1] += diff.X * diff.Y;
                covariance[0, 2] += diff.X * diff.Z;
                covariance[1, 1] += diff.Y * diff.Y;
                covariance[1, 2] += diff.Y * diff.Z;
                covariance[2, 2] += diff.Z * diff.Z;
            }

            // Fill symmetric elements
            covariance[1, 0] = covariance[0, 1];
            covariance[2, 0] = covariance[0, 2];
            covariance[2, 1] = covariance[1, 2];

            // Find eigenvector corresponding to smallest eigenvalue
            // This is a simplified approach - in practice, you'd use proper eigendecomposition
            var normal = EstimateSmallestEigenvector(covariance);
            
            return Vector3.Normalize(normal);
        }

        /// <summary>
        /// Simple estimation of smallest eigenvector (normal direction)
        /// </summary>
        private Vector3 EstimateSmallestEigenvector(float[,] matrix)
        {
            // Simplified power iteration method
            var vector = new Vector3(1, 1, 1);
            
            for (int i = 0; i < 10; i++) // Few iterations
            {
                var newVector = new Vector3(
                    matrix[0, 0] * vector.X + matrix[0, 1] * vector.Y + matrix[0, 2] * vector.Z,
                    matrix[1, 0] * vector.X + matrix[1, 1] * vector.Y + matrix[1, 2] * vector.Z,
                    matrix[2, 0] * vector.X + matrix[2, 1] * vector.Y + matrix[2, 2] * vector.Z
                );
                
                if (newVector.Length() > 0)
                    vector = Vector3.Normalize(newVector);
            }
            
            return vector;
        }

        /// <summary>
        /// Perform Iterative Closest Point (ICP) alignment
        /// </summary>
        private Matrix4x4 PerformICP(Vector3[] sourcePoints, Vector3[] targetPoints, int maxIterations = 20)
        {
            var transformation = Matrix4x4.Identity;
            var currentSource = (Vector3[])sourcePoints.Clone();

            for (int iter = 0; iter < maxIterations; iter++)
            {
                // Find closest points
                var correspondences = new List<(Vector3 source, Vector3 target)>();
                
                foreach (var sourcePoint in currentSource)
                {
                    var closestTarget = targetPoints
                        .OrderBy(t => Vector3.Distance(sourcePoint, t))
                        .First();
                    
                    correspondences.Add((sourcePoint, closestTarget));
                }

                // Calculate transformation
                var iterTransform = CalculateTransformation(correspondences);
                
                // Apply transformation
                for (int i = 0; i < currentSource.Length; i++)
                {
                    currentSource[i] = Vector3.Transform(currentSource[i], iterTransform);
                }

                transformation = Matrix4x4.Multiply(iterTransform, transformation);
                
                // Check convergence (simplified)
                if (iter > 0 && IsConverged(correspondences))
                    break;
            }

            return transformation;
        }

        /// <summary>
        /// Calculate transformation matrix from point correspondences
        /// </summary>
        private Matrix4x4 CalculateTransformation(List<(Vector3 source, Vector3 target)> correspondences)
        {
            if (correspondences.Count == 0)
                return Matrix4x4.Identity;

            // Calculate centroids
            var sourceCentroid = Vector3.Zero;
            var targetCentroid = Vector3.Zero;
            
            foreach (var (source, target) in correspondences)
            {
                sourceCentroid += source;
                targetCentroid += target;
            }
            
            sourceCentroid /= correspondences.Count;
            targetCentroid /= correspondences.Count;

            // Calculate translation
            var translation = targetCentroid - sourceCentroid;
            
            // For simplicity, return translation-only transformation
            // In practice, you'd calculate rotation using SVD
            return Matrix4x4.CreateTranslation(translation);
        }

        /// <summary>
        /// Check if ICP has converged
        /// </summary>
        private bool IsConverged(List<(Vector3 source, Vector3 target)> correspondences, float threshold = 0.001f)
        {
            var avgDistance = correspondences.Average(c => Vector3.Distance(c.source, c.target));
            return avgDistance < threshold;
        }

        /// <summary>
        /// Transform point cloud by transformation matrix
        /// </summary>
        private ColoredPoint3D[] TransformPointCloud(ColoredPoint3D[] points, Matrix4x4 transformation)
        {
            var transformedPoints = new ColoredPoint3D[points.Length];
            
            for (int i = 0; i < points.Length; i++)
            {
                var transformedPosition = Vector3.Transform(points[i].Position, transformation);
                transformedPoints[i] = new ColoredPoint3D(transformedPosition, points[i].Color, points[i].Normal);
            }
            
            return transformedPoints;
        }
    }

    /// <summary>
    /// Integer vector for voxel coordinates
    /// </summary>
    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(Vector3Int other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector3Int other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(Vector3Int left, Vector3Int right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3Int left, Vector3Int right)
        {
            return !left.Equals(right);
        }
    }
}
