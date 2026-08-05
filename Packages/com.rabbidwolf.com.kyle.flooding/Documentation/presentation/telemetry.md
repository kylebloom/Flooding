# Flood telemetry adapters

Telemetry adapters expose framework-neutral flood presentation values for UI
bindings. They do **not** depend on TextMeshPro or uGUI and never mutate
simulation. Bind their public properties from your own UI, or subscribe to
`ValuesChanged`.

| Component | Source | Typical use |
| --- | --- | --- |
| `FloodVolumeTelemetry` | `FloodVolume` (+ optional connection) | Fill bars, volume readouts, door flow HUD |
| `FloodCameraTelemetry` | `FloodCameraTracker` | Underwater indicator, depth meter |

TMP / uGUI text binders belong in Samples or game code — not in the core
package.

---

## FloodVolumeTelemetry

Menu: **Add Component > Flooding > Flood Volume Telemetry**.

### Use this when

- HUD or systems need fill percentage, cubic meters, capacity, or optional
  connection flow without coupling to a specific UI framework.

### Beginner setup

1. Select a GameObject (often the compartment root).
2. **Add Component > Flood Volume Telemetry**.
3. Assign **Volume** (or leave empty to use a `FloodVolume` on the same object).
4. Optionally assign **Connection** to report `CurrentFlowRate`.

### Key Inspector fields

| Field | Notes | Default |
| --- | --- | --- |
| **Volume** | Fill / capacity source | Auto same GO |
| **Connection** | Optional flow rate source | — |
| **Update Automatically** | LateUpdate Refresh | `true` |

### Exposed values

| Property | Meaning |
| --- | --- |
| `FillPercentage` | 0–1 |
| `CurrentVolumeCubicMeters` | Current water (m³) |
| `CapacityCubicMeters` | Capacity (m³) |
| `IsEmpty` / `IsFull` | Boolean gates |
| `ConnectionFlowRateCubicMetersPerSecond` | Signed applied flow, or `0` if none |
| `HasConnection` | Whether a connection is assigned |

### Scripting example

```csharp
var telemetry = compartment.GetComponent<FloodVolumeTelemetry>();
telemetry.ValuesChanged += () =>
{
    fillBar.value = telemetry.FillPercentage;
    volumeLabel = $"{telemetry.CurrentVolumeCubicMeters:0.0} m³";
};
```

For region members, values follow the volume’s reported state (region-owned
water when bound).

---

## FloodCameraTelemetry

Menu: **Add Component > Flooding > Flood Camera Telemetry**.

### Use this when

- UI needs underwater / depth state from an existing
  [`FloodCameraTracker`](../components/flood-camera-tracker.md).

### Beginner setup

1. Select the camera GameObject (recommended) or a UI director object.
2. **Add Component > Flood Camera Telemetry**.
3. Assign **Tracker**, or leave empty to resolve from this object / Main Camera.
4. Ensure the tracker is configured (explicit volume or auto-discover).

### Key Inspector fields

| Field | Notes | Default |
| --- | --- | --- |
| **Tracker** | Underwater / depth source | Auto resolve |
| **Update Automatically** | LateUpdate Refresh | `true` |

### Exposed values

| Property | Meaning |
| --- | --- |
| `IsInsideFloodVolume` | Viewpoint inside active volume geometry |
| `IsUnderwater` | Latched underwater (hysteresis applied) |
| `SurfaceSignedDistanceMeters` | Positive above water (m) |
| `SubmersionDepthMeters` | Depth when submerged; else `0` (m) |
| `ActiveVolume` | Selected `FloodVolume`, or null |

### Scripting example

```csharp
var camTelemetry = Camera.main.GetComponent<FloodCameraTelemetry>();
camTelemetry.ValuesChanged += () =>
{
    underwaterIcon.enabled = camTelemetry.IsUnderwater;
    depthText = camTelemetry.SubmersionDepthMeters.ToString("0.00");
};
```

### Common mistakes

- Telemetry without a working tracker (all values stay dry / null).
- Binding UI every frame without `ValuesChanged` when you only need change
  notifications — either is valid; pick one pattern.
- Expecting compartment screen-space clipping data — telemetry mirrors tracker
  plane distance only.

---

## Verification checklist

1. Enter Play Mode with rising water.
2. Confirm volume telemetry fill rises with `FloodVolume` / region state.
3. Confirm camera telemetry toggles underwater near the waterline without flicker
   (tune tracker hysteresis if needed).
4. Confirm disabling telemetry does not change simulation.

## Related

- [Camera tracker](../components/flood-camera-tracker.md)
- [Editor workflow — telemetry](../editor-workflow.md#flood-telemetry-for-ui)
- [Scenario 9](../editor-workflow.md#scenario-9--first-person-camera-through-a-rising-flood)
