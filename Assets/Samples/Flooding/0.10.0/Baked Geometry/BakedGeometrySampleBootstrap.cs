using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// SAMPLE ONLY controller for the Baked Geometry hull-section sample.
    /// Optionally animates fill and roll, toggles baked-cell presentation, and
    /// draws a Game-view HUD. Water presentation is owned by
    /// <see cref="FloodBakedSurfaceRenderer"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Samples/Baked Geometry Sample Bootstrap")]
    public sealed class BakedGeometrySampleBootstrap : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Serialized baked-data Flood Volume demonstrated by this sample.")]
        private FloodVolume floodVolume;

        [SerializeField]
        [Tooltip("Optional Flood Volume Data used for bake resolution and retained-cell HUD fields. When unassigned, values come from the Flood Volume.")]
        private FloodVolumeData bakedData;

        [SerializeField]
        [Tooltip("Presentation root for Editor-built retained-cell cubes. Toggled with B in Play Mode.")]
        private GameObject bakedCellsPresentation;

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
        private bool paused;
        private bool showBakedCells;

        private void Awake()
        {
            if (floodVolume != null && bakedData == null)
                bakedData = floodVolume.BakedVolumeData;

            showBakedCells = bakedCellsPresentation != null
                && bakedCellsPresentation.activeSelf;
            ApplyBakedCellsVisibility();
        }

        private void Update()
        {
            HandleKeyboard();

            if (floodVolume == null || paused)
                return;

            if (animateFill)
                AnimateFill();

            if (animateRoll)
                AnimateRoll();
        }

        private void OnGUI()
        {
            if (floodVolume == null)
                return;

            var data = bakedData != null
                ? bakedData
                : floodVolume.BakedVolumeData;
            var capacity = floodVolume.MaximumVolume;
            var current = floodVolume.CurrentVolume;
            var fillPercent = capacity > 0f
                ? current / capacity * 100f
                : 0f;
            var resolution = data != null
                ? data.SampleResolution
                : Vector3.zero;
            var sampleCount = data != null ? data.SampleCount : 0;
            var resolutionLabel = data != null
                ? $"{resolution.x:0.##} × {resolution.y:0.##} × {resolution.z:0.##} m"
                : "n/a";

            var hasBoundary = data != null && data.HasPresentationBoundary;
            var surfaceLabel = hasBoundary
                ? "Source mesh bake"
                : "Voxel cells (legacy)";
            var cellsLabel = showBakedCells
                ? "Baked cells shown"
                : "Baked cells hidden";

            const float boxWidth = 440f;
            var boxX = 16f;
            GUI.Box(new Rect(boxX, 16f, boxWidth, 268f), "Baked Geometry");
            GUI.Label(
                new Rect(boxX + 14f, 44f, boxWidth - 28f, 20f),
                $"Capacity:       {capacity,8:F2} m³");
            GUI.Label(
                new Rect(boxX + 14f, 64f, boxWidth - 28f, 20f),
                $"Current Volume: {current,8:F2} m³");
            GUI.Label(
                new Rect(boxX + 14f, 84f, boxWidth - 28f, 20f),
                $"Fill:           {fillPercent,7:F0}%");
            GUI.Label(
                new Rect(boxX + 14f, 112f, boxWidth - 28f, 20f),
                $"Bake resolution: {resolutionLabel}");
            GUI.Label(
                new Rect(boxX + 14f, 132f, boxWidth - 28f, 20f),
                $"Retained cells:  {sampleCount}");
            GUI.Label(
                new Rect(boxX + 14f, 156f, boxWidth - 28f, 20f),
                "Simulation geometry: Voxel occupancy");
            GUI.Label(
                new Rect(boxX + 14f, 176f, boxWidth - 28f, 20f),
                $"Surface boundary:     {surfaceLabel}");
            GUI.Label(
                new Rect(boxX + 14f, 196f, boxWidth - 28f, 20f),
                paused
                    ? $"Paused — {cellsLabel}"
                    : $"Running — {cellsLabel}");
            GUI.Label(
                new Rect(boxX + 14f, 220f, boxWidth - 28f, 40f),
                "[Space] Pause   [B] Show baked cells   [R] Toggle roll\n"
                + "Voxels answer quantity; boundary answers footprint shape.");
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

        private void HandleKeyboard()
        {
            if (WasPressed(KeyCode.Space
#if ENABLE_INPUT_SYSTEM
                , Key.Space
#endif
            ))
            {
                paused = !paused;
            }

            if (WasPressed(KeyCode.B
#if ENABLE_INPUT_SYSTEM
                , Key.B
#endif
            ))
            {
                showBakedCells = !showBakedCells;
                ApplyBakedCellsVisibility();
            }

            if (WasPressed(KeyCode.R
#if ENABLE_INPUT_SYSTEM
                , Key.R
#endif
            ))
            {
                animateRoll = !animateRoll;
                if (!animateRoll && floodVolume != null)
                    floodVolume.transform.localRotation = Quaternion.identity;
            }
        }

        private void ApplyBakedCellsVisibility()
        {
            if (bakedCellsPresentation != null)
                bakedCellsPresentation.SetActive(showBakedCells);
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

#if ENABLE_INPUT_SYSTEM
        private static bool WasPressed(KeyCode legacy, Key modern)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[modern].wasPressedThisFrame)
                return true;

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(legacy);
#else
            return false;
#endif
        }
#else
        private static bool WasPressed(KeyCode legacy)
        {
            return Input.GetKeyDown(legacy);
        }
#endif
    }
}
