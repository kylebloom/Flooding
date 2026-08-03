using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presents rectangular flood state by scaling and positioning a child
    /// transform along the compartment's local Y axis.
    /// </summary>
    public class FloodCubeSurfaceRenderer : FloodSurfaceRenderer
    {
        [SerializeField]
        [Tooltip("Child transform scaled and positioned to represent the rectangular water body.")]
        private Transform waterVisual;

        [SerializeField]
        [Tooltip("Water height in meters below which the visual is hidden.")]
        [Min(0f)]
        private float minimumVisibleHeight = 0.01f;

        private MeshFilter waterMeshFilter;
        private Mesh originalMesh;
        private Mesh runtimeMesh;

        /// <summary>
        /// Gets or sets the transform used to display the water body.
        /// </summary>
        public Transform WaterVisual
        {
            get => waterVisual;
            set
            {
                ReleaseMesh();
                waterVisual = value;
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

        protected override void OnValidate()
        {
            base.OnValidate();
            minimumVisibleHeight = Mathf.Max(0f, minimumVisibleHeight);
        }

        protected override void Awake()
        {
            base.Awake();
            ResolveMeshFilter();
        }

        private void OnDestroy()
        {
            ReleaseMesh();
        }

        /// <inheritdoc />
        protected override void ApplyState(FloodState state)
        {
            if (SourceVolume == null || waterVisual == null)
                return;

            if (SourceVolume.GeometryMode
                != FloodGeometryMode.RectangularPrism)
            {
                waterVisual.gameObject.SetActive(false);
                return;
            }

            var height = (float)state.Height;

            if (height <= minimumVisibleHeight)
            {
                waterVisual.gameObject.SetActive(false);
                return;
            }

            waterVisual.gameObject.SetActive(true);

            ResolveMeshFilter();

            if (waterMeshFilter != null
                && SourceVolume.Geometry
                    is IExtrudedFloodVolumeGeometry geometry)
            {
                EnsureRuntimeMesh();
                var localSurfacePlane = FloodPlaneUtility.WorldToLocal(
                    SourceVolume.transform,
                    state.SurfacePlane);
                var meshData =
                    FloodExtrudedGeometryQueries.BuildSubmergedMesh(
                        geometry,
                        localSurfacePlane);

                runtimeMesh.Clear();
                runtimeMesh.vertices = meshData.Vertices;
                runtimeMesh.triangles = meshData.Triangles;
                runtimeMesh.RecalculateBounds();
                runtimeMesh.RecalculateNormals();
                waterVisual.localPosition = Vector3.zero;
                waterVisual.localRotation = Quaternion.identity;
                waterVisual.localScale = Vector3.one;
                return;
            }

            waterVisual.localScale = new Vector3(
                SourceVolume.Width,
                height,
                SourceVolume.Length);

            waterVisual.localPosition = new Vector3(
                0f,
                height * 0.5f,
                0f);
        }

        private void ResolveMeshFilter()
        {
            if (waterVisual == null)
            {
                waterMeshFilter = null;
                return;
            }

            var resolved = waterVisual.GetComponent<MeshFilter>();

            if (waterMeshFilter == resolved)
                return;

            ReleaseMesh();
            waterMeshFilter = resolved;
        }

        private void EnsureRuntimeMesh()
        {
            if (runtimeMesh != null)
                return;

            originalMesh = waterMeshFilter.sharedMesh;
            runtimeMesh = new Mesh
            {
                name = $"{name} Flood Water",
            };
            runtimeMesh.MarkDynamic();
            waterMeshFilter.sharedMesh = runtimeMesh;
        }

        private void ReleaseMesh()
        {
            if (runtimeMesh == null)
                return;

            if (waterMeshFilter != null
                && waterMeshFilter.sharedMesh == runtimeMesh)
            {
                waterMeshFilter.sharedMesh = originalMesh;
            }

            if (Application.isPlaying)
                Destroy(runtimeMesh);
            else
                DestroyImmediate(runtimeMesh);

            runtimeMesh = null;
            originalMesh = null;
        }
    }
}
