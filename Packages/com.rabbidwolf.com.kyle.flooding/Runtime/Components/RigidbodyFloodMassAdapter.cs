using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Applies dry-body and aggregate flood mass to one Rigidbody.
    /// </summary>
    [AddComponentMenu("Flooding/Rigidbody Flood Mass Adapter")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RigidbodyFloodMassAdapter : MonoBehaviour
    {
        private const float MinimumDryMass = 0.0001f;

        [SerializeField]
        [Tooltip("Flood mass aggregator whose child compartments contribute water mass.")]
        private FloodMassAggregator floodMass;

        [SerializeField]
        [Tooltip("Vessel mass without flood water, in kilograms. This remains the adapter's authoritative baseline.")]
        [Min(MinimumDryMass)]
        private float dryMass = 1000f;

        [SerializeField]
        [Tooltip("Vessel center of mass without flood water, in Rigidbody-local meters.")]
        private Vector3 dryCenterOfMassLocal;

        private Rigidbody targetRigidbody;

        /// <summary>
        /// Gets or sets the aggregate flood mass source.
        /// </summary>
        public FloodMassAggregator FloodMass
        {
            get => floodMass;
            set => floodMass = value;
        }

        /// <summary>
        /// Gets the configured dry-body mass in kilograms.
        /// </summary>
        public float DryMass => dryMass;

        /// <summary>
        /// Gets the configured dry-body center of mass in Rigidbody-local meters.
        /// </summary>
        public Vector3 DryCenterOfMassLocal => dryCenterOfMassLocal;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ApplyMassContribution();
        }

        private void FixedUpdate()
        {
            ApplyMassContribution();
        }

        private void OnDisable()
        {
            RestoreDryBody();
        }

        private void OnValidate()
        {
            dryMass = Mathf.Max(MinimumDryMass, dryMass);

            if (floodMass == null)
                floodMass = GetComponent<FloodMassAggregator>();
        }

        /// <summary>
        /// Configures the authoritative dry-body baseline.
        /// </summary>
        /// <param name="massKilograms">Positive mass in kilograms.</param>
        /// <param name="centerOfMassLocal">
        /// Dry center of mass in Rigidbody-local meters.
        /// </param>
        public void ConfigureDryBody(
            float massKilograms,
            Vector3 centerOfMassLocal)
        {
            if (float.IsNaN(massKilograms)
                || float.IsInfinity(massKilograms)
                || massKilograms < MinimumDryMass)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(massKilograms),
                    massKilograms,
                    "Dry mass must be finite and positive.");
            }

            dryMass = massKilograms;
            dryCenterOfMassLocal = centerOfMassLocal;
            ApplyMassContribution();
        }

        /// <summary>
        /// Applies the latest dry-plus-flood composite to the Rigidbody.
        /// </summary>
        public void ApplyMassContribution()
        {
            ResolveReferences();

            if (targetRigidbody == null)
                return;

            var floodContribution = floodMass == null
                ? FloodMassContribution.Empty
                : floodMass.CurrentContribution;
            var totalMass = dryMass + floodContribution.Mass;
            var dryCenterWorld =
                targetRigidbody.transform.TransformPoint(dryCenterOfMassLocal);
            var centerWorld = floodContribution.Mass > 0d
                ? (
                    (dryMass * (Vector3d)dryCenterWorld)
                    + (floodContribution.Mass
                        * (Vector3d)floodContribution.CenterOfMassWorld))
                    / totalMass
                : (Vector3d)dryCenterWorld;

            targetRigidbody.mass = (float)System.Math.Min(
                totalMass,
                float.MaxValue);
            targetRigidbody.centerOfMass =
                targetRigidbody.transform.InverseTransformPoint(
                    (Vector3)centerWorld);
        }

        /// <summary>
        /// Restores the configured dry-body mass and local center of mass.
        /// </summary>
        public void RestoreDryBody()
        {
            ResolveReferences();

            if (targetRigidbody == null)
                return;

            targetRigidbody.mass = dryMass;
            targetRigidbody.centerOfMass = dryCenterOfMassLocal;
        }

        private void ResolveReferences()
        {
            if (targetRigidbody == null)
                targetRigidbody = GetComponent<Rigidbody>();

            if (floodMass == null)
                floodMass = GetComponent<FloodMassAggregator>();
        }

        private readonly struct Vector3d
        {
            private Vector3d(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            private double X { get; }

            private double Y { get; }

            private double Z { get; }

            public static explicit operator Vector3d(Vector3 value)
            {
                return new Vector3d(value.x, value.y, value.z);
            }

            public static explicit operator Vector3(Vector3d value)
            {
                return new Vector3(
                    (float)value.X,
                    (float)value.Y,
                    (float)value.Z);
            }

            public static Vector3d operator *(
                double scale,
                Vector3d value)
            {
                return new Vector3d(
                    scale * value.X,
                    scale * value.Y,
                    scale * value.Z);
            }

            public static Vector3d operator +(
                Vector3d left,
                Vector3d right)
            {
                return new Vector3d(
                    left.X + right.X,
                    left.Y + right.Y,
                    left.Z + right.Z);
            }

            public static Vector3d operator /(
                Vector3d value,
                double divisor)
            {
                return new Vector3d(
                    value.X / divisor,
                    value.Y / divisor,
                    value.Z / divisor);
            }
        }
    }
}
