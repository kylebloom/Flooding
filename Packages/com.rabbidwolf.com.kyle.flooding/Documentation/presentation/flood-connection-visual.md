# FloodConnectionVisual

`FloodConnectionVisual` is an optional VFX consumer for a
[`FloodConnection`](../components/flood-connection.md). It reads applied flow
rate and world flow direction, then drives an indicator Transform, particle
emission, and/or a mesh toggle. It never changes connection state or simulation.

Menu: **Add Component > Flooding > Flood Connection Visual**.

## Use this when

- You want a door, hatch, or breach to show flow direction and intensity.
- You already have (or will author) indicator / particle / mesh content — the
  package does not ship particle systems or flow meshes.

## Do not use this when

- You need localized jet + floor-spread storytelling at the opening — use
  [local ingress](../local-ingress.md) instead (or in addition).
- You only need sound — use [`FloodConnectionAudio`](audio.md#floodconnectionaudio).

## Beginner setup

1. Select the GameObject that has **Flood Connection** (or a child at the
   opening).
2. **Add Component > Flood Connection Visual**.
3. Confirm **Connection** resolves (auto on same GameObject).
4. Assign any combination of:
   - **Flow Indicator** — Transform oriented along flow and scaled by intensity
   - **Flow Particles** — ParticleSystem emission rate scales with \|flow\|
   - **Flow Mesh** — MeshRenderer enabled while flowing
5. Leave thresholds at defaults until you see live flow rates:
   - **Low Flow Threshold**: `0.25` m³/s
   - **High Flow Threshold**: `2` m³/s

## Key Inspector fields

| Field | Unit / notes | Default |
| --- | --- | --- |
| **Connection** | Source of applied flow / direction | Auto same GO |
| **Flow Indicator** | Optional Transform | — |
| **Flow Particles** | Optional ParticleSystem | — |
| **Flow Mesh** | Optional MeshRenderer | — |
| **Low Flow Threshold** | Absolute applied flow (m³/s) for low band | `0.25` |
| **High Flow Threshold** | Absolute applied flow (m³/s) at saturation | `2` |
| **Indicator Scale At Full Flow** | Local scale multiplier at full intensity | `2` |
| **Particle Emission At Full Flow** | particles/s at full intensity | `40` |

Intensity is a 0–1 value between the low and high thresholds (shared helper with
connection audio).

## Verification checklist

1. Enter Play Mode with unequal water levels across the connection (or an
   exterior breach with head).
2. Confirm the indicator points along `FlowDirectionWorld` when flowing.
3. Confirm particles emit only while applied flow is non-idle.
4. Disable this component and confirm water volume / tick metrics are unchanged.

## Common mistakes

- Expecting visuals without live applied flow (`IsOpen` false, `OpenFraction`
  zero, or equalized levels).
- Assigning no visual targets (component runs but nothing is visible).
- Using configured source rate mentally — this component uses **measured**
  connection `CurrentFlowRate`, not `FloodSource.FlowRate`.
- Zero flow direction falls back to the visual GameObject’s `transform.forward`.

## Runtime API notes

```csharp
var visual = connection.GetComponent<FloodConnectionVisual>();
visual.Refresh();
float intensity = visual.CurrentIntensity; // 0–1
```

- Properties: `Connection`, `FlowIndicator`, `FlowParticles`, `FlowMesh`,
  `CurrentIntensity`
- `Refresh()` — apply current diagnostics immediately (also runs from
  `LateUpdate`)

## Related

- [Audio — FloodConnectionAudio](audio.md#floodconnectionaudio)
- [Local ingress](../local-ingress.md)
- [Scenario 7](../editor-workflow.md#scenario-7--flow-visuals-and-audio)
