using System.Numerics;

namespace MeshGenerator.Models
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
}
