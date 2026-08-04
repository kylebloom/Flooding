using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presents the free surface of baked geometry. Volume comes from occupancy
    /// cells; when the bake includes a presentation boundary, contours are the
    /// gravity plane intersected with that immutable boundary mesh rather than
    /// per-cell voxel patches. Runtime never analyzes a live source Mesh Filter.
    /// </summary>
    public sealed class FloodBakedSurfaceRenderer : FloodSurfaceRenderer
    {
        [SerializeField]
        [Tooltip("Mesh Filter on the child water-surface GameObject. The component generates only the gravity-aligned free surface.")]
        private MeshFilter waterMeshFilter;

        [SerializeField]
        [Tooltip("Water volume in cubic meters below which the target Mesh Renderer is disabled.")]
        [Min(0f)]
        private float minimumVisibleVolume = 0.001f;

        private Mesh runtimeMesh;
        private MeshRenderer waterMeshRenderer;

        /// <summary>Gets or sets the generated surface target.</summary>
        public MeshFilter WaterMeshFilter
        {
            get => waterMeshFilter;
            set
            {
                if (waterMeshFilter == value)
                    return;

                ReleaseMesh();
                waterMeshFilter = value;
                ResolveRenderer();
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
            ResolveRenderer();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            minimumVisibleVolume = Mathf.Max(0f, minimumVisibleVolume);
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

            var geometry = SourceVolume.Geometry;
            if (geometry == null
                || state.Volume <= minimumVisibleVolume
                || state.Volume >= state.Capacity
                    - FloodGeometryTolerances.SolverAbsoluteVolume)
            {
                SetVisible(false);
                return;
            }

            var localPlane = FloodPlaneUtility.WorldToLocal(
                SourceVolume.transform,
                state.SurfacePlane);
            var intersection =
                geometry.EvaluateSubmersion(localPlane).SurfaceIntersection;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            foreach (var contour in intersection.Contours)
            {
                if (contour.Vertices.Count < 3)
                    continue;

                var offset = vertices.Count;
                foreach (var point in contour.Vertices)
                    vertices.Add(ConvertToTargetLocal(point));

                for (var index = 1;
                     index < contour.Vertices.Count - 1;
                     index++)
                {
                    triangles.Add(offset);
                    triangles.Add(offset + index);
                    triangles.Add(offset + index + 1);
                }
            }

            EnsureMesh();
            runtimeMesh.Clear();
            runtimeMesh.SetVertices(vertices);
            runtimeMesh.SetTriangles(triangles, 0);
            runtimeMesh.RecalculateBounds();
            runtimeMesh.RecalculateNormals();
            SetVisible(triangles.Count > 0);
        }

        private Vector3 ConvertToTargetLocal(Vector3 sourceLocalPoint)
        {
            if (SourceVolume.transform == waterMeshFilter.transform)
                return sourceLocalPoint;

            return waterMeshFilter.transform.InverseTransformPoint(
                SourceVolume.transform.TransformPoint(sourceLocalPoint));
        }

        private void EnsureMesh()
        {
            if (runtimeMesh != null)
                return;

            runtimeMesh = new Mesh
            {
                name = $"{name} Baked Flood Surface",
            };
            runtimeMesh.MarkDynamic();
            waterMeshFilter.sharedMesh = runtimeMesh;
            ResolveRenderer();
        }

        private void ResolveRenderer()
        {
            waterMeshRenderer = waterMeshFilter == null
                ? null
                : waterMeshFilter.GetComponent<MeshRenderer>();
        }

        private void SetVisible(bool visible)
        {
            if (waterMeshRenderer == null)
                ResolveRenderer();
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
