using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presents an extruded footprint as a generated water-volume mesh.
    /// The mesh is presentation-only and never mutates simulation state.
    /// </summary>
    public sealed class FloodPolygonSurfaceRenderer : FloodSurfaceRenderer
    {
        [SerializeField]
        [Tooltip("MeshFilter on the water visual GameObject. Its mesh is generated from the source volume footprint.")]
        private MeshFilter waterMeshFilter;

        [SerializeField]
        [Tooltip("Water height in meters below which the target MeshRenderer is disabled.")]
        [Min(0f)]
        private float minimumVisibleHeight = 0.01f;

        private Mesh runtimeMesh;
        private MeshRenderer waterMeshRenderer;

        /// <summary>
        /// Gets or sets the target water MeshFilter.
        /// </summary>
        public MeshFilter WaterMeshFilter
        {
            get => waterMeshFilter;
            set
            {
                if (waterMeshFilter == value)
                    return;

                ReleaseMesh();
                waterMeshFilter = value;
                waterMeshRenderer =
                    waterMeshFilter == null
                        ? null
                        : waterMeshFilter.GetComponent<MeshRenderer>();
                SnapToCurrentState();
            }
        }

        /// <summary>
        /// Gets or sets the minimum visible water height in meters.
        /// </summary>
        public float MinimumVisibleHeight
        {
            get => minimumVisibleHeight;
            set
            {
                minimumVisibleHeight = Mathf.Max(0f, value);
                SnapToCurrentState();
            }
        }

        protected override void Reset()
        {
            base.Reset();
            waterMeshFilter = GetComponentInChildren<MeshFilter>();
        }

        protected override void Awake()
        {
            base.Awake();

            if (waterMeshFilter == null)
                waterMeshFilter = GetComponentInChildren<MeshFilter>();

            waterMeshRenderer =
                waterMeshFilter == null
                    ? null
                    : waterMeshFilter.GetComponent<MeshRenderer>();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            minimumVisibleHeight = Mathf.Max(0f, minimumVisibleHeight);

            if (waterMeshFilter == null)
                waterMeshFilter = GetComponentInChildren<MeshFilter>();
        }

        private void OnDestroy()
        {
            ReleaseMesh();
        }

        /// <inheritdoc />
        protected override void ApplyState(FloodState state)
        {
            if (SourceVolume == null || waterMeshFilter == null)
                return;

            if (!(SourceVolume.Geometry
                is IExtrudedFloodVolumeGeometry extrudedGeometry))
            {
                SetRendererVisible(false);
                return;
            }

            var height = (float)state.Height;

            if (height <= minimumVisibleHeight)
            {
                SetRendererVisible(false);
                return;
            }

            EnsureMesh();
            var localSurfacePlane = FloodPlaneUtility.WorldToLocal(
                SourceVolume.transform,
                state.SurfacePlane);
            var meshData =
                FloodExtrudedGeometryQueries.BuildSubmergedMesh(
                    extrudedGeometry,
                    localSurfacePlane);
            var meshVertices = ConvertVerticesToTargetLocal(
                meshData.Vertices);

            runtimeMesh.Clear();
            runtimeMesh.vertices = meshVertices;
            runtimeMesh.triangles = meshData.Triangles;
            runtimeMesh.RecalculateBounds();
            runtimeMesh.RecalculateNormals();
            SetRendererVisible(meshData.Triangles.Length > 0);
        }

        private Vector3[] ConvertVerticesToTargetLocal(
            Vector3[] sourceLocalVertices)
        {
            var sourceTransform = SourceVolume.transform;
            var targetTransform = waterMeshFilter.transform;

            if (sourceTransform == targetTransform)
                return sourceLocalVertices;

            var sourceToTarget =
                targetTransform.worldToLocalMatrix
                * sourceTransform.localToWorldMatrix;
            var converted = new Vector3[sourceLocalVertices.Length];

            for (var index = 0; index < sourceLocalVertices.Length; index++)
            {
                converted[index] =
                    sourceToTarget.MultiplyPoint3x4(
                        sourceLocalVertices[index]);
            }

            return converted;
        }

        private void EnsureMesh()
        {
            if (runtimeMesh != null)
                return;

            ReleaseMesh();

            runtimeMesh = new Mesh
            {
                name = $"{name} Flood Water",
            };
            runtimeMesh.MarkDynamic();
            waterMeshFilter.sharedMesh = runtimeMesh;
            waterMeshRenderer = waterMeshFilter.GetComponent<MeshRenderer>();
        }

        private void SetRendererVisible(bool visible)
        {
            if (waterMeshRenderer == null && waterMeshFilter != null)
            {
                waterMeshRenderer =
                    waterMeshFilter.GetComponent<MeshRenderer>();
            }

            if (waterMeshRenderer != null)
                waterMeshRenderer.enabled = visible;
        }

        private void ReleaseMesh()
        {
            if (runtimeMesh == null)
                return;

            if (waterMeshFilter != null
                && waterMeshFilter.sharedMesh == runtimeMesh)
            {
                waterMeshFilter.sharedMesh = null;
            }

            if (Application.isPlaying)
                Destroy(runtimeMesh);
            else
                DestroyImmediate(runtimeMesh);

            runtimeMesh = null;
        }
    }
}
