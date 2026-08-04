using UnityEngine;

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// SAMPLE ONLY.
    /// Provides artificial restoring forces so changes to Rigidbody center of
    /// mass produce an easily visible roll/pitch response.
    /// This is NOT a buoyancy, hydrodynamics, or vessel-stability simulation.
    /// Do not copy this into production ship physics.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Samples/Sample Vessel Support")]
    public sealed class SampleVesselSupport : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("SAMPLE ONLY. World-space support plane height in meters. Not a waterline or buoyancy draft.")]
        private float supportHeight = 1f;

        [SerializeField]
        [Tooltip("SAMPLE ONLY. Upward spring stiffness per support point in newtons per meter.")]
        [Min(0f)]
        private float springStiffness = 14000f;

        [SerializeField]
        [Tooltip("SAMPLE ONLY. Vertical damping per support point in newton-seconds per meter.")]
        [Min(0f)]
        private float damping = 2800f;

        [SerializeField]
        [Tooltip("SAMPLE ONLY. Half-width of the four support points in local meters.")]
        [Min(0.01f)]
        private float halfWidth = 2f;

        [SerializeField]
        [Tooltip("SAMPLE ONLY. Half-length of the four support points in local meters.")]
        [Min(0.01f)]
        private float halfLength = 3f;

        [SerializeField]
        [Tooltip("SAMPLE ONLY. Local Y position of each support point in meters.")]
        private float supportPointY = -0.55f;

        private Rigidbody targetRigidbody;

        private void Awake()
        {
            targetRigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (targetRigidbody == null)
                return;

            ApplySupport(new Vector3(-halfWidth, supportPointY, -halfLength));
            ApplySupport(new Vector3(-halfWidth, supportPointY, halfLength));
            ApplySupport(new Vector3(halfWidth, supportPointY, -halfLength));
            ApplySupport(new Vector3(halfWidth, supportPointY, halfLength));
        }

        private void ApplySupport(Vector3 localPoint)
        {
            var worldPoint = transform.TransformPoint(localPoint);
            var compression = supportHeight - worldPoint.y;
            if (compression <= 0f)
                return;

            var verticalSpeed =
                targetRigidbody.GetPointVelocity(worldPoint).y;
            var force = Mathf.Max(
                0f,
                (springStiffness * compression) - (damping * verticalSpeed));
            targetRigidbody.AddForceAtPosition(
                Vector3.up * force,
                worldPoint,
                ForceMode.Force);
        }
    }
}
