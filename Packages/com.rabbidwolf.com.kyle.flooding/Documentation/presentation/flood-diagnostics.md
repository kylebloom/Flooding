# FloodDiagnostics

`FloodDiagnostics` is a read-only Scene-view / Inspector overlay for flooding
and optional Rigidbody mass state. It does **not** advance a manager tick, alter
connections, change water volume, or write Rigidbody mass properties.

Menu: **Add Component > Flooding > Flood Diagnostics**.

## Use this when

- You are tuning compartments, connections, gravity, or flood-mass COM in the
  Editor.
- You want visual confirmation of solved surface planes and flow direction.

## Do not use this when

- You need runtime player-facing VFX — use surface renderers, connection visual,
  or local ingress instead.
- You need UI numbers in builds — use [telemetry](telemetry.md).

## Beginner setup

1. Select the vessel root or shared manager root (`Flood System`).
2. **Add Component > Flood Diagnostics**. Attach **one** diagnostic component to
   the root — do not add one per compartment.
3. Leave **Discover Children** enabled when volumes/connections live under that
   root (includes inactive children).
4. If the hierarchy is split, disable **Discover Children** and assign
   **Volumes** / **Connections** explicitly.
5. Assign **Simulation Manager** when it is not under the diagnostic root
   (supplies active gravity).
6. For mass work, assign **Mass Adapter** and **Target Rigidbody** when they are
   not under the root.
7. Keep the diagnostics GameObject **selected** and the Scene view visible while
   playing or scrubbing.

Overlays draw only while the diagnostics GameObject is selected.

## Key Inspector fields

### Sources

| Field | Notes | Default |
| --- | --- | --- |
| **Simulation Manager** | Active gravity display | Auto children |
| **Mass Adapter** | Dry mass / dry COM | Auto children |
| **Target Rigidbody** | Combined mass / world COM | Auto children |
| **Discover Children** | Find volumes/connections below root | `true` |
| **Volumes** / **Connections** | Explicit lists when discovery off | empty |

### Visibility

| Field | Shows | Default |
| --- | --- | --- |
| **Show Centers Of Mass** | Water / dry / combined COM | `true` |
| **Show Gravity** | Active gravity vector | `true` |
| **Show Surface Planes** | Solved planes + volume labels | `true` |
| **Show Connections** | Flow arrows + head/rate labels | `true` |

### Scene view scale (world meters)

| Field | Default |
| --- | --- |
| **Center Of Mass Marker Radius** | `0.15` |
| **Gravity Arrow Length** | `2` |
| **Surface Plane Size** | `2` |
| **Flow Arrow Length** | `1` |

### Scene view colors

Water COM, dry COM, combined COM, gravity, surface plane, and connection colors
are configurable in the Inspector (defaults are cyan / amber / magenta / red /
cyan / green).

## What you should see

With the diagnostics GameObject selected in Play Mode:

- Aggregate water COM and mass (kg)
- Configured dry COM / mass when a mass adapter is present
- Current combined Rigidbody COM / mass
- Active gravity (m/s²)
- Each volume’s solved surface plane and water volume (m³)
- Each connection’s direction, pressure head (m), requested/applied rates (m³/s)

## Runtime API notes

```csharp
var diagnostics = root.GetComponent<FloodDiagnostics>();
FloodDiagnosticSnapshot snapshot = diagnostics.CaptureSnapshot();
```

Visibility, scale, and color are getter-only from scripts; configure them in the
Inspector. `CaptureSnapshot()` is read-only.

## Verification checklist

1. Select the diagnostics root; confirm gizmos appear in the Scene view.
2. Raise water; confirm surface planes and COM markers move.
3. Open a connection with head; confirm flow arrows and rates update.
4. Confirm water volume is unchanged after enabling/disabling diagnostics.

## Common mistakes

- Expecting gizmos without selecting the diagnostics GameObject.
- Adding diagnostics on every compartment (noisy; one root is enough).
- Discovery off with empty explicit lists (nothing to draw).
- Treating diagnostics as a substitute for player visuals.

## Related

- [Scenario 8](../editor-workflow.md#scenario-8--scene-view-diagnostics-while-tuning)
- [Editor workflow — diagnostics](../editor-workflow.md#inspect-flooding-diagnostics-in-the-scene-view)
- [Surface renderers](surface-renderers.md) for runtime water meshes
