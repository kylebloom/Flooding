#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Reflection;
using UnityEngine;

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// Sample-only first-person controls and HUD for the Region Stress demo.
    /// Simulation ownership stays with package components.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Samples/Region Stress Bootstrap")]
    public sealed class RegionStressBootstrap : MonoBehaviour
    {
        [Header("Simulation")]

        [SerializeField]
        [Tooltip("Shared simulation manager for tick metrics.")]
        private FloodSimulationManager simulationManager;

        [SerializeField]
        [Tooltip("Compartment A region (breach destination).")]
        private FloodRegion regionA;

        [SerializeField]
        [Tooltip("Corridor + stair multi-deck region.")]
        private FloodRegion regionCorridor;

        [SerializeField]
        [Tooltip("Compartment B region (pump destination).")]
        private FloodRegion regionB;

        [SerializeField]
        [Tooltip("Ocean → Region A breach connection.")]
        private FloodConnection breach;

        [SerializeField]
        [Tooltip("Region A → Corridor door connection.")]
        private FloodConnection door;

        [SerializeField]
        [Tooltip("Corridor → Region B hatch connection.")]
        private FloodConnection hatch;

        [SerializeField]
        [Tooltip("Bilge pump sink removing water from Region B.")]
        private FloodSink pump;

        [SerializeField]
        [Tooltip("Vessel root rotated when demonstrating tilt.")]
        private Transform vesselRoot;

        [Header("Player")]

        [SerializeField]
        [Tooltip("Player root that receives CharacterController motion.")]
        private CharacterController characterController;

        [SerializeField]
        [Tooltip("Camera transform used for look and FloodCameraTracker.")]
        private Transform cameraTransform;

        [SerializeField]
        [Tooltip("Flood camera tracker on the player camera.")]
        private FloodCameraTracker cameraTracker;

        [SerializeField]
        [Tooltip("Optional URP underwater effect. Stored as Component so the sample compiles without URP.")]
        private Component underwaterEffect;

        [SerializeField]
        [Tooltip("Optional underwater audio presentation component.")]
        private FloodUnderwaterAudio underwaterAudio;

        [SerializeField]
        [Tooltip("Optional camera telemetry for the HUD.")]
        private FloodCameraTelemetry cameraTelemetry;

        [Header("Controls")]

        [SerializeField]
        [Tooltip("Walk speed in meters per second.")]
        [Min(0.1f)]
        private float moveSpeed = 2.4f;

        [SerializeField]
        [Tooltip("Mouse look sensitivity in degrees per pixel.")]
        [Min(0.01f)]
        private float lookSensitivity = 0.12f;

        [SerializeField]
        [Tooltip("Gravity applied to the CharacterController in meters per second squared.")]
        private float gravity = -20f;

        [SerializeField]
        [Tooltip("Euler angles applied to the vessel root when tilt demo is enabled.")]
        private Vector3 tiltedVesselEuler = new(0f, 0f, 8f);

        private float yaw;
        private float pitch;
        private float verticalVelocity;
        private bool vesselTilted;
        private bool cursorLocked = true;
        private bool closedSystemMode;
        private double closedSystemBaselineVolume = -1d;

        private void Start()
        {
            if (cameraTransform != null)
            {
                var euler = cameraTransform.eulerAngles;
                yaw = euler.y;
                pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            }

            SetCursorLocked(true);
        }

        private void Update()
        {
            if (WasPressedEscape())
                SetCursorLocked(false);

            if (!cursorLocked && WasPressedPrimaryClick())
                SetCursorLocked(true);

            HandleApertureKeys();
            HandleToggleKeys();

            if (WasPressedKeyT())
                ToggleVesselTilt();

            if (WasPressedKeyR())
                DrainAllRegions();

            if (WasPressedKeyC())
                ToggleClosedSystemMode();

            UpdateLook();
            UpdateMove();
        }

        private void OnGUI()
        {
            const float width = 560f;
            var x = 16f;
            var y = 16f;
            GUI.Box(new Rect(x, y, width, 360f), "Region Stress (0.14.3)");
            y += 28f;

            DrawRegionLine(ref y, x, width, "A", regionA);
            DrawRegionLine(ref y, x, width, "Corridor/Stair", regionCorridor);
            DrawRegionLine(ref y, x, width, "B", regionB);

            var totalFinite = TotalFiniteVolume();
            GUI.Label(
                new Rect(x + 14f, y, width - 28f, 20f),
                $"Total finite: {totalFinite:F2} m³"
                + (closedSystemMode && closedSystemBaselineVolume >= 0d
                    ? $"  (closed baseline {closedSystemBaselineVolume:F2})"
                    : string.Empty));
            y += 20f;

            var metrics = simulationManager != null
                ? simulationManager.LastTickMetrics
                : default;
            GUI.Label(
                new Rect(x + 14f, y, width - 28f, 20f),
                $"ConservationError: {metrics.ConservationError:E3}  "
                + $"extIn {metrics.ExternalInflowVolume:F3}  "
                + $"extOut {metrics.ExternalOutflowVolume:F3}  "
                + $"sink {metrics.ConfiguredSinkVolume:F3}");
            y += 20f;

            GUI.Label(
                new Rect(x + 14f, y, width - 28f, 20f),
                $"Breach {FormatAperture(breach)}  "
                + $"Door {FormatAperture(door)}  "
                + $"Hatch {FormatAperture(hatch)}  "
                + $"Pump {(pump != null && pump.IsActive ? "ON" : "OFF")}"
                + (closedSystemMode ? "  [CLOSED SYSTEM]" : string.Empty));
            y += 20f;

            var underwater = cameraTelemetry != null
                ? cameraTelemetry.IsUnderwater
                : cameraTracker != null && cameraTracker.IsUnderwater;
            var depth = cameraTelemetry != null
                ? cameraTelemetry.SubmersionDepthMeters
                : cameraTracker != null ? cameraTracker.SubmersionDepthMeters : 0f;
            GUI.Label(
                new Rect(x + 14f, y, width - 28f, 20f),
                $"Camera underwater: {underwater}  depth {depth:F2} m  "
                + $"URP blend {ReadEffectBlend(underwaterEffect):F2}  "
                + $"audio {(underwaterAudio != null ? underwaterAudio.CurrentUnderwaterBlend : 0f):F2}");
            y += 20f;

            GUI.Label(
                new Rect(x + 14f, y, width - 28f, 20f),
                $"Tilt: {(vesselTilted ? "ON" : "OFF")}  "
                + BakeSummary(regionA, "A") + "  "
                + BakeSummary(regionCorridor, "C") + "  "
                + BakeSummary(regionB, "B"));
            y += 22f;

            GUI.Label(
                new Rect(x + 14f, y, width - 28f, 80f),
                "WASD move, mouse look, Esc unlock, click relock.\n"
                + "1/2/3 breach 25/60/100%  4/5/6 door 0/25/100%  7/8/9 hatch 0/50/100%\n"
                + "B/D/H toggle breach/door/hatch  P pump  C closed-system  T tilt  R drain");
        }

        private void HandleApertureKeys()
        {
            if (WasPressedDigit(1))
                SetFraction(breach, 0.25f);
            if (WasPressedDigit(2))
                SetFraction(breach, 0.6f);
            if (WasPressedDigit(3))
                SetFraction(breach, 1f);
            if (WasPressedDigit(4))
                SetFraction(door, 0f);
            if (WasPressedDigit(5))
                SetFraction(door, 0.25f);
            if (WasPressedDigit(6))
                SetFraction(door, 1f);
            if (WasPressedDigit(7))
                SetFraction(hatch, 0f);
            if (WasPressedDigit(8))
                SetFraction(hatch, 0.5f);
            if (WasPressedDigit(9))
                SetFraction(hatch, 1f);
        }

        private void HandleToggleKeys()
        {
            if (WasPressedKeyB() && breach != null)
                breach.IsOpen = !breach.IsOpen;
            if (WasPressedKeyD() && door != null)
                door.IsOpen = !door.IsOpen;
            if (WasPressedKeyH() && hatch != null)
                hatch.IsOpen = !hatch.IsOpen;
            if (WasPressedKeyP() && pump != null)
                pump.IsActive = !pump.IsActive;
        }

        private void ToggleClosedSystemMode()
        {
            closedSystemMode = !closedSystemMode;
            if (!closedSystemMode)
            {
                closedSystemBaselineVolume = -1d;
                return;
            }

            if (breach != null)
            {
                breach.IsOpen = false;
                breach.OpenFraction = 0f;
            }

            if (pump != null)
                pump.IsActive = false;

            closedSystemBaselineVolume = TotalFiniteVolume();
        }

        private void DrainAllRegions()
        {
            Drain(regionA);
            Drain(regionCorridor);
            Drain(regionB);
            closedSystemMode = false;
            closedSystemBaselineVolume = -1d;
        }

        private static void Drain(FloodRegion region)
        {
            if (region == null)
                return;

            region.RemoveWater(region.CurrentVolume);
        }

        private void ToggleVesselTilt()
        {
            if (vesselRoot == null)
                return;

            vesselTilted = !vesselTilted;
            vesselRoot.localRotation = vesselTilted
                ? Quaternion.Euler(tiltedVesselEuler)
                : Quaternion.identity;
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void UpdateLook()
        {
            if (!cursorLocked || cameraTransform == null)
                return;

            var look = ReadLookDelta();
            yaw += look.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, -85f, 85f);
            cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void UpdateMove()
        {
            if (characterController == null || cameraTransform == null)
                return;

            var input = ReadMoveInput();
            var forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            var right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();

            var planar = (forward * input.y + right * input.x) * moveSpeed;
            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            verticalVelocity += gravity * Time.deltaTime;
            var velocity = new Vector3(planar.x, verticalVelocity, planar.z);
            characterController.Move(velocity * Time.deltaTime);
        }

        private double TotalFiniteVolume()
        {
            double total = 0d;
            if (regionA != null)
                total += regionA.CurrentVolume;
            if (regionCorridor != null)
                total += regionCorridor.CurrentVolume;
            if (regionB != null)
                total += regionB.CurrentVolume;
            return total;
        }

        private static void DrawRegionLine(
            ref float y,
            float x,
            float width,
            string label,
            FloodRegion region)
        {
            if (region == null)
            {
                GUI.Label(new Rect(x + 14f, y, width - 28f, 20f), $"{label}: (missing)");
                y += 20f;
                return;
            }

            GUI.Label(
                new Rect(x + 14f, y, width - 28f, 20f),
                $"{label}: {region.CurrentVolume:F2}/{region.MaximumVolume:F2} m³ "
                + $"({region.FillPercentage * 100f:F0}%)  "
                + $"members {region.Members.Count}  "
                + $"h {region.CurrentHeight:F2} m");
            y += 20f;
        }

        private static string BakeSummary(FloodRegion region, string label)
        {
            if (region == null)
                return $"{label}:—";

            var data = region.BakedRegionData;
            if (data == null || !data.IsUsable)
                return $"{label}:no bake";

            return $"{label}:{data.SampleCount} cells"
                + (data.HasPresentationBoundary ? "+PB" : string.Empty);
        }

        private static string FormatAperture(FloodConnection connection)
        {
            if (connection == null)
                return "—";

            if (!connection.IsOpen)
                return "CLOSED";

            return $"{connection.OpenFraction * 100f:F0}%";
        }

        private static void SetFraction(FloodConnection connection, float fraction)
        {
            if (connection == null)
                return;

            connection.IsOpen = fraction > 0f;
            connection.OpenFraction = fraction;
        }

        private static Vector2 ReadMoveInput()
        {
            var x = 0f;
            var y = 0f;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                    x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                    x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                    y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    y += 1f;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            x += Input.GetAxisRaw("Horizontal");
            y += Input.GetAxisRaw("Vertical");
#endif
            var input = new Vector2(x, y);
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private static Vector2 ReadLookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
                return mouse.delta.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"))
                * 10f;
#else
            return Vector2.zero;
#endif
        }

        private static bool WasPressedEscape() => WasPressedKeyCode(KeyCode.Escape);

        private static bool WasPressedPrimaryClick()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private static bool WasPressedKeyT() => WasPressedKeyCode(KeyCode.T);
        private static bool WasPressedKeyR() => WasPressedKeyCode(KeyCode.R);
        private static bool WasPressedKeyC() => WasPressedKeyCode(KeyCode.C);
        private static bool WasPressedKeyB() => WasPressedKeyCode(KeyCode.B);
        private static bool WasPressedKeyD() => WasPressedKeyCode(KeyCode.D);
        private static bool WasPressedKeyH() => WasPressedKeyCode(KeyCode.H);
        private static bool WasPressedKeyP() => WasPressedKeyCode(KeyCode.P);

        private static bool WasPressedDigit(int digit)
        {
            var code = KeyCode.Alpha0 + digit;
            return WasPressedKeyCode(code);
        }

        private static bool WasPressedKeyCode(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var control = keyCode switch
                {
                    KeyCode.Escape => keyboard.escapeKey,
                    KeyCode.T => keyboard.tKey,
                    KeyCode.R => keyboard.rKey,
                    KeyCode.C => keyboard.cKey,
                    KeyCode.B => keyboard.bKey,
                    KeyCode.D => keyboard.dKey,
                    KeyCode.H => keyboard.hKey,
                    KeyCode.P => keyboard.pKey,
                    KeyCode.Alpha1 => keyboard.digit1Key,
                    KeyCode.Alpha2 => keyboard.digit2Key,
                    KeyCode.Alpha3 => keyboard.digit3Key,
                    KeyCode.Alpha4 => keyboard.digit4Key,
                    KeyCode.Alpha5 => keyboard.digit5Key,
                    KeyCode.Alpha6 => keyboard.digit6Key,
                    KeyCode.Alpha7 => keyboard.digit7Key,
                    KeyCode.Alpha8 => keyboard.digit8Key,
                    KeyCode.Alpha9 => keyboard.digit9Key,
                    _ => null,
                };
                if (control != null && control.wasPressedThisFrame)
                    return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(keyCode);
#else
            return false;
#endif
        }

        private static float ReadEffectBlend(Component effect)
        {
            if (effect == null)
                return 0f;

            var property = effect.GetType().GetProperty(
                "EffectBlend",
                BindingFlags.Instance | BindingFlags.Public);
            if (property == null || property.PropertyType != typeof(float))
                return 0f;

            return (float)property.GetValue(effect);
        }
    }
}
