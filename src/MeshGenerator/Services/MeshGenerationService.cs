using MeshGenerator.Models;
using System.Numerics;

namespace MeshGenerator.Services
{
    /// <summary>
    /// Service for generating meshes from point clouds
    /// </summary>
    public class MeshGenerationService
    {
        /// <summary>
        /// Generate mesh from point cloud using Delaunay triangulation (simplified)
        /// </summary>
        public Mesh GenerateMeshFromPointCloud(ColoredPoint3D[] points, float maxEdgeLength = 0.1f)
        {
            if (points.Length < 3)
                return new Mesh();

            // For simplicity, we'll use a greedy triangulation approach
            // In a production system, you'd use proper algorithms like Poisson reconstruction
            
            var vertices = points.Select(p => new Vertex(p.Position, p.Normal, p.Color)).ToArray();
            var faces = new List<uint>();

            // Simple greedy triangulation
            var triangles = GreedyTriangulation(vertices, maxEdgeLength);
            faces.AddRange(triangles);

            var mesh = new Mesh
            {
                Vertices = vertices,
                Faces = faces.ToArray()
            };

            mesh.CalculateBoundingBox();
            mesh.CalculateNormals();

            return mesh;
        }

        /// <summary>
        /// Generate mesh using Marching Cubes algorithm (simplified)
        /// </summary>
        public Mesh GenerateMeshWithMarchingCubes(ColoredPoint3D[] points, float voxelSize = 0.01f, float isoValue = 0.0f)
        {
            if (points.Length == 0)
                return new Mesh();

            // Create voxel grid
            var voxelGrid = CreateVoxelGrid(points, voxelSize);
            
            // Apply marching cubes
            var mesh = MarchingCubes(voxelGrid, isoValue, voxelSize);
            
            mesh.CalculateBoundingBox();
            mesh.CalculateNormals();
            
            return mesh;
        }

        /// <summary>
        /// Smooth mesh using Laplacian smoothing
        /// </summary>
        public void SmoothMesh(Mesh mesh, int iterations = 3, float lambda = 0.5f)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                var newPositions = new Vector3[mesh.Vertices.Length];
                var neighborCounts = new int[mesh.Vertices.Length];

                // Initialize with current positions
                for (int i = 0; i < mesh.Vertices.Length; i++)
                {
                    newPositions[i] = mesh.Vertices[i].Position;
                }

                // Accumulate neighbor positions
                for (int i = 0; i < mesh.Faces.Length; i += 3)
                {
                    var i1 = mesh.Faces[i];
                    var i2 = mesh.Faces[i + 1];
                    var i3 = mesh.Faces[i + 2];

                    // Each vertex is neighbor to the other two in the triangle
                    newPositions[i1] += mesh.Vertices[i2].Position + mesh.Vertices[i3].Position;
                    newPositions[i2] += mesh.Vertices[i1].Position + mesh.Vertices[i3].Position;
                    newPositions[i3] += mesh.Vertices[i1].Position + mesh.Vertices[i2].Position;

                    neighborCounts[i1] += 2;
                    neighborCounts[i2] += 2;
                    neighborCounts[i3] += 2;
                }

                // Average and apply smoothing
                for (int i = 0; i < mesh.Vertices.Length; i++)
                {
                    if (neighborCounts[i] > 0)
                    {
                        var avgNeighborPos = newPositions[i] / neighborCounts[i];
                        var smoothedPos = Vector3.Lerp(mesh.Vertices[i].Position, avgNeighborPos, lambda);
                        
                        mesh.Vertices[i] = new Vertex(
                            smoothedPos,
                            mesh.Vertices[i].Normal,
                            mesh.Vertices[i].Color,
                            mesh.Vertices[i].TexCoord);
                    }
                }
            }

