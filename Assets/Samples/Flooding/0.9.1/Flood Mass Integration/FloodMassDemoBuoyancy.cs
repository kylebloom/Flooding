using UnityEngine;

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// Sample-only spring support that makes flood COM shifts visible.
    /// This is not a production buoyancy model.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloodMassDemoBuoyancy : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("World-space support height in meters.")]
        private float supportHeight = 1f;

        [SerializeField]
        [Tooltip("Upward spring stiffness per support point in newtons per meter.")]
        [Min(0f)]
        private float springStiffness = 12000f;

        [SerializeField]
        [Tooltip("Vertical damping per support point in newton-seconds per meter.")]
        [Min(0f)]
        private float damping = 2500f;

        [SerializeField]
        [Tooltip("Half-width of the four support points in local meters.")]
        [Min(0.01f)]
        private float halfWidth = 2f;

        [SerializeField]
        [Tooltip("Half-length of the four support points in local meters.")]
        [Min(0.01f)]
        private float halfLength = 3f;

        [SerializeField]
        [Tooltip("Local Y position of each support point in meters.")]
        private float supportPointY = -0.5f;

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
