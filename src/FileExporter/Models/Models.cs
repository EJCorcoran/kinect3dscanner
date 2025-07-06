using System.Numerics;

namespace FileExporter.Models
{
    /// <summary>
    /// Represents a 3D point with color information (copy for standalone use)
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
    /// Represents a 3D mesh vertex
    /// </summary>
    public struct Vertex
    {
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector3 Color { get; set; }
        public Vector2 TexCoord { get; set; }

        public Vertex(Vector3 position, Vector3 normal = default, Vector3 color = default, Vector2 texCoord = default)
        {
            Position = position;
            Normal = normal;
            Color = color;
            TexCoord = texCoord;
        }
    }

    /// <summary>
    /// Represents a 3D mesh
    /// </summary>
    public class Mesh
    {
        public Vertex[] Vertices { get; set; } = Array.Empty<Vertex>();
        public uint[] Faces { get; set; } = Array.Empty<uint>(); // Triangle indices

        public int VertexCount => Vertices.Length;
        public int TriangleCount => Faces.Length / 3;
    }
}
