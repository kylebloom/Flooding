# Flooding package

The Flooding package provides gameplay-focused bulk-water simulation for Unity.
It models compartment flooding with volume-authoritative state, fixed-step
orchestration, connections, exterior boundaries, and optional presentation—not
computational fluid dynamics.

## Requirements

- Unity Editor 6.5 (`6000.5.6f1`)
- No render-pipeline requirement for core simulation, `FloodCameraTracker`,
  underwater audio, or telemetry
- Universal Render Pipeline (`com.unity.render-pipelines.universal` ≥ 17) only
  for the optional `Kyle.Flooding.URP` underwater assembly and the included
  `Materials/Floodwater.mat` / `Materials/FloodUnderwater.mat`
- Built-in, HDRP, or custom pipelines: install Flooding without URP; assign your
  own transparent water material. The URP assembly is excluded automatically
  when Universal RP is not present

## Installation

For a local clone, open **Window > Package Management > Package Manager**,
choose **+ > Install package from disk**, and select
`Packages/com.rabbidwolf.com.kyle.flooding/package.json`.

To install from Git, choose **+ > Install package from git URL** and use:

```text
https://github.com/kylebloom/Flooding.git?path=/Packages/com.rabbidwolf.com.kyle.flooding
```

For a reproducible dependency, append `#<commit-or-tag>` after the package path
to pin a tested revision.

## Getting started

Practical step-by-step setup lives in the
[Unity Editor workflow](editor-workflow.md):

