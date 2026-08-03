using UnityEngine;

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// Optionally animates the baked-geometry sample's fill and roll.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BakedGeometrySampleBootstrap : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Serialized baked-data Flood Volume demonstrated by this sample.")]
        private FloodVolume floodVolume;

        [SerializeField]
        [Tooltip("When enabled, cycles the water amount between the configured fill fractions.")]
        private bool animateFill = true;

        [SerializeField]
        [Tooltip("When enabled, gently rolls the compartment around its local Z axis.")]
        private bool animateRoll = true;

        [SerializeField]
        [Tooltip("Lowest target water amount as a fraction of baked capacity.")]
        [Range(0f, 1f)]
        private float minimumFillFraction = 0.28f;

        [SerializeField]
        [Tooltip("Highest target water amount as a fraction of baked capacity.")]
        [Range(0f, 1f)]
        private float maximumFillFraction = 0.72f;

        [SerializeField]
        [Tooltip("Water added to or removed from the sample each second, in cubic meters per second.")]
        [Min(0.01f)]
        private float fillRate = 1.5f;

        [SerializeField]
        [Tooltip("Maximum compartment roll from level in degrees.")]
        [Range(0f, 30f)]
        private float rollDegrees = 10f;

        [SerializeField]
        [Tooltip("Seconds required for one complete compartment roll cycle.")]
        [Min(0.1f)]
        private float rollPeriod = 7f;

        private bool filling = true;

        private void Update()
        {
            if (floodVolume == null)
                return;

            if (animateFill)
                AnimateFill();

            if (animateRoll)
                AnimateRoll();
        }

        private void OnValidate()
        {
            minimumFillFraction = Mathf.Clamp01(minimumFillFraction);
            maximumFillFraction = Mathf.Clamp(
                maximumFillFraction,
                minimumFillFraction,
                1f);
            fillRate = Mathf.Max(0.01f, fillRate);
            rollPeriod = Mathf.Max(0.1f, rollPeriod);
        }

        private void AnimateFill()
        {
            var capacity = floodVolume.MaximumVolume;
            var minimum = capacity * minimumFillFraction;
            var maximum = capacity * maximumFillFraction;

            if (filling && floodVolume.CurrentVolume >= maximum)
                filling = false;
            else if (!filling && floodVolume.CurrentVolume <= minimum)
                filling = true;

            if (filling)
                floodVolume.AddWaterOverTime(fillRate, Time.deltaTime);
            else
                floodVolume.RemoveWaterOverTime(fillRate, Time.deltaTime);
        }

        private void AnimateRoll()
        {
            var phase = Time.time * Mathf.PI * 2f / rollPeriod;
            floodVolume.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Sin(phase) * rollDegrees);
        }
    }
}
