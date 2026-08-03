using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Kyle.Flooding.Samples
{
    /// <summary>
    /// SAMPLE ONLY controller for the Flood Mass Integration cutaway barge.
    /// Redistributes compartment volumes, drives an auto-demo, Game-view COM
    /// markers, and HUD. Compartment water presentation is owned by
    /// <see cref="FloodCubeSurfaceRenderer"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Samples/Flood Mass Demo Bootstrap")]
    public sealed class FloodMassDemoBootstrap : MonoBehaviour
    {
        private enum FloodPreset
        {
            Empty = 0,
            Port = 1,
            Starboard = 2,
            Bow = 3,
            Stern = 4,
            StarboardBow = 5,
        }

        private enum AutoDemoPhase
        {
            SettleEmpty,
            ShowStarboard,
            SettleStarboard,
            ResetAfterStarboard,
            ShowBow,
            SettleBow,
            ResetAfterBow,
            ShowStarboardBow,
            SettleStarboardBow,
            ResetAfterStarboardBow,
        }

        [Header("Simulation")]

        [SerializeField]
        [Tooltip("Vessel Rigidbody whose mass and attitude are demonstrated.")]
        private Rigidbody vesselRigidbody;

        [SerializeField]
        [Tooltip("Adapter that applies dry-plus-flood mass to the vessel Rigidbody.")]
        private RigidbodyFloodMassAdapter massAdapter;

        [SerializeField]
        [Tooltip("Aggregator that combines child compartment flood mass.")]
        private FloodMassAggregator massAggregator;

        [SerializeField]
        [Tooltip("Port-bow FloodVolume.")]
        private FloodVolume portBow;

        [SerializeField]
        [Tooltip("Starboard-bow FloodVolume.")]
        private FloodVolume starboardBow;

        [SerializeField]
        [Tooltip("Port-stern FloodVolume.")]
        private FloodVolume portStern;

        [SerializeField]
        [Tooltip("Starboard-stern FloodVolume.")]
        private FloodVolume starboardStern;

        [Header("Presentation")]

        [SerializeField]
        [Tooltip("Game-view marker for the dry-body center of mass.")]
        private Transform dryComMarker;

        [SerializeField]
        [Tooltip("Game-view marker for the aggregate flood-water center of mass.")]
        private Transform floodComMarker;

        [SerializeField]
        [Tooltip("Game-view marker for the combined dry-plus-flood center of mass.")]
        private Transform combinedComMarker;

        [SerializeField]
        [Tooltip("Optional line from dry COM to combined COM.")]
        private LineRenderer comShiftLine;

        [Header("Demo Tuning")]

        [SerializeField]
        [Tooltip("Cubic meters applied to each flooded compartment in keyboard presets.")]
        [Min(0.01f)]
        private float presetVolumePerCompartment = 2.4f;

        [SerializeField]
        [Tooltip("Cubic meters per second transferred by WASD between paired compartments.")]
        [Min(0.01f)]
        private float transferRate = 1.5f;

        [SerializeField]
        [Tooltip("Seconds each auto-demo flood preset remains visible before resetting.")]
        [Min(0.5f)]
        private float autoDemoHoldSeconds = 4f;

        [SerializeField]
        [Tooltip("Seconds to wait after emptying before the next auto-demo preset.")]
        [Min(0.25f)]
        private float autoDemoResetSeconds = 2f;

        [SerializeField]
        [Tooltip("When enabled, cycles flood presets until the user presses a control key.")]
        private bool autoDemoEnabled = true;

        private bool autoDemoActive = true;
        private AutoDemoPhase autoDemoPhase = AutoDemoPhase.SettleEmpty;
        private float autoDemoTimer;
        private Vector3 resetPosition;
        private Quaternion resetRotation;

        private void Awake()
        {
            if (vesselRigidbody == null)
                vesselRigidbody = GetComponent<Rigidbody>();

            if (massAdapter == null)
                massAdapter = GetComponent<RigidbodyFloodMassAdapter>();

            if (massAggregator == null)
                massAggregator = GetComponent<FloodMassAggregator>();

            resetPosition = transform.position;
            resetRotation = transform.rotation;
            autoDemoActive = autoDemoEnabled;
            autoDemoPhase = AutoDemoPhase.SettleEmpty;
            autoDemoTimer = autoDemoResetSeconds;
        }

        private void Start()
        {
            ApplyPreset(FloodPreset.Empty, resetPose: false);
        }

        private void Update()
        {
            HandleKeyboard();

            if (autoDemoActive)
                TickAutoDemo(Time.deltaTime);
        }

        private void LateUpdate()
        {
            RefreshComMarkers();
        }

        private void OnGUI()
        {
            if (massAdapter == null || massAggregator == null || vesselRigidbody == null)
                return;

            var dryMass = massAdapter.DryMass;
            var waterMass = massAggregator.Mass;
            var totalMass = dryMass + waterMass;
            var floodLocal = transform.InverseTransformPoint(
                massAggregator.CenterOfMassWorld);
            var combinedLocal = transform.InverseTransformPoint(
                vesselRigidbody.worldCenterOfMass);
            var euler = transform.localEulerAngles;
            var roll = NormalizeSignedAngle(euler.z);
            var pitch = NormalizeSignedAngle(euler.x);

            const float boxWidth = 420f;
            var boxX = 16f;
            GUI.Box(new Rect(boxX, 16f, boxWidth, 268f), "Flood Mass Integration");
            GUI.Label(
                new Rect(boxX + 14f, 44f, boxWidth - 28f, 20f),
                $"Dry mass:   {dryMass,8:F0} kg");
            GUI.Label(
                new Rect(boxX + 14f, 64f, boxWidth - 28f, 20f),
                $"Water mass: {waterMass,8:F0} kg");
            GUI.Label(
                new Rect(boxX + 14f, 84f, boxWidth - 28f, 20f),
                $"Total mass: {totalMass,8:F0} kg");
            GUI.Label(
                new Rect(boxX + 14f, 110f, boxWidth - 28f, 20f),
                $"Water COM local:  X {floodLocal.x:+0.00;-0.00}  "
                + $"Y {floodLocal.y:+0.00;-0.00}  Z {floodLocal.z:+0.00;-0.00}");
            GUI.Label(
                new Rect(boxX + 14f, 130f, boxWidth - 28f, 20f),
                $"Combined COM loc: X {combinedLocal.x:+0.00;-0.00}  "
                + $"Y {combinedLocal.y:+0.00;-0.00}  Z {combinedLocal.z:+0.00;-0.00}");
            GUI.Label(
                new Rect(boxX + 14f, 156f, boxWidth - 28f, 20f),
                $"Roll: {roll,6:F1}°    Pitch: {pitch,6:F1}°");
            GUI.Label(
                new Rect(boxX + 14f, 182f, boxWidth - 28f, 40f),
                "1 Empty   2 Port   3 Starboard   4 Bow\n"
                + "5 Stern   6 Starboard Bow   R Reset+Auto");
            GUI.Label(
                new Rect(boxX + 14f, 226f, boxWidth - 28f, 40f),
                "A/D transfer port↔starboard   W/S fore↔aft\n"
                + "Visible water observes FloodState; it does not cause the COM shift.");
        }

        private void HandleKeyboard()
        {
            var usedControl = false;
            var pressedReset = WasPressed(KeyCode.R
#if ENABLE_INPUT_SYSTEM
                , Key.R
#endif
            );

            if (WasPressed(KeyCode.Alpha1, KeyCode.Keypad1
#if ENABLE_INPUT_SYSTEM
                , Key.Digit1, Key.Numpad1
#endif
                ))
            {
                ApplyPreset(FloodPreset.Empty, resetPose: false);
                usedControl = true;
            }
            else if (WasPressed(KeyCode.Alpha2, KeyCode.Keypad2
#if ENABLE_INPUT_SYSTEM
                , Key.Digit2, Key.Numpad2
#endif
                ))
            {
                ApplyPreset(FloodPreset.Port, resetPose: false);
                usedControl = true;
            }
            else if (WasPressed(KeyCode.Alpha3, KeyCode.Keypad3
#if ENABLE_INPUT_SYSTEM
                , Key.Digit3, Key.Numpad3
#endif
                ))
            {
                ApplyPreset(FloodPreset.Starboard, resetPose: false);
                usedControl = true;
            }
            else if (WasPressed(KeyCode.Alpha4, KeyCode.Keypad4
#if ENABLE_INPUT_SYSTEM
                , Key.Digit4, Key.Numpad4
#endif
                ))
            {
                ApplyPreset(FloodPreset.Bow, resetPose: false);
                usedControl = true;
            }
            else if (WasPressed(KeyCode.Alpha5, KeyCode.Keypad5
#if ENABLE_INPUT_SYSTEM
                , Key.Digit5, Key.Numpad5
#endif
                ))
            {
                ApplyPreset(FloodPreset.Stern, resetPose: false);
                usedControl = true;
            }
            else if (WasPressed(KeyCode.Alpha6, KeyCode.Keypad6
#if ENABLE_INPUT_SYSTEM
                , Key.Digit6, Key.Numpad6
#endif
                ))
            {
                ApplyPreset(FloodPreset.StarboardBow, resetPose: false);
                usedControl = true;
            }
            else if (pressedReset)
            {
                ResetVesselPose();
                ApplyPreset(FloodPreset.Empty, resetPose: false);
                autoDemoActive = autoDemoEnabled;
                autoDemoPhase = AutoDemoPhase.SettleEmpty;
                autoDemoTimer = autoDemoResetSeconds;
                usedControl = true;
            }

            var transfer = transferRate * Time.deltaTime;
            if (IsHeld(KeyCode.A
#if ENABLE_INPUT_SYSTEM
                , Key.A
#endif
                ))
            {
                TransferPair(starboardBow, portBow, transfer);
                TransferPair(starboardStern, portStern, transfer);
                usedControl = true;
            }
            else if (IsHeld(KeyCode.D
#if ENABLE_INPUT_SYSTEM
                , Key.D
#endif
                ))
            {
                TransferPair(portBow, starboardBow, transfer);
                TransferPair(portStern, starboardStern, transfer);
                usedControl = true;
            }

            if (IsHeld(KeyCode.W
#if ENABLE_INPUT_SYSTEM
                , Key.W
#endif
                ))
            {
                TransferPair(portStern, portBow, transfer);
                TransferPair(starboardStern, starboardBow, transfer);
                usedControl = true;
            }
            else if (IsHeld(KeyCode.S
#if ENABLE_INPUT_SYSTEM
                , Key.S
#endif
                ))
            {
                TransferPair(portBow, portStern, transfer);
                TransferPair(starboardBow, starboardStern, transfer);
                usedControl = true;
            }

            if (usedControl && !pressedReset)
                autoDemoActive = false;
        }

#if ENABLE_INPUT_SYSTEM
        private static bool WasPressed(KeyCode legacyA, KeyCode legacyB, Key modernA, Key modernB)
        {
            return WasPressed(legacyA, modernA) || WasPressed(legacyB, modernB);
        }

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

        private static bool IsHeld(KeyCode legacy, Key modern)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[modern].isPressed)
                return true;

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(legacy);
#else
            return false;
#endif
        }