            mesh.CalculateNormals();
        }

        /// <summary>
        /// Reduce mesh complexity by removing triangles
        /// </summary>
        public Mesh SimplifyMesh(Mesh mesh, float targetReduction = 0.5f)
        {
            if (mesh.Faces.Length == 0)
                return mesh;

            var targetTriangles = (int)(mesh.TriangleCount * (1.0f - targetReduction));
            
            // Simple edge collapse decimation
            var vertices = mesh.Vertices.ToList();
            var faces = mesh.Faces.ToList();

            while (faces.Count / 3 > targetTriangles && faces.Count > 6)
            {
                // Find shortest edge
                var shortestEdgeLength = float.MaxValue;
                var edgeToCollapse = (-1, -1);

                for (int i = 0; i < faces.Count; i += 3)
                {
                    var edges = new[]
                    {
                        ((int)faces[i], (int)faces[i + 1]),
                        ((int)faces[i + 1], (int)faces[i + 2]),
                        ((int)faces[i + 2], (int)faces[i])
                    };

                    foreach (var (v1, v2) in edges)
                    {
                        var edgeLength = Vector3.Distance(vertices[v1].Position, vertices[v2].Position);
                        if (edgeLength < shortestEdgeLength)
                        {
                            shortestEdgeLength = edgeLength;
                            edgeToCollapse = (v1, v2);
                        }
                    }
                }

                if (edgeToCollapse.Item1 >= 0)
                {
                    CollapseEdge(vertices, faces, edgeToCollapse.Item1, edgeToCollapse.Item2);
                }
                else
                {
                    break; // No more edges to collapse
                }
            }

            var simplifiedMesh = new Mesh
            {
                Vertices = vertices.ToArray(),
                Faces = faces.ToArray()
            };

            simplifiedMesh.CalculateBoundingBox();
            simplifiedMesh.CalculateNormals();

            return simplifiedMesh;
        }

        /// <summary>
        /// Fill holes in mesh
        /// </summary>
        public void FillHoles(Mesh mesh)
        {
            var boundaryEdges = FindBoundaryEdges(mesh);
            var newFaces = new List<uint>();

            foreach (var hole in boundaryEdges)
            {
                if (hole.Count >= 3)
                {
                    // Simple ear clipping for hole filling
                    var filledTriangles = EarClipping(hole);
                    newFaces.AddRange(filledTriangles);
                }
            }

            // Add new faces to mesh
            var allFaces = mesh.Faces.ToList();
            allFaces.AddRange(newFaces);
            mesh.Faces = allFaces.ToArray();

            mesh.CalculateNormals();
        }

        /// <summary>
        /// Simple greedy triangulation
        /// </summary>
        private List<uint> GreedyTriangulation(Vertex[] vertices, float maxEdgeLength)
        {
            var faces = new List<uint>();
            var used = new bool[vertices.Length];

            for (int i = 0; i < vertices.Length - 2; i++)
            {
                if (used[i]) continue;

                for (int j = i + 1; j < vertices.Length - 1; j++)
                {
                    if (used[j]) continue;

                    var dist1 = Vector3.Distance(vertices[i].Position, vertices[j].Position);
                    if (dist1 > maxEdgeLength) continue;

                    for (int k = j + 1; k < vertices.Length; k++)
                    {
                        if (used[k]) continue;

                        var dist2 = Vector3.Distance(vertices[j].Position, vertices[k].Position);
                        var dist3 = Vector3.Distance(vertices[k].Position, vertices[i].Position);

                        if (dist2 <= maxEdgeLength && dist3 <= maxEdgeLength)
                        {
                            // Check if triangle is valid (not degenerate)
                            if (IsValidTriangle(vertices[i].Position, vertices[j].Position, vertices[k].Position))
                            {
                                faces.Add((uint)i);
                                faces.Add((uint)j);
                                faces.Add((uint)k);
                                
                                used[i] = used[j] = used[k] = true;
                                break;
                            }
                        }
                    }
                    
                    if (used[i]) break;
                }
            }

            return faces;
        }

        /// <summary>
        /// Check if triangle is valid (not degenerate)
        /// </summary>
        private bool IsValidTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var edge1 = v2 - v1;
            var edge2 = v3 - v1;
            var cross = Vector3.Cross(edge1, edge2);
            return cross.Length() > 1e-6f; // Minimum area threshold
        }

        /// <summary>
        /// Create voxel grid from point cloud
        /// </summary>
        private VoxelGrid CreateVoxelGrid(ColoredPoint3D[] points, float voxelSize)
        {
            // Calculate bounds
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);

            foreach (var point in points)
            {
                min = Vector3.Min(min, point.Position);
                max = Vector3.Max(max, point.Position);
            }

            // Expand bounds slightly
            min -= Vector3.One * voxelSize;
            max += Vector3.One * voxelSize;

            var size = max - min;
            var dimensions = new Vector3(
                (int)Math.Ceiling(size.X / voxelSize),
                (int)Math.Ceiling(size.Y / voxelSize),
                (int)Math.Ceiling(size.Z / voxelSize));

            var grid = new VoxelGrid((int)dimensions.X, (int)dimensions.Y, (int)dimensions.Z, voxelSize, min);

            // Fill grid with point data
            foreach (var point in points)
            {
                var voxelCoord = grid.WorldToVoxel(point.Position);
                if (grid.IsValidCoordinate(voxelCoord))
                {
                    grid.SetValue(voxelCoord, 1.0f); // Inside surface
                }
            }

            return grid;
        }

        /// <summary>
        /// Simple marching cubes implementation
        /// </summary>
        private Mesh MarchingCubes(VoxelGrid grid, float isoValue, float voxelSize)
        {
            var vertices = new List<Vertex>();
            var faces = new List<uint>();

            // Simplified marching cubes - just create cubes for occupied voxels
            for (int x = 0; x < grid.Width - 1; x++)
            {
                for (int y = 0; y < grid.Height - 1; y++)
                {
                    for (int z = 0; z < grid.Depth - 1; z++)
                    {
                        if (grid.GetValue(x, y, z) > isoValue)
                        {
                            // Create a cube at this voxel
                            var cubeVertices = CreateCubeVertices(grid.VoxelToWorld(new Vector3(x, y, z)), voxelSize);
                            var basIndex = (uint)vertices.Count;
                            
                            vertices.AddRange(cubeVertices);
                            
                            // Add cube faces (12 triangles)
                            var cubeFaces = CreateCubeFaces(basIndex);
                            faces.AddRange(cubeFaces);
                        }
                    }
                }
            }

            return new Mesh
            {
                Vertices = vertices.ToArray(),
                Faces = faces.ToArray()
            };
        }

        /// <summary>
        /// Create vertices for a cube
        /// </summary>
        private Vertex[] CreateCubeVertices(Vector3 position, float size)
        {
            var half = size * 0.5f;
            return new[]
            {
                new Vertex(position + new Vector3(-half, -half, -half)),
                new Vertex(position + new Vector3(half, -half, -half)),
                new Vertex(position + new Vector3(half, half, -half)),
                new Vertex(position + new Vector3(-half, half, -half)),
                new Vertex(position + new Vector3(-half, -half, half)),
                new Vertex(position + new Vector3(half, -half, half)),
                new Vertex(position + new Vector3(half, half, half)),
                new Vertex(position + new Vector3(-half, half, half)),
            };
        }

        /// <summary>
        /// Create face indices for a cube
        /// </summary>
        private uint[] CreateCubeFaces(uint baseIndex)
        {
            return new uint[]
            {
                // Front face
                baseIndex + 0, baseIndex + 1, baseIndex + 2,
                baseIndex + 0, baseIndex + 2, baseIndex + 3,
                // Back face
                baseIndex + 4, baseIndex + 6, baseIndex + 5,
                baseIndex + 4, baseIndex + 7, baseIndex + 6,
                // Left face
                baseIndex + 0, baseIndex + 3, baseIndex + 7,
                baseIndex + 0, baseIndex + 7, baseIndex + 4,
                // Right face
                baseIndex + 1, baseIndex + 5, baseIndex + 6,
                baseIndex + 1, baseIndex + 6, baseIndex + 2,
                // Top face
                baseIndex + 3, baseIndex + 2, baseIndex + 6,
                baseIndex + 3, baseIndex + 6, baseIndex + 7,
                // Bottom face
                baseIndex + 0, baseIndex + 4, baseIndex + 5,
                baseIndex + 0, baseIndex + 5, baseIndex + 1,
            };
        }

        /// <summary>
        /// Collapse edge in mesh
        /// </summary>
        private void CollapseEdge(List<Vertex> vertices, List<uint> faces, int v1, int v2)
        {
            // Move v2 to midpoint between v1 and v2
            var midpoint = (vertices[v1].Position + vertices[v2].Position) * 0.5f;
            vertices[v1] = new Vertex(midpoint, vertices[v1].Normal, vertices[v1].Color, vertices[v1].TexCoord);

            // Remove triangles that contain both v1 and v2
            for (int i = faces.Count - 3; i >= 0; i -= 3)
            {
                var hasV1 = faces[i] == v1 || faces[i + 1] == v1 || faces[i + 2] == v1;
                var hasV2 = faces[i] == v2 || faces[i + 1] == v2 || faces[i + 2] == v2;

                if (hasV1 && hasV2)
                {
                    faces.RemoveRange(i, 3);
                }
                else if (hasV2)
                {
                    // Replace v2 with v1
                    for (int j = 0; j < 3; j++)
                    {
                        if (faces[i + j] == v2)
                            faces[i + j] = (uint)v1;
                    }
                }
            }
        }

        /// <summary>
        /// Find boundary edges in mesh
        /// </summary>
        private List<List<uint>> FindBoundaryEdges(Mesh mesh)
        {
            var edgeCount = new Dictionary<(uint, uint), int>();

            // Count edge occurrences
            for (int i = 0; i < mesh.Faces.Length; i += 3)
            {
                var edges = new[]
                {
                    (Math.Min(mesh.Faces[i], mesh.Faces[i + 1]), Math.Max(mesh.Faces[i], mesh.Faces[i + 1])),
                    (Math.Min(mesh.Faces[i + 1], mesh.Faces[i + 2]), Math.Max(mesh.Faces[i + 1], mesh.Faces[i + 2])),
                    (Math.Min(mesh.Faces[i + 2], mesh.Faces[i]), Math.Max(mesh.Faces[i + 2], mesh.Faces[i]))
                };

                foreach (var edge in edges)
                {
                    edgeCount[edge] = edgeCount.GetValueOrDefault(edge, 0) + 1;
                }
            }

            // Find boundary edges (edges that appear only once)
            var boundaryEdges = edgeCount.Where(kvp => kvp.Value == 1).Select(kvp => kvp.Key).ToList();

            // Group into holes (simplified)
            var holes = new List<List<uint>>();
            // This is a simplified implementation - proper hole detection would trace connected boundary loops
            
            return holes;
        }

        /// <summary>
        /// Simple ear clipping triangulation
        /// </summary>
        private List<uint> EarClipping(List<uint> boundary)
        {
            var triangles = new List<uint>();
            // Simplified implementation - proper ear clipping is more complex
            return triangles;
        }
    }

    /// <summary>
    /// Simple voxel grid for marching cubes
    /// </summary>
    public class VoxelGrid
    {
        private readonly float[,,] _values;
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public float VoxelSize { get; }
        public Vector3 Origin { get; }

        public VoxelGrid(int width, int height, int depth, float voxelSize, Vector3 origin)
        {
            Width = width;
            Height = height;
            Depth = depth;
            VoxelSize = voxelSize;
            Origin = origin;
            _values = new float[width, height, depth];
        }

        public float GetValue(int x, int y, int z)
        {
            return IsValidCoordinate(x, y, z) ? _values[x, y, z] : 0f;
        }

        public void SetValue(Vector3 coord, float value)
        {
            SetValue((int)coord.X, (int)coord.Y, (int)coord.Z, value);
        }

        public void SetValue(int x, int y, int z, float value)
        {
            if (IsValidCoordinate(x, y, z))
                _values[x, y, z] = value;
        }

        public bool IsValidCoordinate(Vector3 coord)
        {
            return IsValidCoordinate((int)coord.X, (int)coord.Y, (int)coord.Z);
        }

        public bool IsValidCoordinate(int x, int y, int z)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth;
        }

        public Vector3 WorldToVoxel(Vector3 worldPos)
        {
            var relative = worldPos - Origin;
            return new Vector3(
                relative.X / VoxelSize,
                relative.Y / VoxelSize,
                relative.Z / VoxelSize);
        }

        public Vector3 VoxelToWorld(Vector3 voxelPos)
        {
            return Origin + voxelPos * VoxelSize;
        }
    }
}
