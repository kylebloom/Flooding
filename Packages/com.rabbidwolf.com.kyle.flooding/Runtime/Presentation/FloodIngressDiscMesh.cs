using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Shared unit-radius disc mesh used by local ingress presentation.
    /// </summary>
    public static class FloodIngressDiscMesh
    {
        private const int DefaultSegmentCount = 32;

        private static Mesh sharedMesh;
        private static bool hasSharedMesh;

        /// <summary>
        /// Gets a lazily created shared unit disc in the XZ plane (normal +Y).
        /// </summary>
        public static Mesh SharedUnitDisc
        {
            get
            {
                if (hasSharedMesh && sharedMesh != null)
                    return sharedMesh;

                sharedMesh = CreateUnitDisc(DefaultSegmentCount);
                sharedMesh.name = "FloodIngressUnitDisc";
                sharedMesh.hideFlags = HideFlags.HideAndDontSave;
                hasSharedMesh = true;
                return sharedMesh;
            }
        }

        /// <summary>
        /// Creates a unit-radius disc mesh centered at the origin in the XZ plane.
        /// </summary>
        public static Mesh CreateUnitDisc(int segmentCount)
        {
            var segments = Mathf.Max(3, segmentCount);
            var vertexCount = segments + 1;
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            normals[0] = Vector3.up;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (var i = 0; i < segments; i++)
            {
                var angle = (i / (float)segments) * Mathf.PI * 2f;
                var x = Mathf.Cos(angle);
                var z = Mathf.Sin(angle);
                vertices[i + 1] = new Vector3(x, 0f, z);
                normals[i + 1] = Vector3.up;
                uvs[i + 1] = new Vector2((x + 1f) * 0.5f, (z + 1f) * 0.5f);

                var tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = i + 2 <= segments ? i + 2 : 1;
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles,
            };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
