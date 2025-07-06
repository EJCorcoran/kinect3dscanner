using System.Numerics;

namespace MeshGenerator.Models
{
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
        public Vector3 BoundingBoxMin { get; set; }
        public Vector3 BoundingBoxMax { get; set; }

        public int VertexCount => Vertices.Length;
        public int TriangleCount => Faces.Length / 3;

        /// <summary>
        /// Calculate bounding box for the mesh
        /// </summary>
        public void CalculateBoundingBox()
        {
            if (Vertices.Length == 0)
            {
                BoundingBoxMin = BoundingBoxMax = Vector3.Zero;
                return;
            }

            BoundingBoxMin = BoundingBoxMax = Vertices[0].Position;

            foreach (var vertex in Vertices)
            {
                BoundingBoxMin = Vector3.Min(BoundingBoxMin, vertex.Position);
                BoundingBoxMax = Vector3.Max(BoundingBoxMax, vertex.Position);
            }
        }

        /// <summary>
        /// Calculate vertex normals from face normals
        /// </summary>
        public void CalculateNormals()
        {
            // Initialize normals to zero
            var normals = new Vector3[Vertices.Length];

            // Calculate face normals and accumulate at vertices
            for (int i = 0; i < Faces.Length; i += 3)
            {
                var i1 = Faces[i];
                var i2 = Faces[i + 1];
                var i3 = Faces[i + 2];

                var v1 = Vertices[i1].Position;
                var v2 = Vertices[i2].Position;
                var v3 = Vertices[i3].Position;

                var edge1 = v2 - v1;
                var edge2 = v3 - v1;
                var faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                normals[i1] += faceNormal;
                normals[i2] += faceNormal;
                normals[i3] += faceNormal;
            }

            // Normalize vertex normals
            for (int i = 0; i < Vertices.Length; i++)
            {
                if (normals[i].Length() > 0)
                {
                    normals[i] = Vector3.Normalize(normals[i]);
                }

                Vertices[i] = new Vertex(
                    Vertices[i].Position,
                    normals[i],
                    Vertices[i].Color,
                    Vertices[i].TexCoord);
            }
        }

        /// <summary>
        /// Transform mesh by matrix
        /// </summary>
        public void Transform(Matrix4x4 transformation)
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                var vertex = Vertices[i];
                vertex.Position = Vector3.Transform(vertex.Position, transformation);
                
                // Transform normal (use inverse transpose for proper normal transformation)
                if (Matrix4x4.Invert(transformation, out var inverse))
                {
                    var transposeInverse = Matrix4x4.Transpose(inverse);
                    vertex.Normal = Vector3.TransformNormal(vertex.Normal, transposeInverse);
                    vertex.Normal = Vector3.Normalize(vertex.Normal);
                }

                Vertices[i] = vertex;
            }

            CalculateBoundingBox();
        }
    }
}
