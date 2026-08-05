using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only orchestrator for local ingress spread visuals.
    /// </summary>
    /// <remarks>
    /// Reads solver flow through <see cref="FloodIngressSampler"/> and never
    /// mutates <see cref="FloodVolume"/> state. Local patches are a transient
    /// visual proxy that converge toward the authoritative bulk surface.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Flood Local Ingress Presenter")]
    public sealed class FloodLocalIngressPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Target")]

        [SerializeField]
        [Tooltip("Flood volume whose ingress is visualized. Gameplay queries remain based on this volume's solver state.")]
        private FloodVolume volume;

        [SerializeField]
        [Tooltip("Presentation profile controlling spread, settling, convergence, and flow response curves.")]
        private FloodIngressPresentationProfile profile;

        [Header("Providers")]

        [SerializeField]
        [Tooltip("Flood connections that may deliver ingress into the target volume. Prefer explicit assignment; auto-discover runs only on enable/refresh.")]
        private FloodConnection[] connections = Array.Empty<FloodConnection>();

        [SerializeField]
        [Tooltip("Flood sources that may deliver ingress into the target volume. Prefer explicit assignment; auto-discover runs only on enable/refresh.")]
        private FloodSource[] sources = Array.Empty<FloodSource>();

        [SerializeField]
        [Tooltip("When enabled, discovers connections/sources targeting the volume once on enable or when RefreshProviders is called. Never searches every frame.")]
        private bool autoDiscoverProviders = true;

        [Header("Floor")]

        [SerializeField]
        [Tooltip("Presentation floor plane. Position is a point on the floor; up is the floor normal. Local patches align to this normal.")]
        private Transform floorPlane;

        [SerializeField]
        [Tooltip("When enabled, performs a one-shot downward raycast along -floor normal when a patch slot is first activated or reused.")]
        private bool raycastFloorOnPatchCreate;

        [SerializeField]
        [Tooltip("Physics layers used by the optional one-shot floor raycast.")]
        private LayerMask floorRaycastMask = ~0;

        [SerializeField]
        [Tooltip("Maximum one-shot floor raycast distance in meters.")]
        [Min(0.01f)]
        private float floorRaycastDistance = 8f;

        [Header("Visuals")]

        [SerializeField]
        [Tooltip("Material for pooled local ingress discs. Transparent water materials work best.")]
        private Material patchMaterial;

        [SerializeField]
        [Tooltip("Base color for local patches. Alpha is multiplied by local opacity after handoff.")]
        private Color patchColor = new(0.18f, 0.5f, 0.82f, 0.55f);

        [SerializeField]
        [Tooltip("Optional stream presenters paired with providers (index-aligned to connections then sources). Extra presenters are ignored; missing entries skip streams.")]
        private FloodIngressStreamPresenter[] streamPresenters =
            Array.Empty<FloodIngressStreamPresenter>();

        [SerializeField]
        [Tooltip("When disabled, local ingress presentation is suppressed without affecting simulation.")]
        private bool presentationEnabled = true;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Draws Scene-view gizmos for anchors, directions, and patch radii.")]
        private bool drawDebugGizmos = true;

        private FloodIngressPresentationState state;
        private FloodIngressSample[] sampleBuffer = Array.Empty<FloodIngressSample>();
        private DiscSlot[] discSlots = Array.Empty<DiscSlot>();
        private MaterialPropertyBlock propertyBlock;
        private bool providersResolved;

        /// <summary>
        /// Gets or sets the destination flood volume.
        /// </summary>
        public FloodVolume Volume
        {
            get => volume;
            set => volume = value;
        }

        /// <summary>
        /// Gets or sets the presentation profile.
        /// </summary>
        public FloodIngressPresentationProfile Profile
        {
            get => profile;
            set => profile = value;
        }

        /// <summary>
        /// Gets or sets whether local ingress presentation is active.
        /// </summary>
        public bool PresentationEnabled
        {
            get => presentationEnabled;
            set => presentationEnabled = value;
        }

        /// <summary>
        /// Gets or sets the presentation floor plane Transform.
        /// </summary>
        public Transform FloorPlane
        {
            get => floorPlane;
            set => floorPlane = value;
        }

        /// <summary>
        /// Gets or sets the patch material.
        /// </summary>
        public Material PatchMaterial
        {
            get => patchMaterial;
            set => patchMaterial = value;
        }

        /// <summary>
        /// Gets or sets explicit connection providers.
        /// </summary>
        public FloodConnection[] Connections
        {
            get => connections;
            set
            {
                connections = value ?? Array.Empty<FloodConnection>();
                providersResolved = true;
            }
        }

        /// <summary>
        /// Gets or sets explicit source providers.
        /// </summary>
        public FloodSource[] Sources
        {
            get => sources;
            set
            {
                sources = value ?? Array.Empty<FloodSource>();
                providersResolved = true;
            }
        }

        /// <summary>
        /// Gets the number of active local patches.
        /// </summary>
        public int ActivePatchCount => state?.ActivePatchCount ?? 0;

        /// <summary>
        /// Gets the oldest active patch age in seconds.
        /// </summary>
        public float OldestPatchAgeSeconds => state?.OldestPatchAgeSeconds ?? 0f;

        /// <summary>
        /// Gets the average handoff fraction across active patches.
        /// </summary>
        public float AverageHandoffFraction => state?.AverageHandoffFraction ?? 1f;

        /// <summary>
        /// Gets the latest summed inflow rate sampled this frame.
        /// </summary>
        public float CurrentInflowRateCubicMetersPerSecond { get; private set; }

        /// <summary>
        /// Gets a read-only view of presentation patch slots.
        /// </summary>
        public ReadOnlySpan<FloodIngressPatchState> Patches =>
            state != null ? state.Patches : ReadOnlySpan<FloodIngressPatchState>.Empty;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            EnsureState();
        }

        private void OnEnable()
        {
            EnsureState();
            if (autoDiscoverProviders
                && !providersResolved
                && !HasExplicitProviders())
            {
                RefreshProviders();
            }
            else
            {
                providersResolved = true;
            }
        }

        private void OnDisable()
        {
            HideAllDiscs();
            HideStreams(0f);
        }

        private void LateUpdate()
        {
            Refresh(Time.deltaTime);
        }

        /// <summary>
        /// Discovers connections and sources that target <see cref="Volume"/>.
        /// Intended for enable/setup — not per-frame use.
        /// </summary>
        public void RefreshProviders()
        {
            if (volume == null)
            {
                connections = Array.Empty<FloodConnection>();
                sources = Array.Empty<FloodSource>();
                providersResolved = true;
                return;
            }

            var foundConnections =
                UnityEngine.Object.FindObjectsByType<FloodConnection>();
            var connectionList = new FloodConnection[foundConnections.Length];
            var connectionCount = 0;
            for (var i = 0; i < foundConnections.Length; i++)
            {
                var connection = foundConnections[i];
                if (connection == null)
                    continue;

                if (connection.VolumeA == volume || connection.VolumeB == volume)
                    connectionList[connectionCount++] = connection;
            }

            connections = new FloodConnection[connectionCount];
            Array.Copy(connectionList, connections, connectionCount);

            var foundSources = UnityEngine.Object.FindObjectsByType<FloodSource>();
            var sourceList = new FloodSource[foundSources.Length];
            var sourceCount = 0;
            for (var i = 0; i < foundSources.Length; i++)
            {
                var source = foundSources[i];
                if (source != null && source.Target == volume)
                    sourceList[sourceCount++] = source;
            }

            sources = new FloodSource[sourceCount];
            Array.Copy(sourceList, sources, sourceCount);
            providersResolved = true;
        }

        /// <summary>
        /// Immediately refreshes local ingress presentation.
        /// </summary>
        public void Refresh(float deltaTime)
        {
            EnsureState();

            if (!presentationEnabled || !isActiveAndEnabled || volume == null || profile == null)
            {
                CurrentInflowRateCubicMetersPerSecond = 0f;
                state.Clear();
                HideAllDiscs();
                HideStreams(deltaTime);
                return;
            }

            var floorNormal = ResolveFloorNormal();
            var sampleCount = CollectSamples();
            CurrentInflowRateCubicMetersPerSecond = SumFlowRates(sampleCount);

            state.Tick(
                deltaTime,
                sampleBuffer.AsSpan(0, sampleCount),
                profile,
                floorNormal);

            SyncDiscVisuals();
            SyncStreams(deltaTime, sampleCount);
        }

        private void EnsureState()
        {
            var capacity = profile != null
                ? profile.MaximumSimultaneousPatches
                : FloodIngressPresentationProfile.DefaultMaximumSimultaneousPatches;

            if (state == null)
                state = new FloodIngressPresentationState(capacity);
            else
                state.EnsureCapacity(capacity);

            EnsureDiscSlots(capacity);

            var maxSamples =
                (connections?.Length ?? 0) + (sources?.Length ?? 0);
            if (sampleBuffer.Length < maxSamples)
                sampleBuffer = new FloodIngressSample[Math.Max(4, maxSamples)];
        }

        private int CollectSamples()
        {
            var count = 0;

            if (connections != null)
            {
                for (var i = 0; i < connections.Length; i++)
                {
                    if (FloodIngressSampler.TrySample(
                            connections[i],
                            volume,
                            out var sample))
                    {
                        sampleBuffer[count++] = sample;
                    }
                }
            }

            if (sources != null)
            {
                for (var i = 0; i < sources.Length; i++)
                {
                    if (FloodIngressSampler.TrySample(
                            sources[i],
                            volume,
                            out var sample))
                    {
                        sampleBuffer[count++] = sample;
                    }
                }
            }

            return count;
        }

        private float SumFlowRates(int sampleCount)
        {
            var sum = 0f;
            for (var i = 0; i < sampleCount; i++)
                sum += sampleBuffer[i].FlowRateCubicMetersPerSecond;
            return sum;
        }

        private void EnsureDiscSlots(int capacity)
        {
            if (discSlots.Length == capacity)
                return;

            for (var i = capacity; i < discSlots.Length; i++)
                DestroySlot(ref discSlots[i]);

            var next = new DiscSlot[capacity];
            var copy = Math.Min(capacity, discSlots.Length);
            Array.Copy(discSlots, next, copy);
            discSlots = next;

            for (var i = 0; i < discSlots.Length; i++)
            {
                if (discSlots[i].Transform == null)
                    discSlots[i] = CreateDiscSlot(i);
            }
        }

        private DiscSlot CreateDiscSlot(int index)
        {
            var discObject = new GameObject($"Ingress Patch {index}");
            discObject.transform.SetParent(transform, false);
            discObject.SetActive(false);

            var filter = discObject.AddComponent<MeshFilter>();
            filter.sharedMesh = FloodIngressDiscMesh.SharedUnitDisc;
            var renderer = discObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = patchMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return new DiscSlot
            {
                Transform = discObject.transform,
                Renderer = renderer,
                HasFloorProjection = false,
            };
        }

        private void SyncDiscVisuals()
        {
            propertyBlock ??= new MaterialPropertyBlock();
            var patches = state.Patches;
            var offset = profile != null ? profile.FloorOffsetMeters : 0.01f;

            for (var i = 0; i < discSlots.Length; i++)
            {
                ref var slot = ref discSlots[i];
                if (i >= patches.Length || !patches[i].IsActive)
                {
                    if (slot.Transform != null)
                        slot.Transform.gameObject.SetActive(false);
                    slot.HasFloorProjection = false;
                    continue;
                }

                var patch = patches[i];
                if (slot.Transform == null)
                    slot = CreateDiscSlot(i);

                if (!slot.HasFloorProjection)
                {
                    slot.ProjectedCenter = ProjectToFloor(
                        patch.CenterWorld,
                        patch.FloorNormalWorld,
                        offset,
                        allowRaycast: raycastFloorOnPatchCreate);
                    slot.HasFloorProjection = true;
                }
                else
                {
                    // Keep projection sticky for the provider-owned slot; update
                    // laterally if the ingress anchor moves significantly.
                    var planar = ProjectToFloor(
                        patch.CenterWorld,
                        patch.FloorNormalWorld,
                        offset,
                        allowRaycast: false);
                    slot.ProjectedCenter = planar;
                }

                var normal = patch.FloorNormalWorld.sqrMagnitude > 0.0001f
                    ? patch.FloorNormalWorld.normalized
                    : Vector3.up;
                slot.Transform.gameObject.SetActive(true);
                slot.Transform.SetPositionAndRotation(
                    slot.ProjectedCenter,
                    Quaternion.FromToRotation(Vector3.up, normal));

                var radius = Mathf.Max(0.01f, patch.CurrentRadius);
                slot.Transform.localScale = new Vector3(radius, 1f, radius);

                if (slot.Renderer != null)
                {
                    if (patchMaterial != null)
                        slot.Renderer.sharedMaterial = patchMaterial;

                    var color = patchColor;
                    color.a *= patch.LocalOpacity;
                    slot.Renderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor(BaseColorId, color);
                    propertyBlock.SetColor(ColorId, color);
                    slot.Renderer.SetPropertyBlock(propertyBlock);
                    slot.Renderer.enabled = color.a > 0.001f;
                }
            }
        }

        private Vector3 ProjectToFloor(
            Vector3 worldPosition,
            Vector3 floorNormal,
            float offset,
            bool allowRaycast)
        {
            if (floorNormal.sqrMagnitude <= 0.0001f)
                floorNormal = Vector3.up;
            else
                floorNormal = floorNormal.normalized;

            var planePoint = floorPlane != null ? floorPlane.position : transform.position;
            if (floorPlane != null)
                floorNormal = floorPlane.up.sqrMagnitude > 0.0001f
                    ? floorPlane.up.normalized
                    : floorNormal;

            if (allowRaycast)
            {
                var origin = worldPosition + (floorNormal * 0.25f);
                if (Physics.Raycast(
                        origin,
                        -floorNormal,
                        out var hit,
                        floorRaycastDistance,
                        floorRaycastMask,
                        QueryTriggerInteraction.Ignore))
                {
                    return hit.point + (hit.normal.normalized * offset);
                }
            }

            var toPoint = worldPosition - planePoint;
            var projected = worldPosition - (Vector3.Dot(toPoint, floorNormal) * floorNormal);
            return projected + (floorNormal * offset);
        }

        private Vector3 ResolveFloorNormal()
        {
            if (floorPlane != null && floorPlane.up.sqrMagnitude > 0.0001f)
                return floorPlane.up.normalized;

            return Vector3.up;
        }

        private void SyncStreams(float deltaTime, int sampleCount)
        {
            if (streamPresenters == null || streamPresenters.Length == 0)
                return;

            for (var i = 0; i < streamPresenters.Length; i++)
            {
                var stream = streamPresenters[i];
                if (stream == null)
                    continue;

                var matched = false;
                for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                {
                    // Pair by provider order: connections then sources.
                    if (TryGetProviderId(i, out var providerId)
                        && sampleBuffer[sampleIndex].ProviderId.Equals(providerId))
                    {
                        stream.Apply(sampleBuffer[sampleIndex], profile, deltaTime);
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                    stream.Hide(deltaTime);
            }
        }

        private bool TryGetProviderId(int streamIndex, out EntityId providerId)
        {
            providerId = default;
            var connectionCount = connections?.Length ?? 0;
            if (streamIndex < connectionCount)
            {
                var connection = connections[streamIndex];
                if (connection == null)
                    return false;

                providerId = connection.GetEntityId();
                return true;
            }

            var sourceIndex = streamIndex - connectionCount;
            if (sources == null || sourceIndex < 0 || sourceIndex >= sources.Length)
                return false;

            var source = sources[sourceIndex];
            if (source == null)
                return false;

            providerId = source.GetEntityId();
            return true;
        }

        private void HideStreams(float deltaTime)
        {
            if (streamPresenters == null)
                return;

            for (var i = 0; i < streamPresenters.Length; i++)
                streamPresenters[i]?.Hide(deltaTime);
        }

        private void HideAllDiscs()
        {
            for (var i = 0; i < discSlots.Length; i++)
            {
                if (discSlots[i].Transform != null)
                    discSlots[i].Transform.gameObject.SetActive(false);
                discSlots[i].HasFloorProjection = false;
            }
        }

        private static void DestroySlot(ref DiscSlot slot)
        {
            if (slot.Transform != null)
                Destroy(slot.Transform.gameObject);

            slot = default;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
                return;

            if (floorPlane != null)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 0.4f, 0.8f);
                Gizmos.DrawLine(
                    floorPlane.position,
                    floorPlane.position + (floorPlane.up * 0.5f));
            }

            if (connections != null)
            {
                for (var i = 0; i < connections.Length; i++)
                {
                    var connection = connections[i];
                    if (connection == null)
                        continue;

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(connection.IngressWorldPosition, 0.08f);
                    if (FloodIngressSampler.TrySample(connection, volume, out var sample))
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawRay(
                            sample.WorldPosition,
                            sample.DirectionWorld * 0.75f);
                    }
                }
            }

            if (state == null)
                return;

            var patches = state.Patches;
            for (var i = 0; i < patches.Length; i++)
            {
                if (!patches[i].IsActive)
                    continue;

                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.85f);
                Gizmos.DrawWireSphere(patches[i].CenterWorld, patches[i].CurrentRadius);
                Gizmos.color = new Color(0.9f, 0.9f, 0.2f, 0.5f);
                Gizmos.DrawWireSphere(patches[i].CenterWorld, patches[i].TargetRadius);
            }
        }

        private bool HasExplicitProviders()
        {
            if (connections != null)
            {
                for (var i = 0; i < connections.Length; i++)
                {
                    if (connections[i] != null)
                        return true;
                }
            }

            if (sources != null)
            {
                for (var i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != null)
                        return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            floorRaycastDistance = Mathf.Max(0.01f, floorRaycastDistance);
            connections ??= Array.Empty<FloodConnection>();
            sources ??= Array.Empty<FloodSource>();
            streamPresenters ??= Array.Empty<FloodIngressStreamPresenter>();
        }

        private struct DiscSlot
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public Vector3 ProjectedCenter;
            public bool HasFloorProjection;
        }
    }
}
