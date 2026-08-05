using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Procedural tapered jet mesh deformed along a presentation-only ballistic
    /// curve. Vertex buffers are reused; no per-frame mesh allocation.
    /// </summary>
    public sealed class FloodIngressJetMesh
    {
        public const int DefaultRadialSegments = 12;
        public const int DefaultLengthSegments = 10;

        private readonly int radialSegments;
        private readonly int lengthSegments;
        private readonly Mesh mesh;
        private readonly Vector3[] vertices;
        private readonly Vector3[] normals;
        private readonly Vector2[] uvs;
        private readonly int[] triangles;
        private readonly Vector3[] pathPoints;
        private readonly Vector3[] pathTangents;

        /// <summary>
        /// Creates a reusable jet mesh deformer.
        /// </summary>
        public FloodIngressJetMesh(
            int radialSegments = DefaultRadialSegments,
            int lengthSegments = DefaultLengthSegments)
        {
            this.radialSegments = Mathf.Max(3, radialSegments);
            this.lengthSegments = Mathf.Max(2, lengthSegments);

            var ringCount = this.lengthSegments + 1;
            var vertexCount = ringCount * this.radialSegments;
            vertices = new Vector3[vertexCount];
            normals = new Vector3[vertexCount];
            uvs = new Vector2[vertexCount];
            triangles = new int[this.lengthSegments * this.radialSegments * 6];
            pathPoints = new Vector3[ringCount];
            pathTangents = new Vector3[ringCount];

            BuildTopology();
            mesh = new Mesh
            {
                name = "FloodIngressJet",
                hideFlags = HideFlags.HideAndDontSave,
            };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Gets the reusable mesh instance.
        /// </summary>
        public Mesh Mesh => mesh;

        /// <summary>
        /// Gets the last computed impact / end point in world space.
        /// </summary>
        public Vector3 ImpactPointWorld { get; private set; }

        /// <summary>
        /// Gets the last computed impact normal in world space.
        /// </summary>
        public Vector3 ImpactNormalWorld { get; private set; }

        /// <summary>
        /// Gets whether the last deform found a floor intersection.
        /// </summary>
        public bool HasImpact { get; private set; }

        /// <summary>
        /// Deforms the jet along a ballistic presentation curve.
        /// </summary>
        public void Deform(
            Vector3 originWorld,
            Vector3 directionWorld,
            Vector3 gravityWorld,
            float initialSpeed,
            float lifetimeSeconds,
            float sourceWidth,
            float taper,
            Vector3 floorPointWorld,
            Vector3 floorNormalWorld)
        {
            if (directionWorld.sqrMagnitude <= 0.0001f)
                directionWorld = Vector3.forward;
            else
                directionWorld.Normalize();

            if (floorNormalWorld.sqrMagnitude <= 0.0001f)
                floorNormalWorld = Vector3.up;
            else
                floorNormalWorld = floorNormalWorld.normalized;

            initialSpeed = Mathf.Max(0.01f, initialSpeed);
            lifetimeSeconds = Mathf.Max(0.05f, lifetimeSeconds);
            sourceWidth = Mathf.Max(0.005f, sourceWidth);
            taper = Mathf.Clamp01(taper);

            var velocity = directionWorld * initialSpeed;
            var ringCount = lengthSegments + 1;
            HasImpact = false;
            ImpactPointWorld = originWorld + (velocity * lifetimeSeconds);
            ImpactNormalWorld = floorNormalWorld;

            var clippedLifetime = lifetimeSeconds;
            for (var i = 0; i < ringCount; i++)
            {
                var t = (i / (float)lengthSegments) * lifetimeSeconds;
                var point = originWorld + (velocity * t) + (0.5f * gravityWorld * (t * t));
                var tangent = velocity + (gravityWorld * t);
                if (tangent.sqrMagnitude <= 0.0001f)
                    tangent = directionWorld;
                else
                    tangent.Normalize();

                pathPoints[i] = point;
                pathTangents[i] = tangent;

                var toPlane = point - floorPointWorld;
                var signed = Vector3.Dot(toPlane, floorNormalWorld);
                if (i > 0 && signed <= 0f && !HasImpact)
                {
                    var previous = pathPoints[i - 1];
                    var prevSigned = Vector3.Dot(previous - floorPointWorld, floorNormalWorld);
                    var denom = prevSigned - signed;
                    var alpha = denom > 0.0001f ? prevSigned / denom : 1f;
                    ImpactPointWorld = Vector3.Lerp(previous, point, Mathf.Clamp01(alpha));
                    ImpactNormalWorld = floorNormalWorld;
                    HasImpact = true;
                    clippedLifetime = Mathf.Lerp(
                        ((i - 1) / (float)lengthSegments) * lifetimeSeconds,
                        t,
                        Mathf.Clamp01(alpha));
                }
            }

            if (HasImpact)
            {
                for (var i = 0; i < ringCount; i++)
                {
                    var t = (i / (float)lengthSegments) * clippedLifetime;
                    pathPoints[i] =
                        originWorld + (velocity * t) + (0.5f * gravityWorld * (t * t));
                    var tangent = velocity + (gravityWorld * t);
                    pathTangents[i] = tangent.sqrMagnitude > 0.0001f
                        ? tangent.normalized
                        : directionWorld;
                }
            }

            var upRef = Mathf.Abs(Vector3.Dot(directionWorld, Vector3.up)) > 0.9f
                ? Vector3.forward
                : Vector3.up;

            for (var ring = 0; ring < ringCount; ring++)
            {
                var center = pathPoints[ring];
                var tangent = pathTangents[ring];
                var normal = Vector3.Cross(tangent, upRef);
                if (normal.sqrMagnitude <= 0.0001f)
                    normal = Vector3.Cross(tangent, Vector3.right);
                normal.Normalize();
                var binormal = Vector3.Cross(tangent, normal).normalized;

                var along = ring / (float)lengthSegments;
                var radius = sourceWidth * 0.5f * Mathf.Lerp(1f, Mathf.Max(0.05f, taper), along);

                for (var spoke = 0; spoke < radialSegments; spoke++)
                {
                    var angle = (spoke / (float)radialSegments) * Mathf.PI * 2f;
                    var offset =
                        (normal * Mathf.Cos(angle) * radius)
                        + (binormal * Mathf.Sin(angle) * radius);
                    var index = (ring * radialSegments) + spoke;
                    // Store origin-relative local positions so the MeshFilter can
                    // sit at the ingress origin with identity rotation.
                    vertices[index] = (center + offset) - originWorld;
                    normals[index] = offset.sqrMagnitude > 0.0001f
                        ? offset.normalized
                        : normal;
                    uvs[index] = new Vector2(spoke / (float)radialSegments, along);
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.RecalculateBounds();
        }

        private void BuildTopology()
        {
            var tri = 0;
            for (var ring = 0; ring < lengthSegments; ring++)
            {
                for (var spoke = 0; spoke < radialSegments; spoke++)
                {
                    var current = (ring * radialSegments) + spoke;
                    var next = (ring * radialSegments) + ((spoke + 1) % radialSegments);
                    var currentUpper = current + radialSegments;
                    var nextUpper = next + radialSegments;

                    triangles[tri++] = current;
                    triangles[tri++] = currentUpper;
                    triangles[tri++] = nextUpper;
                    triangles[tri++] = current;
                    triangles[tri++] = nextUpper;
                    triangles[tri++] = next;
                }
            }
        }
    }
}
