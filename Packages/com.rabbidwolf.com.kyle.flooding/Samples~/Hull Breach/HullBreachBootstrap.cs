using UnityEngine;

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// Updates authored hull-breach sample visuals and Game-view readout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HullBreachBootstrap : MonoBehaviour
    {
        [Header("Simulation Sources")]

        [SerializeField]
        [Tooltip("Authored External Fluid Body representing the ocean waterline.")]
        private ExternalFluidBoundary ocean;

        [SerializeField]
        [Tooltip("Authored FloodVolume representing the breached compartment.")]
        private FloodVolume compartment;

        [SerializeField]
        [Tooltip("Authored FloodConnection opening between ocean and compartment.")]
        private FloodConnection breach;

        [Header("Presentation")]

        [SerializeField]
        [Tooltip("Persistent cube Transform scaled to show compartment water fill.")]
        private Transform compartmentWaterVisual;

        [SerializeField]
        [Tooltip("Optional ocean surface visual aligned to the external waterline.")]
        private Transform oceanSurfaceVisual;

        [SerializeField]
        [Tooltip("Inset in meters removed from each side of the compartment water visual.")]
        [Min(0f)]
        private float waterVisualInset = 0.08f;

        [SerializeField]
        [Tooltip("Height difference in meters at or below which the readout reports Equalized.")]
        [Min(0f)]
        private float equalizedHeightTolerance = 0.02f;

        private void LateUpdate()
        {
            RefreshCompartmentWater();
            RefreshOceanVisual();
        }

        private void OnGUI()
        {
            if (ocean == null || compartment == null || breach == null)
                return;

            var oceanHeight = ocean.transform.position.y;
            var heightDifference = Mathf.Abs(compartment.CurrentHeight - oceanHeight);
            var status = !breach.IsOpen
                ? "Closed"
                : heightDifference <= equalizedHeightTolerance
                    ? "Equalized"
                    : compartment.CurrentHeight < oceanHeight
                        ? "Inflow"
                        : "Outflow";

            const float boxWidth = 460f;
            var boxX = Mathf.Max(16f, (Screen.width - boxWidth) * 0.5f);

            GUI.Box(new Rect(boxX, 16f, boxWidth, 160f), "Hull Breach");
            GUI.Label(
                new Rect(boxX + 14f, 44f, boxWidth - 28f, 20f),
                $"Ocean waterline: {oceanHeight:F3} m");
            GUI.Label(
                new Rect(boxX + 14f, 66f, boxWidth - 28f, 20f),
                $"Compartment: {compartment.CurrentVolume:F3} m³  "
                + $"({compartment.CurrentHeight:F3} m high)");
            GUI.Label(
                new Rect(boxX + 14f, 88f, boxWidth - 28f, 20f),
                $"Requested / applied: {breach.RequestedFlowRate:F3} / "
                + $"{breach.CurrentFlowRate:F3} m³/s");
            GUI.Label(
                new Rect(boxX + 14f, 110f, boxWidth - 28f, 20f),
                $"{status}; |interior - ocean|: {heightDifference:F3} m");
            GUI.Label(
                new Rect(boxX + 14f, 132f, boxWidth - 28f, 20f),
                "Tune ocean Y, breach Y, Is Open, or rotate the compartment.");
        }

        private void RefreshCompartmentWater()
        {
            if (compartment == null || compartmentWaterVisual == null)
                return;

            var height = Mathf.Max(0.001f, compartment.CurrentHeight);
            var width = Mathf.Max(
                0.001f,
                compartment.Width - (waterVisualInset * 2f));
            var length = Mathf.Max(
                0.001f,
                compartment.Length - (waterVisualInset * 2f));
            compartmentWaterVisual.localPosition = new Vector3(0f, height * 0.5f, 0f);
            compartmentWaterVisual.localScale = new Vector3(width, height, length);
        }

        private void RefreshOceanVisual()
        {
            if (ocean == null || oceanSurfaceVisual == null)
                return;

            var source = ocean.SurfaceTransform != null
                ? ocean.SurfaceTransform
                : ocean.transform;
            oceanSurfaceVisual.position = source.position;
            oceanSurfaceVisual.rotation = Quaternion.LookRotation(
                Vector3.Cross(source.right, source.up),
                source.up);
        }

        private void OnValidate()
        {
            waterVisualInset = SanitizeNonNegative(waterVisualInset, 0.08f);
            equalizedHeightTolerance = SanitizeNonNegative(
                equalizedHeightTolerance,
                0.02f);
        }

        private static float SanitizeNonNegative(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Max(0f, value);
        }
    }
}