#else
        private static bool WasPressed(KeyCode legacyA, KeyCode legacyB)
        {
            return Input.GetKeyDown(legacyA) || Input.GetKeyDown(legacyB);
        }

        private static bool WasPressed(KeyCode legacy)
        {
            return Input.GetKeyDown(legacy);
        }

        private static bool IsHeld(KeyCode legacy)
        {
            return Input.GetKey(legacy);
        }
#endif

        private void TickAutoDemo(float deltaTime)
        {
            autoDemoTimer -= deltaTime;
            if (autoDemoTimer > 0f)
                return;

            switch (autoDemoPhase)
            {
                case AutoDemoPhase.SettleEmpty:
                    ApplyPreset(FloodPreset.Starboard, resetPose: false);
                    autoDemoPhase = AutoDemoPhase.ShowStarboard;
                    autoDemoTimer = autoDemoHoldSeconds;
                    break;

                case AutoDemoPhase.ShowStarboard:
                    autoDemoPhase = AutoDemoPhase.SettleStarboard;
                    autoDemoTimer = 0.01f;
                    break;

                case AutoDemoPhase.SettleStarboard:
                    ApplyPreset(FloodPreset.Empty, resetPose: false);
                    ResetVesselPose();
                    autoDemoPhase = AutoDemoPhase.ResetAfterStarboard;
                    autoDemoTimer = autoDemoResetSeconds;
                    break;

                case AutoDemoPhase.ResetAfterStarboard:
                    ApplyPreset(FloodPreset.Bow, resetPose: false);
                    autoDemoPhase = AutoDemoPhase.ShowBow;
                    autoDemoTimer = autoDemoHoldSeconds;
                    break;

                case AutoDemoPhase.ShowBow:
                    autoDemoPhase = AutoDemoPhase.SettleBow;
                    autoDemoTimer = 0.01f;
                    break;

                case AutoDemoPhase.SettleBow:
                    ApplyPreset(FloodPreset.Empty, resetPose: false);
                    ResetVesselPose();
                    autoDemoPhase = AutoDemoPhase.ResetAfterBow;
                    autoDemoTimer = autoDemoResetSeconds;
                    break;

                case AutoDemoPhase.ResetAfterBow:
                    ApplyPreset(FloodPreset.StarboardBow, resetPose: false);
                    autoDemoPhase = AutoDemoPhase.ShowStarboardBow;
                    autoDemoTimer = autoDemoHoldSeconds;
                    break;

                case AutoDemoPhase.ShowStarboardBow:
                    autoDemoPhase = AutoDemoPhase.SettleStarboardBow;
                    autoDemoTimer = 0.01f;
                    break;

                case AutoDemoPhase.SettleStarboardBow:
                    ApplyPreset(FloodPreset.Empty, resetPose: false);
                    ResetVesselPose();
                    autoDemoPhase = AutoDemoPhase.ResetAfterStarboardBow;
                    autoDemoTimer = autoDemoResetSeconds;
                    break;

                case AutoDemoPhase.ResetAfterStarboardBow:
                    autoDemoPhase = AutoDemoPhase.SettleEmpty;
                    autoDemoTimer = autoDemoResetSeconds;
                    break;
            }
        }

        private void ApplyPreset(FloodPreset preset, bool resetPose)
        {
            if (resetPose)
                ResetVesselPose();

            var v = presetVolumePerCompartment;
            switch (preset)
            {
                case FloodPreset.Empty:
                    SetTargetVolumes(0f, 0f, 0f, 0f);
                    break;
                case FloodPreset.Port:
                    SetTargetVolumes(v, 0f, v, 0f);
                    break;
                case FloodPreset.Starboard:
                    SetTargetVolumes(0f, v, 0f, v);
                    break;
                case FloodPreset.Bow:
                    SetTargetVolumes(v, v, 0f, 0f);
                    break;
                case FloodPreset.Stern:
                    SetTargetVolumes(0f, 0f, v, v);
                    break;
                case FloodPreset.StarboardBow:
                    SetTargetVolumes(0f, v * 1.25f, 0f, 0f);
                    break;
            }
        }

        private void SetTargetVolumes(
            float portBowVolume,
            float starboardBowVolume,
            float portSternVolume,
            float starboardSternVolume)
        {
            SetVolume(portBow, portBowVolume);
            SetVolume(starboardBow, starboardBowVolume);
            SetVolume(portStern, portSternVolume);
            SetVolume(starboardStern, starboardSternVolume);
            massAdapter?.ApplyMassContribution();

            // Direct volume writes are outside a manager tick; snap renderers
            // so water visuals and COM markers update immediately.
            var renderers = GetComponentsInChildren<FloodCubeSurfaceRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
                renderers[i].SnapToCurrentState();
        }

        private static void SetVolume(FloodVolume volume, float targetCubicMeters)
        {
            if (volume == null)
                return;

            var target = Mathf.Clamp(targetCubicMeters, 0f, volume.MaximumVolume);
            var current = volume.CurrentVolume;
            var delta = target - current;

            if (Mathf.Abs(delta) <= 0.0001f)
                return;

            if (delta > 0f)
                volume.AddWater(delta);
            else
                volume.RemoveWater(-delta);
        }

        private static void TransferPair(
            FloodVolume source,
            FloodVolume destination,
            float cubicMeters)
        {
            if (source == null || destination == null || cubicMeters <= 0f)
                return;

            var available = source.CurrentVolume;
            var capacity = Mathf.Max(0f, destination.MaximumVolume - destination.CurrentVolume);
            var amount = Mathf.Min(cubicMeters, available, capacity);
            if (amount <= 0f)
                return;

            source.RemoveWater(amount);
            destination.AddWater(amount);
        }

        private void ResetVesselPose()
        {
            if (vesselRigidbody != null)
            {
                vesselRigidbody.linearVelocity = Vector3.zero;
                vesselRigidbody.angularVelocity = Vector3.zero;
                vesselRigidbody.position = resetPosition;
                vesselRigidbody.rotation = resetRotation;
            }
            else
            {
                transform.SetPositionAndRotation(resetPosition, resetRotation);
            }
        }

        private void RefreshComMarkers()
        {
            if (massAdapter == null || massAggregator == null || vesselRigidbody == null)
                return;

            var dryWorld = transform.TransformPoint(massAdapter.DryCenterOfMassLocal);
            var floodContribution = massAggregator.CurrentContribution;
            var floodWorld = floodContribution.Mass > 0d
                ? floodContribution.CenterOfMassWorld
                : dryWorld;
            var combinedWorld = vesselRigidbody.worldCenterOfMass;

            if (dryComMarker != null)
            {
                dryComMarker.position = dryWorld;
                dryComMarker.gameObject.SetActive(true);
            }

            if (floodComMarker != null)
            {
                floodComMarker.position = floodWorld;
                floodComMarker.gameObject.SetActive(floodContribution.Mass > 0d);
            }

            if (combinedComMarker != null)
            {
                combinedComMarker.position = combinedWorld;
                combinedComMarker.gameObject.SetActive(true);
            }

            if (comShiftLine != null)
            {
                comShiftLine.positionCount = 2;
                comShiftLine.SetPosition(0, dryWorld);
                comShiftLine.SetPosition(1, combinedWorld);
                comShiftLine.enabled = floodContribution.Mass > 0d;
            }
        }

        private static float NormalizeSignedAngle(float degrees)
        {
            var value = degrees;
            while (value > 180f)
                value -= 360f;
            while (value < -180f)
                value += 360f;
            return value;
        }

        private void OnValidate()
        {
            presetVolumePerCompartment = Mathf.Max(0.01f, presetVolumePerCompartment);
            transferRate = Mathf.Max(0.01f, transferRate);
            autoDemoHoldSeconds = Mathf.Max(0.5f, autoDemoHoldSeconds);
            autoDemoResetSeconds = Mathf.Max(0.25f, autoDemoResetSeconds);

            if (vesselRigidbody == null)
                vesselRigidbody = GetComponent<Rigidbody>();
            if (massAdapter == null)
                massAdapter = GetComponent<RigidbodyFloodMassAdapter>();
            if (massAggregator == null)
                massAggregator = GetComponent<FloodMassAggregator>();
        }
    }
}
