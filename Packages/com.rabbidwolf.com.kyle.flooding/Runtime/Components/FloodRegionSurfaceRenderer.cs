using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presents one continuous free surface for a <see cref="FloodRegion"/>
    /// from composite geometry and the region's shared <see cref="FloodState"/>.
    /// </summary>
    /// <remarks>
    /// Member-volume surface renderers must not also draw for composed members;
    /// use this component for region-level presentation.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class FloodRegionSurfaceRenderer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Flood region whose immutable state drives this renderer.")]
        private FloodRegion floodRegion;

        [SerializeField]
        [Tooltip("Child transform that receives the submerged water mesh (Mesh Filter required).")]
        private Transform waterVisual;

        [SerializeField]
        [Tooltip("Seconds used to interpolate toward each published state. Set to zero to apply states immediately.")]
        [Min(0f)]
        private float interpolationDuration = 0.1f;

        [SerializeField]
        [Tooltip("Water height in meters below which the visual is hidden.")]
        [Min(0f)]
        private float minimumVisibleHeight = 0.01f;

        private MeshFilter waterMeshFilter;
        private Mesh runtimeMesh;
        private FloodState interpolationStart;
        private FloodState targetState;
        private FloodState displayedState;
        private float interpolationElapsed;
        private bool hasDisplayedState;
        private bool isSubscribed;

        /// <summary>
        /// Gets or sets the flood region that drives this renderer.
        /// </summary>
        public FloodRegion SourceRegion
        {
            get => floodRegion;
            set
            {
                if (floodRegion == value)
                    return;

                Unsubscribe();
                floodRegion = value;
                Subscribe();
                SnapToCurrentState();
            }
        }

        /// <summary>
        /// Gets the state most recently applied to the mesh.
        /// </summary>
        public FloodState DisplayedState => displayedState;

        private void Reset()
        {
            floodRegion = GetComponent<FloodRegion>();
        }

        private void Awake()
        {
            if (floodRegion == null)
                floodRegion = GetComponent<FloodRegion>();

            ResolveMeshFilter();
        }

        private void OnEnable()
        {
            Subscribe();
            WarnIfMemberRenderersPresent();
        }

        private void Start()
        {
            SnapToCurrentState();
        }

        private void Update()
        {
            UpdateInterpolation(Time.deltaTime);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (runtimeMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(runtimeMesh);
                else
                    DestroyImmediate(runtimeMesh);
            }
        }

        private void OnValidate()
        {
            interpolationDuration = Mathf.Max(0f, interpolationDuration);
            minimumVisibleHeight = Mathf.Max(0f, minimumVisibleHeight);

            if (floodRegion == null)
                floodRegion = GetComponent<FloodRegion>();
        }

        /// <summary>
        /// Immediately applies the source region's current state.
        /// </summary>
        public void SnapToCurrentState()
        {
            if (floodRegion == null)
                return;

            SetDisplayedState(floodRegion.CurrentState);
            interpolationStart = displayedState;
            targetState = displayedState;
            interpolationElapsed = interpolationDuration;
            hasDisplayedState = true;
        }

        private void HandleStateChanged(FloodState state)
        {
            // Occupancy free-surface rebuilds are relatively expensive; snap
            // instead of interpolating so mesh work stays on publish ticks.
            if (floodRegion != null
                && floodRegion.Geometry is BakedFloodGeometry)
            {
                SetDisplayedState(state);
                interpolationStart = state;
                targetState = state;
                interpolationElapsed = interpolationDuration;
                return;
            }

            if (!hasDisplayedState || interpolationDuration <= 0f)
            {
                SetDisplayedState(state);
                interpolationStart = state;
                targetState = state;
                interpolationElapsed = interpolationDuration;
                return;
            }

            interpolationStart = displayedState;
            targetState = state;
            interpolationElapsed = 0f;
        }

        private void UpdateInterpolation(float deltaTime)
        {
            if (!hasDisplayedState || displayedState == targetState)
                return;

            if (interpolationDuration <= 0f)
            {
                SetDisplayedState(targetState);
                return;
            }

            interpolationElapsed += Mathf.Max(0f, deltaTime);
            var interpolation =
                Mathf.Clamp01(interpolationElapsed / interpolationDuration);

            if (interpolation >= 1f)
            {
                SetDisplayedState(targetState);
                return;
            }

            SetDisplayedState(
                Interpolate(interpolationStart, targetState, interpolation));
        }

        private void SetDisplayedState(FloodState state)
        {
            displayedState = state;
            hasDisplayedState = true;
            ApplyState(state);
        }

        private static FloodState Interpolate(
            FloodState start,
            FloodState target,
            float interpolation)
        {
            var volume = Lerp(start.Volume, target.Volume, interpolation);
            var capacity = Lerp(start.Capacity, target.Capacity, interpolation);
            var height = Lerp(start.Height, target.Height, interpolation);
            var fill = Lerp(
                start.FillPercentage,
                target.FillPercentage,
                interpolation);
            var mass = Lerp(
                start.WaterMass,
                target.WaterMass,
                interpolation);

            return new FloodState(
                volume,
                capacity,
                height,
                fill,
                volume <= 0d,
                volume >= capacity,
                InterpolatePlane(
                    start.SurfacePlane,
                    target.SurfacePlane,
                    interpolation),
                mass,
                Vector3.Lerp(
                    start.WaterCenterOfMassWorld,
                    target.WaterCenterOfMassWorld,
                    interpolation));
        }

        private static Plane InterpolatePlane(
            Plane start,
            Plane target,
            float interpolation)
        {
            var normal = Vector3.Slerp(
                start.normal,
                target.normal,
                interpolation).normalized;
            var point = Vector3.Lerp(
                start.ClosestPointOnPlane(Vector3.zero),
                target.ClosestPointOnPlane(Vector3.zero),
                interpolation);
            return new Plane(normal, point);
        }

        private static double Lerp(double start, double target, float t)
        {
            return start + ((target - start) * t);
        }

        private void ApplyState(FloodState state)
        {
            if (floodRegion == null || waterVisual == null)
                return;

            var height = (float)state.Height;

            if (height <= minimumVisibleHeight || state.IsEmpty)
            {
                waterVisual.gameObject.SetActive(false);
                return;
            }

            waterVisual.gameObject.SetActive(true);
            ResolveMeshFilter();

            if (waterMeshFilter == null)
                return;

            EnsureRuntimeMesh();
            var localSurfacePlane = FloodPlaneUtility.WorldToLocal(
                floodRegion.transform,
                state.SurfacePlane);

            switch (floodRegion.Geometry)
            {
                case TwoBoxInclusionExclusionGeometry union
                    when union.PresentationGeometry != null:
                    ApplyExtrudedMesh(
                        union.PresentationGeometry,
                        localSurfacePlane);
                    break;
                case IExtrudedFloodVolumeGeometry extruded
                    when extruded.Footprint.Count >= 3:
                    ApplyExtrudedMesh(extruded, localSurfacePlane);
                    break;
                case BakedFloodGeometry:
                    ApplyOccupancySurfaceMesh(
                        floodRegion.Geometry,
                        localSurfacePlane);
                    break;
                default:
                    waterVisual.gameObject.SetActive(false);
                    return;
            }

            waterVisual.localPosition = Vector3.zero;
            waterVisual.localRotation = Quaternion.identity;
            waterVisual.localScale = Vector3.one;
        }

        private void ApplyExtrudedMesh(
            IExtrudedFloodVolumeGeometry geometry,
            Plane localSurfacePlane)
        {
            var meshData = FloodExtrudedGeometryQueries.BuildSubmergedMesh(
                geometry,
                localSurfacePlane);

            runtimeMesh.Clear();
            runtimeMesh.vertices = meshData.Vertices;
            runtimeMesh.triangles = meshData.Triangles;
            runtimeMesh.RecalculateBounds();
            runtimeMesh.RecalculateNormals();
        }

        private void ApplyOccupancySurfaceMesh(
            IFloodVolumeGeometry geometry,
            Plane localSurfacePlane)
        {
            FloodSurfaceIntersection intersection;
            if (geometry is BakedFloodGeometry baked)
            {
                intersection = baked.EvaluateFreeSurface(localSurfacePlane);
            }
            else
            {
                intersection = geometry.EvaluateSubmersion(localSurfacePlane)
                    .SurfaceIntersection;
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            var planeNormal = localSurfacePlane.normal;
            if (floodRegion != null && waterVisual != null
                && floodRegion.transform != waterVisual)
            {
                planeNormal = waterVisual.InverseTransformDirection(
                    floodRegion.transform.TransformDirection(planeNormal));
            }

            foreach (var contour in intersection.Contours)
            {
                if (contour.Vertices.Count < 3)
                    continue;

                var visualContour = new List<Vector3>(contour.Vertices.Count);
                foreach (var point in contour.Vertices)
                    visualContour.Add(ConvertToWaterVisualLocal(point));

                FloodPlanarPolygonTriangulation.AppendContour(
                    visualContour,
                    planeNormal,
                    vertices,
                    triangles);
            }

            runtimeMesh.Clear();
            runtimeMesh.SetVertices(vertices);
            runtimeMesh.SetTriangles(triangles, 0);
            runtimeMesh.RecalculateBounds();
            runtimeMesh.RecalculateNormals();

            if (triangles.Count == 0)
                waterVisual.gameObject.SetActive(false);
        }

        private Vector3 ConvertToWaterVisualLocal(Vector3 regionLocalPoint)
        {
            if (floodRegion == null || waterVisual == null)
                return regionLocalPoint;

            if (floodRegion.transform == waterVisual)
                return regionLocalPoint;

            return waterVisual.InverseTransformPoint(
                floodRegion.transform.TransformPoint(regionLocalPoint));
        }

        private void WarnIfMemberRenderersPresent()
        {
            if (floodRegion == null)
                return;

            for (var index = 0; index < floodRegion.BoundMembers.Count; index++)
            {
                var member = floodRegion.BoundMembers[index];
                if (member == null)
                    continue;

                var renderer = member.GetComponent<FloodSurfaceRenderer>();
                if (renderer != null && renderer.enabled)
                {
                    Debug.LogWarning(
                        $"FloodVolume '{member.name}' has an enabled surface "
                        + "renderer while belonging to FloodRegion "
                        + $"'{floodRegion.name}'. Disable member renderers and "
                        + "use FloodRegionSurfaceRenderer for continuous "
                        + "presentation.",
                        renderer);
                }
            }
        }

        private void Subscribe()
        {
            if (isSubscribed || floodRegion == null)
                return;

            floodRegion.StateChanged += HandleStateChanged;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || floodRegion == null)
                return;

            floodRegion.StateChanged -= HandleStateChanged;
            isSubscribed = false;
        }

        private void ResolveMeshFilter()
        {
            if (waterVisual == null)
                return;

            waterMeshFilter = waterVisual.GetComponent<MeshFilter>();
        }

        private void EnsureRuntimeMesh()
        {
            if (runtimeMesh != null)
            {
                waterMeshFilter.sharedMesh = runtimeMesh;
                return;
            }

            runtimeMesh = new Mesh
            {
                name = "Flood Region Surface",
            };
            runtimeMesh.MarkDynamic();
            waterMeshFilter.sharedMesh = runtimeMesh;
        }
    }
}
