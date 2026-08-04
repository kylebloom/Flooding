#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;
using Kyle.Flooding.URP;

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// Sample-only first-person controller and Game-view HUD for the
    /// First Person Flooding demo. Simulation and water mesh ownership stay
    /// with package components.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Samples/First Person Flooding Bootstrap")]
    public sealed class FirstPersonFloodingBootstrap : MonoBehaviour
    {
        [Header("Simulation")]

        [SerializeField]
        [Tooltip("Flooded room compartment.")]
        private FloodVolume roomVolume;

        [SerializeField]
        [Tooltip("Inflow source that raises the room water.")]
        private FloodSource inflowSource;

        [SerializeField]
        [Tooltip("Room root rotated when demonstrating a tilted compartment.")]
        private Transform roomRoot;

        [Header("Player")]

        [SerializeField]
        [Tooltip("Player root that receives CharacterController motion.")]
        private CharacterController characterController;

        [SerializeField]
        [Tooltip("Camera transform used for look and FloodCameraTracker viewpoint.")]
        private Transform cameraTransform;

        [SerializeField]
        [Tooltip("Flood camera tracker on the player camera.")]
        private FloodCameraTracker cameraTracker;

        [SerializeField]
        [Tooltip("Optional URP underwater camera effect bridge.")]
        private FloodUnderwaterCameraEffect underwaterEffect;

        [SerializeField]
        [Tooltip("Optional underwater audio presentation component.")]
        private FloodUnderwaterAudio underwaterAudio;

        [SerializeField]
        [Tooltip("Optional volume telemetry for the HUD.")]
        private FloodVolumeTelemetry volumeTelemetry;

        [SerializeField]
        [Tooltip("Optional camera telemetry for the HUD.")]
        private FloodCameraTelemetry cameraTelemetry;

        [Header("Controls")]

        [SerializeField]
        [Tooltip("Walk speed in meters per second.")]
        [Min(0.1f)]
        private float moveSpeed = 2.2f;

        [SerializeField]
        [Tooltip("Mouse look sensitivity in degrees per pixel.")]
        [Min(0.01f)]
        private float lookSensitivity = 0.12f;

        [SerializeField]
        [Tooltip("Gravity applied to the CharacterController in meters per second squared.")]
        private float gravity = -20f;

        [SerializeField]
        [Tooltip("Euler angles applied to the room root when tilt demo is enabled.")]
        private Vector3 tiltedRoomEuler = new(0f, 0f, 12f);

        private float yaw;
        private float pitch;
        private float verticalVelocity;
        private bool roomTilted;
        private bool cursorLocked = true;

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

            if (WasPressedKeyT())
                ToggleRoomTilt();

            if (WasPressedKeyR() && roomVolume != null)
                roomVolume.RemoveWater(roomVolume.CurrentVolume);

            UpdateLook();
            UpdateMove();
        }

        private void OnGUI()
        {
            const float width = 520f;
            var x = 16f;
            GUI.Box(new Rect(x, 16f, width, 210f), "First Person Flooding");

            var fill = volumeTelemetry != null
                ? volumeTelemetry.FillPercentage
                : roomVolume != null ? roomVolume.FillPercentage : 0f;
            var volume = volumeTelemetry != null
                ? volumeTelemetry.CurrentVolumeCubicMeters
                : roomVolume != null ? roomVolume.CurrentVolume : 0f;
            var capacity = volumeTelemetry != null
                ? volumeTelemetry.CapacityCubicMeters
                : roomVolume != null ? roomVolume.MaximumVolume : 0f;

            var underwater = cameraTelemetry != null
                ? cameraTelemetry.IsUnderwater
                : cameraTracker != null && cameraTracker.IsUnderwater;
            var depth = cameraTelemetry != null
                ? cameraTelemetry.SubmersionDepthMeters
                : cameraTracker != null ? cameraTracker.SubmersionDepthMeters : 0f;
            var signed = cameraTelemetry != null
                ? cameraTelemetry.SurfaceSignedDistanceMeters
                : cameraTracker != null
                    ? cameraTracker.SurfaceSignedDistanceMeters
                    : 0f;

            GUI.Label(
                new Rect(x + 14f, 44f, width - 28f, 20f),
                $"Room: {volume:F2} / {capacity:F2} m³  ({fill * 100f:F0}% full)");
            GUI.Label(
                new Rect(x + 14f, 66f, width - 28f, 20f),
                $"Camera underwater: {underwater}  depth {depth:F2} m  "
                + $"signed {signed:F2} m");
            GUI.Label(
                new Rect(x + 14f, 88f, width - 28f, 20f),
                $"URP effect blend: "
                + $"{(underwaterEffect != null ? underwaterEffect.EffectBlend : 0f):F2}  "
                + $"audio blend: "
                + $"{(underwaterAudio != null ? underwaterAudio.CurrentUnderwaterBlend : 0f):F2}");
            GUI.Label(
                new Rect(x + 14f, 110f, width - 28f, 20f),
                $"Room tilt: {(roomTilted ? "ON" : "OFF")}  "
                + $"source rate: "
                + $"{(inflowSource != null ? inflowSource.FlowRate : 0f):F2} m³/s");
            GUI.Label(
                new Rect(x + 14f, 132f, width - 28f, 40f),
                "WASD move, mouse look, Esc unlock cursor, click to relock.\n"
                + "T toggle room tilt (waterline follows SurfacePlane). R drain.");
            GUI.Label(
                new Rect(x + 14f, 176f, width - 28f, 40f),
                "URP: enable Depth Texture + Flood Underwater Renderer Feature "
                + "on your URP Renderer, assign FloodUnderwater material.");
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

        private void ToggleRoomTilt()
        {
            if (roomRoot == null)
                return;

            roomTilted = !roomTilted;
            roomRoot.localRotation = roomTilted
                ? Quaternion.Euler(tiltedRoomEuler)
                : Quaternion.identity;
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
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

        private static bool WasPressedEscape()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

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

        private static bool WasPressedKeyT()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.T);
#else
            return false;
#endif
        }

        private static bool WasPressedKeyR()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }
    }
}
