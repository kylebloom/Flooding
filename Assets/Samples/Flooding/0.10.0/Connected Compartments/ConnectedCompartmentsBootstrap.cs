using UnityEngine;

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// Updates the authored sample visuals and centered Game-view readout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConnectedCompartmentsBootstrap : MonoBehaviour
    {
        [Header("Simulation Sources")]

        [SerializeField]
        [Tooltip("Authored high-water FloodVolume shown as compartment A.")]
        private FloodVolume compartmentA;

        [SerializeField]
        [Tooltip("Authored low-water FloodVolume shown as compartment B.")]
        private FloodVolume compartmentB;

        [SerializeField]
        [Tooltip("Authored FloodConnection whose applied rate drives the flow arrow and readout.")]
        private FloodConnection connection;

        [Header("Presentation")]

        [SerializeField]
        [Tooltip("Persistent cube Transform scaled to show compartment A water.")]
        private Transform compartmentAWaterVisual;

        [SerializeField]
        [Tooltip("Persistent cube Transform scaled to show compartment B water.")]
        private Transform compartmentBWaterVisual;

        [SerializeField]
        [Tooltip("Persistent arrow root rotated and pulsed to show applied flow direction and magnitude.")]
        private Transform flowIndicator;

        [SerializeField]
        [Tooltip("Inset in meters removed from each side of both water visuals to keep compartment walls visible.")]
        [Min(0f)]
        private float waterVisualInset = 0.08f;

        [SerializeField]
        [Tooltip("Arrow scale increase per cubic meter per second of absolute applied flow.")]
        [Min(0f)]
        private float flowPulsePerCubicMeterPerSecond = 0.08f;

        [SerializeField]
        [Tooltip("Height difference in meters at or below which the centered readout reports Equalized.")]
        [Min(0f)]
        private float equalizedHeightTolerance = 0.01f;

        private Vector3 flowIndicatorBaseScale = Vector3.one;

        private void Awake()
        {
            if (flowIndicator != null)
                flowIndicatorBaseScale = flowIndicator.localScale;
        }

        private void LateUpdate()
        {
            RefreshWater(compartmentA, compartmentAWaterVisual);
            RefreshWater(compartmentB, compartmentBWaterVisual);
            RefreshFlowIndicator();
        }

        private void OnGUI()
        {
            if (compartmentA == null || compartmentB == null || connection == null)
                return;

            var heightDifference = Mathf.Abs(
                compartmentA.CurrentHeight - compartmentB.CurrentHeight);
            var status = heightDifference <= equalizedHeightTolerance
                ? "Equalized"
                : "Equalizing";
            const float boxWidth = 430f;
            var boxX = Mathf.Max(16f, (Screen.width - boxWidth) * 0.5f);

            GUI.Box(
                new Rect(boxX, 16f, boxWidth, 138f),
                "Connected Compartments");
            GUI.Label(
                new Rect(boxX + 14f, 44f, boxWidth - 28f, 20f),
                $"A: {compartmentA.CurrentVolume:F3} m³  "
                + $"({compartmentA.CurrentHeight:F3} m high)");
            GUI.Label(
                new Rect(boxX + 14f, 66f, boxWidth - 28f, 20f),
                $"B: {compartmentB.CurrentVolume:F3} m³  "
                + $"({compartmentB.CurrentHeight:F3} m high)");
            GUI.Label(
                new Rect(boxX + 14f, 88f, boxWidth - 28f, 20f),
                $"Requested / applied: {connection.RequestedFlowRate:F3} / "
                + $"{connection.CurrentFlowRate:F3} m³/s");
            GUI.Label(
                new Rect(boxX + 14f, 110f, boxWidth - 28f, 20f),
                $"{status}; height difference: {heightDifference:F3} m");
        }

        private void RefreshWater(FloodVolume volume, Transform waterVisual)
        {
            if (volume == null || waterVisual == null)
                return;

            var height = Mathf.Max(0.001f, volume.CurrentHeight);
            var width = Mathf.Max(0.001f, volume.Width - (waterVisualInset * 2f));
            var length = Mathf.Max(0.001f, volume.Length - (waterVisualInset * 2f));
            waterVisual.localPosition = new Vector3(0f, height * 0.5f, 0f);
            waterVisual.localScale = new Vector3(width, height, length);
        }

        private void RefreshFlowIndicator()
        {
            if (connection == null || flowIndicator == null)
                return;

            var rate = connection.CurrentFlowRate;
            flowIndicator.localRotation = rate < -0.0001d
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;

            var pulse =
                1f
                + ((float)System.Math.Abs(rate)
                    * flowPulsePerCubicMeterPerSecond);
            flowIndicator.localScale = flowIndicatorBaseScale * pulse;
        }

        private void OnValidate()
        {
            waterVisualInset = SanitizeNonNegative(waterVisualInset, 0.08f);
            flowPulsePerCubicMeterPerSecond = SanitizeNonNegative(
                flowPulsePerCubicMeterPerSecond,
                0.08f);
            equalizedHeightTolerance = SanitizeNonNegative(
                equalizedHeightTolerance,
                0.01f);
        }

        private static float SanitizeNonNegative(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Max(0f, value);
        }
    }
}
