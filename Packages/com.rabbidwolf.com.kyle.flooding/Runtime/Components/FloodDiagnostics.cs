using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Captures read-only flooding and Rigidbody state for Editor diagnostics.
    /// </summary>
    [AddComponentMenu("Flooding/Flood Diagnostics")]
    [DisallowMultipleComponent]
    public sealed class FloodDiagnostics : MonoBehaviour
    {
        [Header("Sources")]

        [SerializeField]
        [Tooltip("Manager whose active world-space gravity is displayed. Leave empty to discover one below this diagnostic root.")]
        private FloodSimulationManager simulationManager;

        [SerializeField]
        [Tooltip("Adapter that supplies the configured dry mass and Rigidbody-local dry center of mass. Leave empty to discover one below this diagnostic root.")]
        private RigidbodyFloodMassAdapter massAdapter;

        [SerializeField]
        [Tooltip("Rigidbody whose current combined mass and world-space center of mass are displayed. Leave empty to discover one below this diagnostic root.")]
        private Rigidbody targetRigidbody;

        [SerializeField]
        [Tooltip("When enabled, FloodVolume and FloodConnection components are discovered below this GameObject, including inactive children.")]
        private bool discoverChildren = true;

        [SerializeField]
        [Tooltip("Explicit FloodVolume sources used when Discover Children is disabled. Their state uses cubic meters, kilograms, and world-space meters.")]
        private FloodVolume[] volumes = Array.Empty<FloodVolume>();

        [SerializeField]
        [Tooltip("Explicit FloodConnection sources used when Discover Children is disabled. Their head is meters and rates are cubic meters per second.")]
        private FloodConnection[] connections = Array.Empty<FloodConnection>();

        [Header("Visibility")]

        [SerializeField]
        [Tooltip("Displays aggregate water, configured dry, and current combined Rigidbody centers of mass in world-space meters.")]
        private bool showCentersOfMass = true;

        [SerializeField]
        [Tooltip("Displays the manager's active world-space gravity vector in meters per second squared.")]
        private bool showGravity = true;

        [SerializeField]
        [Tooltip("Displays each FloodVolume solved world-space surface plane and water volume in cubic meters.")]
        private bool showSurfacePlanes = true;

        [SerializeField]
        [Tooltip("Displays each FloodConnection direction, pressure head in meters, and requested/applied rates in cubic meters per second.")]
        private bool showConnections = true;

        [Header("Scene View Scale")]

        [SerializeField]
        [Tooltip("World-space radius in meters for center-of-mass markers.")]
        [Min(0.001f)]
        private float centerOfMassMarkerRadius = 0.15f;

        [SerializeField]
        [Tooltip("World-space length in meters used to draw the active-gravity arrow.")]
        [Min(0.001f)]
        private float gravityArrowLength = 2f;

        [SerializeField]
        [Tooltip("World-space side length in meters used to draw each solved surface plane.")]
        [Min(0.001f)]
        private float surfacePlaneSize = 2f;

        [SerializeField]
        [Tooltip("World-space length in meters used to draw each connection flow arrow.")]
        [Min(0.001f)]
        private float flowArrowLength = 1f;

        [Header("Scene View Colors")]

        [SerializeField]
        [Tooltip("Scene-view color for the aggregate water center-of-mass marker.")]
        private Color waterCenterColor = new(0f, 0.65f, 1f, 1f);

        [SerializeField]
        [Tooltip("Scene-view color for the configured dry center-of-mass marker.")]
        private Color dryCenterColor = new(1f, 0.75f, 0f, 1f);

        [SerializeField]
        [Tooltip("Scene-view color for the current combined Rigidbody center-of-mass marker.")]
        private Color combinedCenterColor = new(0.85f, 0.2f, 1f, 1f);

        [SerializeField]
        [Tooltip("Scene-view color for active gravity measured in meters per second squared.")]
        private Color gravityColor = new(1f, 0.25f, 0.2f, 1f);

        [SerializeField]
        [Tooltip("Scene-view color for solved water surface planes.")]
        private Color surfacePlaneColor = new(0.1f, 0.8f, 1f, 1f);

        [SerializeField]
        [Tooltip("Scene-view color for connection flow arrows and rate labels.")]
        private Color connectionColor = new(0.2f, 1f, 0.35f, 1f);

        public bool ShowCentersOfMass => showCentersOfMass;
        public bool ShowGravity => showGravity;
        public bool ShowSurfacePlanes => showSurfacePlanes;
        public bool ShowConnections => showConnections;
        public float CenterOfMassMarkerRadius => centerOfMassMarkerRadius;
        public float GravityArrowLength => gravityArrowLength;
        public float SurfacePlaneSize => surfacePlaneSize;
        public float FlowArrowLength => flowArrowLength;
        public Color WaterCenterColor => waterCenterColor;
        public Color DryCenterColor => dryCenterColor;
        public Color CombinedCenterColor => combinedCenterColor;
        public Color GravityColor => gravityColor;
        public Color SurfacePlaneColor => surfacePlaneColor;
        public Color ConnectionColor => connectionColor;

        private void Reset()
        {
            simulationManager =
                GetComponentInChildren<FloodSimulationManager>(true);
            massAdapter =
                GetComponentInChildren<RigidbodyFloodMassAdapter>(true);
            targetRigidbody = GetComponentInChildren<Rigidbody>(true);
        }

        private void OnValidate()
        {
            centerOfMassMarkerRadius =
                SanitizePositive(centerOfMassMarkerRadius, 0.15f);
            gravityArrowLength =
                SanitizePositive(gravityArrowLength, 2f);
            surfacePlaneSize =
                SanitizePositive(surfacePlaneSize, 2f);
            flowArrowLength =
                SanitizePositive(flowArrowLength, 1f);
        }

        /// <summary>
        /// Captures current public state without writing to any observed object.
        /// </summary>
        public FloodDiagnosticSnapshot CaptureSnapshot()
        {
            var resolvedVolumes = discoverChildren
                ? GetComponentsInChildren<FloodVolume>(true)
                : volumes ?? Array.Empty<FloodVolume>();
            var resolvedConnections = discoverChildren
                ? GetComponentsInChildren<FloodConnection>(true)
                : connections ?? Array.Empty<FloodConnection>();
            var manager = simulationManager != null
                ? simulationManager
                : discoverChildren
                    ? GetComponentInChildren<FloodSimulationManager>(true)
                    : null;
            var adapter = massAdapter != null
                ? massAdapter
                : discoverChildren
                    ? GetComponentInChildren<RigidbodyFloodMassAdapter>(true)
                    : null;
            var body = targetRigidbody != null
                ? targetRigidbody
                : adapter != null
                    ? adapter.GetComponent<Rigidbody>()
                    : discoverChildren
                        ? GetComponentInChildren<Rigidbody>(true)
                        : null;

            var volumeSnapshots =
                new List<FloodVolumeDiagnostic>(resolvedVolumes.Length);
            var states = new List<FloodState>(resolvedVolumes.Length);

            foreach (var volume in resolvedVolumes)
            {
                if (volume == null)
                    continue;

                var state = volume.CurrentState;
                states.Add(state);
                volumeSnapshots.Add(
                    new FloodVolumeDiagnostic(volume, state));
            }

            var water = FloodDiagnosticMath.CombineWaterMass(
                states,
                transform.position);
            var connectionSnapshots =
                new List<FloodConnectionDiagnostic>(
                    resolvedConnections.Length);

            foreach (var connection in resolvedConnections)
            {
                if (connection != null)
                {
                    connectionSnapshots.Add(
                        FloodDiagnosticMath.CaptureConnection(connection));
                }
            }

            var hasDryCenter = adapter != null && body != null;
            var dryCenter = hasDryCenter
                ? body.transform.TransformPoint(
                    adapter.DryCenterOfMassLocal)
                : transform.position;

            return new FloodDiagnosticSnapshot(
                transform.position,
                manager != null,
                manager != null ? manager.ActiveGravity : Vector3.zero,
                water,
                hasDryCenter,
                adapter != null ? adapter.DryMass : 0d,
                dryCenter,
                body != null,
                body != null ? body.mass : 0d,
                body != null ? body.worldCenterOfMass : transform.position,
                volumeSnapshots.ToArray(),
                connectionSnapshots.ToArray());
        }

        private static float SanitizePositive(float value, float fallback)
        {
            return float.IsNaN(value)
                || float.IsInfinity(value)
                || value <= 0f
                    ? fallback
                    : value;
        }
    }

    /// <summary>
    /// One read-only capture of all configured diagnostic sources.
    /// </summary>
    public readonly struct FloodDiagnosticSnapshot
    {
        internal FloodDiagnosticSnapshot(
            Vector3 originWorld,
            bool hasGravity,
            Vector3 activeGravityWorld,
            FloodMassContribution water,
            bool hasDryCenter,
            double dryMass,
            Vector3 dryCenterWorld,
            bool hasCombinedCenter,
            double combinedMass,
            Vector3 combinedCenterWorld,
            FloodVolumeDiagnostic[] volumes,
            FloodConnectionDiagnostic[] connections)
        {
            OriginWorld = originWorld;
            HasGravity = hasGravity;
            ActiveGravityWorld = activeGravityWorld;
            Water = water;
            HasDryCenter = hasDryCenter;
            DryMass = dryMass;
            DryCenterWorld = dryCenterWorld;
            HasCombinedCenter = hasCombinedCenter;
            CombinedMass = combinedMass;
            CombinedCenterWorld = combinedCenterWorld;
            Volumes = volumes;
            Connections = connections;
        }

        public Vector3 OriginWorld { get; }
        public bool HasGravity { get; }
        public Vector3 ActiveGravityWorld { get; }
        public FloodMassContribution Water { get; }
        public bool HasDryCenter { get; }
        public double DryMass { get; }
        public Vector3 DryCenterWorld { get; }
        public bool HasCombinedCenter { get; }
        public double CombinedMass { get; }
        public Vector3 CombinedCenterWorld { get; }
        public IReadOnlyList<FloodVolumeDiagnostic> Volumes { get; }
        public IReadOnlyList<FloodConnectionDiagnostic> Connections { get; }
    }

    /// <summary>
    /// Read-only diagnostic values for one flood volume.
    /// </summary>
    public readonly struct FloodVolumeDiagnostic
    {
        internal FloodVolumeDiagnostic(FloodVolume source, FloodState state)
        {
            Source = source;
            State = state;
        }

        public FloodVolume Source { get; }
        public FloodState State { get; }
    }

    /// <summary>
    /// Read-only diagnostic values for one flood connection.
    /// </summary>
    public readonly struct FloodConnectionDiagnostic
    {
        internal FloodConnectionDiagnostic(
            FloodConnection source,
            Vector3 positionWorld,
            Vector3 directionWorld,
            double pressureHeadDifference,
            double requestedFlowRate,
            double appliedFlowRate)
        {
            Source = source;
            PositionWorld = positionWorld;
            DirectionWorld = directionWorld;
            PressureHeadDifference = pressureHeadDifference;
            RequestedFlowRate = requestedFlowRate;
            AppliedFlowRate = appliedFlowRate;
        }

        public FloodConnection Source { get; }
        public Vector3 PositionWorld { get; }
        public Vector3 DirectionWorld { get; }
        public double PressureHeadDifference { get; }
        public double RequestedFlowRate { get; }
        public double AppliedFlowRate { get; }
    }

    internal static class FloodDiagnosticMath
    {
        internal static FloodMassContribution CombineWaterMass(
            IReadOnlyList<FloodState> states,
            Vector3 emptyCenterWorld)
        {
            var totalMass = 0d;
            var weightedX = 0d;
            var weightedY = 0d;
            var weightedZ = 0d;

            for (var index = 0; index < states.Count; index++)
            {
                var state = states[index];

                if (state.WaterMass <= 0d)
                    continue;

                totalMass += state.WaterMass;
                weightedX +=
                    state.WaterMass * state.WaterCenterOfMassWorld.x;
                weightedY +=
                    state.WaterMass * state.WaterCenterOfMassWorld.y;
                weightedZ +=
                    state.WaterMass * state.WaterCenterOfMassWorld.z;
            }

            return totalMass > 0d
                ? new FloodMassContribution(
                    totalMass,
                    new Vector3(
                        (float)(weightedX / totalMass),
                        (float)(weightedY / totalMass),
                        (float)(weightedZ / totalMass)))
                : new FloodMassContribution(0d, emptyCenterWorld);
        }

        internal static FloodConnectionDiagnostic CaptureConnection(
            FloodConnection connection)
        {
            var direction = ResolveFlowDirection(
                connection.transform.forward,
                connection.RequestedFlowRate,
                connection.CurrentFlowRate);

            return new FloodConnectionDiagnostic(
                connection,
                connection.transform.position,
                direction,
                connection.PressureHeadDifference,
                connection.RequestedFlowRate,
                connection.CurrentFlowRate);
        }

        internal static Vector3 ResolveFlowDirection(
            Vector3 positiveDirection,
            double requestedFlowRate,
            double appliedFlowRate)
        {
            var signedRate = appliedFlowRate != 0d
                ? appliedFlowRate
                : requestedFlowRate;

            return signedRate > 0d
                ? positiveDirection.normalized
                : signedRate < 0d
                    ? -positiveDirection.normalized
                    : Vector3.zero;
        }
    }
}
