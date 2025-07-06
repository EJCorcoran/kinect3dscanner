using PointCloudProcessor.Models;
using PointCloudProcessor.Services;
using MeshGenerator.Services;
using MeshGenerator.Models;
using FileExporter.Services;
using FileExporter.Models;
using System.Numerics;

namespace PointCloudDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Kinect 3D Scanner - Point Cloud Processing Demo");
            Console.WriteLine("====================================================");
            Console.WriteLine();

            // Generate sample point cloud data (simulates Kinect capture)
            Console.WriteLine("1. Generating sample point cloud...");
            var pointCloud = GenerateSamplePointCloud();
            Console.WriteLine($"   Generated {pointCloud.Length:N0} points");

            // Process point cloud
            Console.WriteLine("\n2. Processing point cloud...");
            var processor = new PointCloudProcessingService();
            
            // Apply voxel grid filtering
            var filteredPoints = processor.VoxelGridFilter(pointCloud, 0.01f);
            Console.WriteLine($"   Filtered to {filteredPoints.Length:N0} points (voxel size: 1cm)");

            // Estimate normals
            var pointsWithNormals = processor.EstimateNormals(filteredPoints);
            Console.WriteLine($"   Estimated normals for {pointsWithNormals.Length:N0} points");

            // Remove outliers
            var cleanPoints = processor.RemoveStatisticalOutliers(pointsWithNormals);
            Console.WriteLine($"   Removed outliers, {cleanPoints.Length:N0} points remaining");

            // Generate mesh
            Console.WriteLine("\n3. Generating mesh...");
            var meshGenerator = new MeshGenerationService();
            
            // Convert point cloud types for mesh generation
            var meshPoints = cleanPoints.Select(p => new MeshGenerator.Models.ColoredPoint3D(p.Position, p.Color, p.Normal)).ToArray();
            var mesh = meshGenerator.GenerateMeshFromPointCloud(meshPoints);
            Console.WriteLine($"   Generated mesh with {mesh.VertexCount:N0} vertices and {mesh.TriangleCount:N0} triangles");

            // Smooth mesh
            meshGenerator.SmoothMesh(mesh, iterations: 2);
            Console.WriteLine($"   Applied smoothing");

            // Export results
            Console.WriteLine("\n4. Exporting files...");
            var exporter = new FileExportService();
            
            var outputDir = "output";
            Directory.CreateDirectory(outputDir);

            // Convert point cloud types for export
            var exportPoints = cleanPoints.Select(p => new FileExporter.Models.ColoredPoint3D(p.Position, p.Color, p.Normal)).ToArray();
            
            // Convert mesh for export
            var exportMesh = new FileExporter.Models.Mesh
            {
                Vertices = mesh.Vertices.Select(v => new FileExporter.Models.Vertex(v.Position, v.Normal, v.Color, v.TexCoord)).ToArray(),
                Faces = mesh.Faces
            };

            // Export point cloud as PLY
            await exporter.ExportToPlyAsync(exportPoints, Path.Combine(outputDir, "pointcloud.ply"));
            Console.WriteLine($"   Exported point cloud: {outputDir}\\pointcloud.ply");

            // Export mesh as STL
            await exporter.ExportToStlAsync(exportMesh, Path.Combine(outputDir, "mesh.stl"));
            Console.WriteLine($"   Exported mesh (STL): {outputDir}\\mesh.stl");

            // Export mesh as OBJ
            await exporter.ExportToObjAsync(exportMesh, Path.Combine(outputDir, "mesh.obj"));
            Console.WriteLine($"   Exported mesh (OBJ): {outputDir}\\mesh.obj");

            Console.WriteLine("\n✅ Demo completed successfully!");
            Console.WriteLine("\nGenerated files:");
            Console.WriteLine($"   - {outputDir}\\pointcloud.ply (point cloud data)");
            Console.WriteLine($"   - {outputDir}\\mesh.stl (3D printable mesh)");
            Console.WriteLine($"   - {outputDir}\\mesh.obj (3D model with materials)");
            Console.WriteLine("\nThese files can be opened in 3D software like Blender, MeshLab, or sent to a 3D printer.");

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Generate a sample point cloud representing a simple sphere
        /// This simulates what would come from the Azure Kinect camera
        /// </summary>
        static PointCloudProcessor.Models.ColoredPoint3D[] GenerateSamplePointCloud()
        {
            var points = new List<PointCloudProcessor.Models.ColoredPoint3D>();
            var random = new Random(42); // Fixed seed for reproducible results

            // Generate points on a sphere surface with some noise
            var sphereRadius = 0.15f; // 15cm radius
            var center = new Vector3(0, 0, 0.5f); // 50cm in front of camera
            var pointCount = 10000;

            for (int i = 0; i < pointCount; i++)
            {
                // Generate random point on sphere using spherical coordinates
                var theta = random.NextSingle() * 2 * MathF.PI; // Azimuth
                var phi = MathF.Acos(2 * random.NextSingle() - 1); // Polar angle

                // Convert to Cartesian coordinates
                var x = sphereRadius * MathF.Sin(phi) * MathF.Cos(theta);
                var y = sphereRadius * MathF.Sin(phi) * MathF.Sin(theta);
                var z = sphereRadius * MathF.Cos(phi);

                // Add some random noise to make it more realistic
                var noise = 0.005f; // 5mm noise
                x += (random.NextSingle() - 0.5f) * noise;
                y += (random.NextSingle() - 0.5f) * noise;
                z += (random.NextSingle() - 0.5f) * noise;

                var position = center + new Vector3(x, y, z);

                // Generate color based on height (gradient from blue to red)
                var normalizedHeight = (position.Y + sphereRadius) / (2 * sphereRadius);
                var color = new Vector3(
                    normalizedHeight,           // Red component
                    0.5f,                      // Green component
                    1.0f - normalizedHeight    // Blue component
                );

                // Normal points outward from sphere center
                var normal = Vector3.Normalize(position - center);

                points.Add(new PointCloudProcessor.Models.ColoredPoint3D(position, color, normal));
            }

            // Add some background noise points (simulates imperfect background removal)
            var noiseCount = 500;
            for (int i = 0; i < noiseCount; i++)
            {
                var noisePos = new Vector3(
                    (random.NextSingle() - 0.5f) * 0.6f,  // ±30cm
                    (random.NextSingle() - 0.5f) * 0.4f,  // ±20cm
                    0.3f + random.NextSingle() * 0.4f     // 30-70cm depth
                );

                var noiseColor = new Vector3(0.2f, 0.2f, 0.2f); // Gray
                points.Add(new PointCloudProcessor.Models.ColoredPoint3D(noisePos, noiseColor));
            }

            return points.ToArray();
        }
    }
}
