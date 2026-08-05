#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System;
using UnityEngine;

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// Updates authored hull-breach sample ocean presentation and Game-view
    /// readout. Compartment water presentation is owned by
    /// <see cref="FloodCubeSurfaceRenderer"/>. Optional bilge
    /// <see cref="FloodSink"/> demonstrates manager-mediated pumping.
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

        [SerializeField]
        [Tooltip("Optional bilge FloodSink that removes water from the compartment.")]
        private FloodSink bilgePump;

        [Header("Presentation")]

        [SerializeField]
        [Tooltip("Optional ocean surface visual aligned to the external waterline.")]
        private Transform oceanSurfaceVisual;

        [SerializeField]
        [Tooltip("Absolute pressure-head difference in meters at or below which the readout reports Equalized.")]
        [Min(0f)]
        private float equalizedHeadTolerance = 0.02f;

        private void Update()
        {
            if (WasPressedKeyB() && bilgePump != null)
                bilgePump.IsActive = !bilgePump.IsActive;
        }

        private void LateUpdate()
        {
            RefreshOceanVisual();
        }

        private void OnGUI()
        {
            if (ocean == null || compartment == null || breach == null)
                return;

            var oceanElevation = GetElevationAlongGravity(ocean.SurfacePlane);
            var headDifference = breach.PressureHeadDifference;
            var absoluteHeadDifference = Math.Abs(headDifference);
            var status = !breach.IsOpen
                ? "Closed"
                : absoluteHeadDifference <= equalizedHeadTolerance
                    ? "Equalized"
                    : headDifference > 0d
                        ? "Inflow"
                        : "Outflow";

            const float boxWidth = 520f;
            var boxHeight = bilgePump != null ? 210f : 160f;
            var boxX = Mathf.Max(16f, (Screen.width - boxWidth) * 0.5f);

            GUI.Box(new Rect(boxX, 16f, boxWidth, boxHeight), "Hull Breach");
            GUI.Label(
                new Rect(boxX + 14f, 44f, boxWidth - 28f, 20f),
                $"Ocean waterline elevation: {oceanElevation:F3} m");
            GUI.Label(
                new Rect(boxX + 14f, 66f, boxWidth - 28f, 20f),
                $"Compartment: {compartment.CurrentVolume:F3} m³  "
                + $"(equiv. height {compartment.CurrentHeight:F3} m)");
            GUI.Label(
                new Rect(boxX + 14f, 88f, boxWidth - 28f, 20f),
                $"Requested / applied: {breach.RequestedFlowRate:F3} / "
                + $"{breach.CurrentFlowRate:F3} m³/s");
            GUI.Label(
                new Rect(boxX + 14f, 110f, boxWidth - 28f, 20f),
                $"{status}; |pressure head A-B|: {absoluteHeadDifference:F3} m");

            var y = 132f;
            if (bilgePump != null)
            {
                GUI.Label(
                    new Rect(boxX + 14f, y, boxWidth - 28f, 20f),
                    $"Bilge pump: {(bilgePump.IsActive ? "ON" : "OFF")}  "
                    + $"Configured: {bilgePump.FlowRate:F2} m³/s  "
                    + $"Actual: {bilgePump.CurrentFlowRate:F2} m³/s");
                y += 22f;
                GUI.Label(
                    new Rect(boxX + 14f, y, boxWidth - 28f, 20f),
                    "B toggle bilge pump. Tune ocean Y, breach, or Is Open.");
            }
            else
            {
                GUI.Label(
                    new Rect(boxX + 14f, y, boxWidth - 28f, 20f),
                    "Tune ocean Y, breach, Is Open, or rotate the compartment.");
            }
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
            equalizedHeadTolerance = SanitizeNonNegative(
                equalizedHeadTolerance,
                0.02f);
        }

        private float GetElevationAlongGravity(Plane surfacePlane)
        {
            var gravity = Physics.gravity;
            var up = gravity.sqrMagnitude
                >= FloodGeometryTolerances.MinimumGravityMagnitude
                    * FloodGeometryTolerances.MinimumGravityMagnitude
                ? -gravity.normalized
                : Vector3.up;

            // A point on the plane projected onto the gravity-up axis.
            var pointOnPlane = surfacePlane.normal * -surfacePlane.distance;
            return Vector3.Dot(pointOnPlane, up);
        }

        private static bool WasPressedKeyB() => WasPressedKey(KeyCode.B);

        private static bool WasPressedKey(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return Input.GetKeyDown(key);

            return key == KeyCode.B
                ? keyboard.bKey.wasPressedThisFrame
                : Input.GetKeyDown(key);
#else
            return Input.GetKeyDown(key);
#endif
        }

        private static float SanitizeNonNegative(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Max(0f, value);
        }
    }
}
