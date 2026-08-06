using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only base component that consumes and interpolates flood
    /// state without mutating the simulation.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class FloodSurfaceRenderer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Flood volume whose immutable state drives this renderer.")]
        private FloodVolume floodVolume;

        [SerializeField]
        [Tooltip("Seconds used to interpolate toward each published state. Set to zero to apply states immediately.")]
        [Min(0f)]
        private float interpolationDuration = 0.1f;

        private FloodState interpolationStart;
        private FloodState targetState;
        private FloodState displayedState;
        private float interpolationElapsed;
        private bool hasDisplayedState;
        private bool isSubscribed;

        /// <summary>
        /// Gets or sets the flood volume that drives this renderer.
        /// </summary>
        public FloodVolume SourceVolume
        {
            get => floodVolume;
            set => SetSourceVolume(value);
        }

        /// <summary>
        /// Gets or sets the interpolation duration in seconds.
        /// </summary>
        public float InterpolationDuration
        {
            get => interpolationDuration;
            set => interpolationDuration = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets the state most recently applied to the concrete renderer.
        /// </summary>
        public FloodState DisplayedState => displayedState;

        protected virtual void Reset()
        {
            floodVolume = GetComponent<FloodVolume>();
        }

        protected virtual void Awake()
        {
            if (floodVolume == null)
                floodVolume = GetComponent<FloodVolume>();
        }

        protected virtual void OnEnable()
        {
            Subscribe();

            if (floodVolume != null && floodVolume.IsRegionMember)
            {
                Debug.LogWarning(
                    $"FloodSurfaceRenderer on '{name}' targets FloodVolume "
                    + $"'{floodVolume.name}', which is a FloodRegion member. "
                    + "Disable this renderer and use FloodRegionSurfaceRenderer "
                    + "on the region for continuous presentation.",
                    this);
            }
        }

        protected virtual void Start()
        {
            SnapToCurrentState();
        }

        protected virtual void Update()
        {
            UpdateInterpolation(Time.deltaTime);
        }

        protected virtual void OnDisable()
        {
            Unsubscribe();
        }

        protected virtual void OnValidate()
        {
            interpolationDuration = Mathf.Max(0f, interpolationDuration);

            if (floodVolume == null)
                floodVolume = GetComponent<FloodVolume>();
        }

        /// <summary>
        /// Immediately applies the source volume's current state.
        /// </summary>
        public void SnapToCurrentState()
        {
            if (floodVolume == null)
                return;

            SetDisplayedState(floodVolume.CurrentState);
            interpolationStart = displayedState;
            targetState = displayedState;
            interpolationElapsed = interpolationDuration;
        }

        /// <summary>
        /// Applies an immutable presentation state to a concrete renderer.
        /// </summary>
        /// <param name="state">State to present.</param>
        protected abstract void ApplyState(FloodState state);

        private void SetSourceVolume(FloodVolume value)
        {
            if (floodVolume == value)
                return;

            Unsubscribe();
            floodVolume = value;

            if (isActiveAndEnabled)
                Subscribe();

            hasDisplayedState = false;
            SnapToCurrentState();
        }

        private void Subscribe()
        {
            if (isSubscribed || floodVolume == null)
                return;

            floodVolume.StateChanged += HandleStateChanged;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || floodVolume == null)
                return;

            floodVolume.StateChanged -= HandleStateChanged;
            isSubscribed = false;
        }

        private void HandleStateChanged(FloodState state)
        {
            // Occupancy free-surface rebuilds are expensive; keep them on tick
            // publishes rather than every interpolated frame.
            if (floodVolume != null
                && floodVolume.Geometry is BakedFloodGeometry)
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
                Interpolate(
                    interpolationStart,
                    targetState,
                    interpolation));
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
                interpolation);

            if (normal.sqrMagnitude <= Mathf.Epsilon)
                normal = target.normal;

            normal.Normalize();

            var startPoint = -start.normal * start.distance;
            var targetPoint = -target.normal * target.distance;

            return new Plane(
                normal,
                Vector3.Lerp(
                    startPoint,
                    targetPoint,
                    interpolation));
        }

        private static double Lerp(
            double start,
            double target,
            float interpolation)
        {
            return start + ((target - start) * interpolation);
        }
    }
}
