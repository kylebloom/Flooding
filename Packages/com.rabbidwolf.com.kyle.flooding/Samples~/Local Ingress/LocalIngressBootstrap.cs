#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// Sample-only controls and HUD for comparing local ingress presentation
    /// against instant bulk-surface equilibrium visuals.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Samples/Local Ingress Bootstrap")]
    public sealed class LocalIngressBootstrap : MonoBehaviour
    {
        [Header("Simulation")]

        [SerializeField]
        [Tooltip("Finite compartment receiving ingress.")]
        private FloodVolume compartment;

        [SerializeField]
        [Tooltip("Primary hull-breach connection driving ingress.")]
        private FloodConnection primaryBreach;

        [SerializeField]
        [Tooltip("Optional second ingress connection (for example a doorway).")]
        private FloodConnection secondaryIngress;

        [SerializeField]
        [Tooltip("Optional configured source used for a tiny-leak comparison.")]
        private FloodSource optionalLeakSource;

        [SerializeField]
        [Tooltip("External ocean boundary used by the primary breach.")]
        private ExternalFluidBoundary ocean;

        [Header("Presentation")]

        [SerializeField]
        [Tooltip("Local ingress presenter for the compartment.")]
        private FloodLocalIngressPresenter ingressPresenter;

        [SerializeField]
        [Tooltip("Bulk free-surface renderer for the compartment.")]
        private FloodSurfaceRenderer surfaceRenderer;

        [Header("Controls")]

        [SerializeField]
        [Tooltip("Flow rate applied to the optional leak source when active.")]
        [Min(0f)]
        private float leakFlowRate = 0.05f;

        [SerializeField]
        [Tooltip("Medium comparison flow scale tip shown in the HUD.")]
        private string controlsHint =
            "I local on/off | 1 tiny | 2 medium | 3 breach | O open | R reset | P secondary";

        private void Update()
        {
            if (WasPressedKeyI() && ingressPresenter != null)
            {
                ingressPresenter.PresentationEnabled =
                    !ingressPresenter.PresentationEnabled;
            }

            if (WasPressedKeyO() && primaryBreach != null)
                primaryBreach.IsOpen = !primaryBreach.IsOpen;

            if (WasPressedKeyP() && secondaryIngress != null)
                secondaryIngress.IsOpen = !secondaryIngress.IsOpen;

            if (WasPressedKeyR() && compartment != null)
                compartment.RemoveWater(compartment.CurrentVolume);

            if (WasPressedKey1())
                ApplyTinyLeakPreset();

            if (WasPressedKey2())
                ApplyMediumPreset();

            if (WasPressedKey3())
                ApplyMajorBreachPreset();
        }

        private void OnGUI()
        {
            if (compartment == null)
                return;

            const float width = 560f;
            var boxX = Mathf.Max(16f, (Screen.width - width) * 0.5f);
            GUI.Box(new Rect(boxX, 12f, width, 210f), "Local Ingress");

            var y = 40f;
            GUI.Label(
                new Rect(boxX + 14f, y, width - 28f, 20f),
                $"Authoritative Volume: {compartment.CurrentVolume:F3} m³");
            y += 20f;
            GUI.Label(
                new Rect(boxX + 14f, y, width - 28f, 20f),
                $"Authoritative Fill: {compartment.FillPercentage * 100f:F1} %");
            y += 20f;

            var inflow = ingressPresenter != null
                ? ingressPresenter.CurrentInflowRateCubicMetersPerSecond
                : primaryBreach != null
                    ? (float)System.Math.Abs(primaryBreach.CurrentFlowRate)
                    : 0f;
            GUI.Label(
                new Rect(boxX + 14f, y, width - 28f, 20f),
                $"Current Inflow Rate: {inflow:F3} m³/s");
            y += 20f;

            if (ingressPresenter != null)
            {
                GUI.Label(
                    new Rect(boxX + 14f, y, width - 28f, 20f),
                    $"Local Ingress: {(ingressPresenter.PresentationEnabled ? "ON" : "OFF")}  "
                    + $"Active Patches: {ingressPresenter.ActivePatchCount}");
                y += 20f;
                GUI.Label(
                    new Rect(boxX + 14f, y, width - 28f, 20f),
                    $"Oldest Patch Age: {ingressPresenter.OldestPatchAgeSeconds:F2} s  "
                    + $"Handoff: {ingressPresenter.AverageHandoffFraction * 100f:F0} %");
                y += 20f;
            }

            if (primaryBreach != null)
            {
                GUI.Label(
                    new Rect(boxX + 14f, y, width - 28f, 20f),
                    $"Primary Breach Open: {primaryBreach.IsOpen}  "
                    + $"Applied: {primaryBreach.CurrentFlowRate:F3} m³/s");
                y += 20f;
            }

            GUI.Label(
                new Rect(boxX + 14f, y, width - 28f, 20f),
                controlsHint);
        }

        private void ApplyTinyLeakPreset()
        {
            if (primaryBreach != null)
                primaryBreach.IsOpen = false;

            if (secondaryIngress != null)
                secondaryIngress.IsOpen = false;

            if (optionalLeakSource != null)
            {
                optionalLeakSource.IsActive = true;
                optionalLeakSource.FlowRate = leakFlowRate;
            }
        }

        private void ApplyMediumPreset()
        {
            if (optionalLeakSource != null)
                optionalLeakSource.IsActive = false;

            if (secondaryIngress != null)
                secondaryIngress.IsOpen = false;

            if (primaryBreach != null)
            {
                primaryBreach.IsOpen = true;
                primaryBreach.OpeningWidth = 0.35f;
                primaryBreach.OpeningHeight = 0.35f;
            }
        }

        private void ApplyMajorBreachPreset()
        {
            if (optionalLeakSource != null)
                optionalLeakSource.IsActive = false;

            if (primaryBreach != null)
            {
                primaryBreach.IsOpen = true;
                primaryBreach.OpeningWidth = 1.4f;
                primaryBreach.OpeningHeight = 1.2f;
            }

            if (secondaryIngress != null)
                secondaryIngress.IsOpen = true;
        }

        private static bool WasPressedKeyI() => WasPressedKey(KeyCode.I);

        private static bool WasPressedKeyO() => WasPressedKey(KeyCode.O);

        private static bool WasPressedKeyP() => WasPressedKey(KeyCode.P);

        private static bool WasPressedKeyR() => WasPressedKey(KeyCode.R);

        private static bool WasPressedKey1() => WasPressedKey(KeyCode.Alpha1);

        private static bool WasPressedKey2() => WasPressedKey(KeyCode.Alpha2);

        private static bool WasPressedKey3() => WasPressedKey(KeyCode.Alpha3);

        private static bool WasPressedKey(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return Input.GetKeyDown(key);

            return key switch
            {
                KeyCode.I => keyboard.iKey.wasPressedThisFrame,
                KeyCode.O => keyboard.oKey.wasPressedThisFrame,
                KeyCode.P => keyboard.pKey.wasPressedThisFrame,
                KeyCode.R => keyboard.rKey.wasPressedThisFrame,
                KeyCode.Alpha1 => keyboard.digit1Key.wasPressedThisFrame,
                KeyCode.Alpha2 => keyboard.digit2Key.wasPressedThisFrame,
                KeyCode.Alpha3 => keyboard.digit3Key.wasPressedThisFrame,
                _ => Input.GetKeyDown(key),
            };
#else
            return Input.GetKeyDown(key);
#endif
        }

        private void OnValidate()
        {
            leakFlowRate = Mathf.Max(0f, leakFlowRate);
        }
    }
}