1. [Getting started paths](editor-workflow.md#getting-started) — prefab smoke
   test, sample import, or build-from-components.
2. [Choose your scenario](editor-workflow.md#choose-your-scenario) — leak,
   doorway, hull breach, polygon, baked geometry, vessel mass, visuals/audio,
   diagnostics, and first-person underwater.
3. Component field reference, scripting, upgrades, and troubleshooting in the
   same document.

Camera / underwater presentation (optional):

- [Track a camera](editor-workflow.md#track-a-camera-or-viewpoint-against-flood-volumes)
- [Flood Underwater Profile](editor-workflow.md#create-an-underwater-presentation-profile)
- [URP underwater setup](editor-workflow.md#urp-underwater-camera-effects)
- [Tune underwater look](editor-workflow.md#tune-underwater-look-symptom--where-to-click)
- [Underwater audio](editor-workflow.md#underwater-audio-audiomixer)
- [Telemetry adapters](editor-workflow.md#flood-telemetry-for-ui)
- [Scenario 9 — first-person rising flood](editor-workflow.md#scenario-9--first-person-camera-through-a-rising-flood)

### Fastest smoke test

1. Drag
   `Packages/com.rabbidwolf.com.kyle.flooding/Runtime/Prefabs/Room.prefab`
   into a scene.
2. Enter Play Mode.

Water rises from the nested active `FloodSource`. If the visual is pink, assign
a transparent material compatible with your render pipeline.

### Scenario cheat sheet

| I want to… | Do this |
| --- | --- |
| Fill one room from a pipe/leak | [Scenario 1](editor-workflow.md#scenario-1--single-room-filling-from-a-leak) |
| Connect two rooms with a door | [Scenario 2](editor-workflow.md#scenario-2--two-rooms-equalizing-through-a-doorway) |
| Flood from an ocean/lake waterline | [Scenario 3](editor-workflow.md#scenario-3--hull-breach-against-an-ocean-waterline) |
| Use a custom floor outline | [Scenario 4](editor-workflow.md#scenario-4--non-rectangular-floor-plan-extruded-polygon) |
| Bake a sloped/curved interior | [Scenario 5](editor-workflow.md#scenario-5--sloped-or-uneven-interior-baked-data) |
| Drive Rigidbody mass from water | [Scenario 6](editor-workflow.md#scenario-6--flood-mass-affecting-a-vessel-rigidbody) |
| Add flow VFX or SFX | [Scenario 7](editor-workflow.md#scenario-7--flow-visuals-and-audio) |
| Debug surfaces and flow in Scene view | [Scenario 8](editor-workflow.md#scenario-8--scene-view-diagnostics-while-tuning) |
| First-person waterline / underwater FX | [Scenario 9](editor-workflow.md#scenario-9--first-person-camera-through-a-rising-flood) |
| Soften wavy underwater look | [Tune underwater look](editor-workflow.md#tune-underwater-look-symptom--where-to-click) |

## Units and ownership

- Distance/height: meters
- Volume: cubic meters
- Flow: cubic meters per second
- Density: kilograms per cubic meter
- Mass: kilograms

Water **volume** is authoritative. Height, surface plane, mass, and center of
mass are derived. Presentation components never mutate simulation state.

## Runtime model (overview)

`FloodSimulationManager` ticks registered volumes, sources, connections, and
external boundaries at a fixed rate. Each tick captures immutable boundary
snapshots, evaluates requests, reconciles finite supply/capacity, commits only
finite volume deltas, then publishes state.

- `FloodVolume` — finite compartment (`IFluidBoundary`) with live state reads
  and read-only point queries (`ContainsPoint`, `IsPointSubmerged`,
  `QueryPoint`).
- `ExternalFluidBoundary` — infinite exterior (**External Fluid Body**).
- `FloodConnection` — pressure-driven opening between two boundaries.
- `FloodSource` — configured injection (not pressure equilibrium).
- Surface renderers — optional water visuals from `FloodState`.
- `FloodConnectionVisual` / audio components — optional flow/fill presentation.
- `FloodCameraTracker` — optional viewpoint / underwater state for presentation.
- `FloodUnderwaterProfile` — shared underwater effect settings asset.
- `Kyle.Flooding.URP` — optional URP underwater renderer feature / waterline pass.
- `FloodUnderwaterAudio` / telemetry adapters — optional presentation consumers.
- `FloodMassAggregator` + `RigidbodyFloodMassAdapter` — optional mass reporting.
- `FloodDiagnostics` — optional read-only Scene-view overlay.

## Samples

In **Window > Package Management > Package Manager**, select **Flooding**, open
**Samples**, and import:

| Sample | Import path | Demonstrates |
| --- | --- | --- |
| Flood Mass Integration | `Assets/Samples/Flooding/0.10.0/Flood Mass Integration` | Cutaway barge: visible water, COM markers, roll/pitch from flood mass |
| Baked Geometry | `Assets/Samples/Flooding/0.10.0/Baked Geometry` | Curved hull bake, cell viz, free surface |
| Connected Compartments | `Assets/Samples/Flooding/0.10.0/Connected Compartments` | Conserved doorway equalization |
| Hull Breach | `Assets/Samples/Flooding/0.10.0/Hull Breach` | Ocean waterline ↔ compartment exchange |
| First Person Flooding | `Assets/Samples/Flooding/0.10.0/First Person Flooding` | Rising flood, waterline crossing, URP underwater FX |

Each imported scene is authored and editable before Play Mode. `Samples~` in
the package is authoritative; re-import can overwrite `Assets/Samples` copies.

## Current limitations

Version `0.10.0` does not provide CFD, mixed densities in one compartment,
automatic overflow-edge discovery, runtime rebaking, buoyancy forces, or
bundled audio/particle content. Gravity-aligned surfaces are instantaneous
equilibrium results. See the Editor workflow troubleshooting section when
setup fails.

Camera underwater presentation limitations:

- The URP fullscreen pass treats the active `FloodVolume.SurfacePlane` as an
  **infinite plane**. It does **not** clip screen-space underwater tint/fog to
  the FloodVolume bounds, so geometry visible through openings that lies below
  the same mathematical plane may receive underwater treatment even when it is
  outside the flooded compartment.
- Volume screen masking (stencil, analytic bounds clip, or dedicated water-
  volume rendering) is not implemented yet.
- The **First Person Flooding** sample’s fullscreen waterline effect requires
  URP plus the renderer-feature setup; without URP the sample still compiles
  and runs tracker/telemetry/audio, but the underwater pass is unavailable.

## Package contents

- `Runtime/Simulation` — deterministic rules and boundary contracts.
- `Runtime/Geometry` — container contracts and implementations.
- `Runtime/Query` — gameplay query result and containment precision types.
- `Runtime/State` — immutable snapshots.
- `Runtime/Presentation` — shared presentation helpers.
- `Runtime/Components` — scene adapters and optional presentation/debug.
- `Runtime/Prefabs` — `Room.prefab` and `Flooding.prefab`.
- `Editor` — baking, drawers, and sample builders.
- `Materials` — prototype URP water material.
- `Tests/Editor`, `Tests/PlayMode`, and optional `Tests/PlayMode.URP`.
- `Runtime.URP` — optional underwater renderer feature (compiled only when
  Universal RP ≥ 17 is installed).
- `Samples~` — Mass Integration, Baked Geometry, Connected Compartments, Hull
  Breach, First Person Flooding.
- `Documentation` — this overview and the Editor workflow guide.

## License

MIT — see the repository [LICENSE](https://github.com/kylebloom/Flooding/blob/main/LICENSE).
