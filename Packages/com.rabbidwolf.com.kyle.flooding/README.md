# Flooding

Flooding is a reusable, gameplay-focused flooding simulation package for Unity
6.5. The current `0.12.0` prototype models gravity-aligned water volume inside
rotated rectangular, extruded-polygon, or Editor-baked complex compartments.

## Current features

- Volume-authoritative flooding simulation measured in cubic meters.
- Reusable container geometry contract for capacity, submerged volume,
  centroid, bounds, and surface contours.
- Rectangular and validated concave polygon-prism compartments.
- Editor-baked geometry data for closed meshes with sloped, curved, or uneven
  interiors; runtime performs no source-mesh analysis.
- Gravity-aligned surfaces solved from authoritative volume.
- Global or manager-specific gravity with documented zero-gravity fallback.
- Immutable state snapshots with surface, mass, and center-of-mass data.
- Read-only gameplay point queries (`ContainsPoint`, `IsPointSubmerged`,
  `QueryPoint`) over live authoritative volume and surface state.
- Aggregate child-compartment flood mass and optional owned-baseline Rigidbody
  mass and center-of-mass integration.
- Infinite exterior exchange through `ExternalFluidBoundary` (**External Fluid
  Body**) connected by `FloodConnection`.
- Exact accepted and rejected quantities for volume changes.
- Fixed-rate simulation with post-commit state publication.
- Configurable direct inflow through `FloodSource`.
- Bidirectional pressure-driven flow through `FloodConnection`.
- Runtime opening control via `FloodConnection.IsOpen` and `OpenFraction`
  (effective-aperture multiplier for doors, hatches, valves, and damage).
- Simultaneous finite-volume transfer reconciliation.
- Replaceable, interpolated presentation driven by immutable state.
- Scaled-cube and generated polygon-mesh water renderers.
- Focused baked-data free-surface renderer.
- Optional `FloodCameraTracker` for viewpoint / underwater presentation state
  (sticky volume selection, hysteresis; no rendering).
- Optional `Kyle.Flooding.URP` underwater fullscreen effect with camera-ray /
  surface-plane waterline crossing (compiled only when Universal RP ≥ 17 is
  installed; requires URP depth texture).
- Optional underwater AudioMixer muffling and framework-neutral flood telemetry
  adapters (no TextMeshPro dependency).
- Optional `FloodConnectionVisual` and connection/source/volume audio consumers.
- Optional local ingress presentation (`FloodLocalIngressPresenter`) that shows
  stream/shallow-spread visuals converging to the bulk free surface without a
  second authoritative water volume.
- Optional read-only Scene-view diagnostics for mass centers, gravity, solved
  surfaces, and connection flow.
- Basic transparent water material and example room prefabs.

## Prerequisites

- Unity Editor 6.5 (`6000.5.6f1`).
- The core simulation, geometry, `FloodCameraTracker`, audio, and telemetry are
  render-pipeline independent. Universal RP is **not** required to install or
  compile the package.
- The optional `Kyle.Flooding.URP` assembly (underwater renderer feature /
  waterline shader) compiles only when
  `com.unity.render-pipelines.universal` ≥ 17 is present.
- The included `Materials/Floodwater.mat` and `Materials/FloodUnderwater.mat`
  are Universal Render Pipeline (URP) materials. In Built-in, HDRP, or a custom
  render pipeline, assign your own transparent material to each water visual.

## Install

### Add package from disk

Use this option when this repository is already on your computer:

1. In Unity, open **Window > Package Management > Package Manager**.
2. Select **+ > Install package from disk**.
3. Select
   `Packages/com.rabbidwolf.com.kyle.flooding/package.json` inside this
   repository.

Do not select the repository root or copy the package into `Assets`.

### Add package from Git URL

1. In **Window > Package Management > Package Manager**, select
   **+ > Install package from git URL**.
2. Enter:

   ```text
   https://github.com/kylebloom/Flooding.git?path=/Packages/com.rabbidwolf.com.kyle.flooding
   ```

For reproducible projects, pin a tested commit or release tag by appending a
revision after the package path, for example:

```text
https://github.com/kylebloom/Flooding.git?path=/Packages/com.rabbidwolf.com.kyle.flooding#<commit-or-tag>
```

An unpinned URL follows the repository's default branch and can change when
Unity resolves the dependency again.

## 60-second quick start: ready-made room

1. In the Project window, open
   `Packages/com.rabbidwolf.com.kyle.flooding/Runtime/Prefabs`.
2. Drag `Room.prefab` into an open scene.
3. Enter Play Mode.

The nested `Flooding` GameObject contains the configured manager, rectangular
volume, active source, and water visual. Water rises automatically.
`Room.prefab` also supplies the surrounding floor and walls and is the fastest
visual smoke test. `Flooding.prefab` contains only the reusable configured
flooding unit, so use it when you already have environment geometry.

The included visual uses the URP-only `Floodwater.mat`. If the simulation runs
but water is invisible or pink in another pipeline, assign a compatible
transparent material to the nested `WaterVisual` Mesh Renderer.

## Getting started and scenarios

For complete step-by-step Editor setup, open the
[Unity Editor workflow](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md):

- [Getting started](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md#getting-started)
  — prefab path, sample import, or build-from-components.
- [Choose your scenario](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md#choose-your-scenario)
  — leak, doorway, hull breach, polygon, baked geometry, vessel mass,
  visuals/audio, and diagnostics.

| Scenario | Link |
| --- | --- |
| Single room + leak (`FloodSource`) | [Scenario 1](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md#scenario-1--single-room-filling-from-a-leak) |
| Two rooms + doorway | [Scenario 2](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md#scenario-2--two-rooms-equalizing-through-a-doorway) |
| Ocean / hull breach | [Scenario 3](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md#scenario-3--hull-breach-against-an-ocean-waterline) |
| Extruded polygon footprint | [Scenario 4](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md#scenario-4--non-rectangular-floor-plan-extruded-polygon) |
| Baked complex interior | [Scenario 5](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md#scenario-5--sloped-or-uneven-interior-baked-data) |
| Rigidbody flood mass | [Scenario 6](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md#scenario-6--flood-mass-affecting-a-vessel-rigidbody) |
| Flow visuals and audio | [Scenario 7](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md#scenario-7--flow-visuals-and-audio) |
| Scene-view diagnostics | [Scenario 8](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md#scenario-8--scene-view-diagnostics-while-tuning) |

## Build your own compartment

1. Create a `Flood System` GameObject and attach
   `FloodSimulationManager`.
2. Create a child compartment GameObject, attach `FloodVolume`, and select
   **Rectangular Prism**, **Extruded Polygon**, or **Baked Data**.
3. Create a child water-visual GameObject with the built-in components required
   by the renderer, then attach the matching package renderer:
   - rectangle: `FloodCubeSurfaceRenderer` with a child cube Transform,
   - polygon: `FloodPolygonSurfaceRenderer` with a child Mesh Filter and Mesh
     Renderer,
   - baked data: `FloodBakedSurfaceRenderer` with a child Mesh Filter and Mesh
     Renderer.
4. Assign a transparent material to the child's Mesh Renderer. The included
   material requires URP; other pipelines need their own material.
5. Create a source GameObject, attach `FloodSource`, assign the same manager and
   target volume, set **Flow Rate** in cubic meters per second, and enable
   **Active**.
6. Enter Play Mode.

Rectangular and polygon modes require no bake. Baked Data mode additionally
requires `FloodVolumeAuthoring`, one readable closed manifold source mesh, and
a current `FloodVolumeData` asset produced in the Editor.

For connections, exterior boundaries, mass integration, diagnostics, every
Inspector field, and troubleshooting, follow the
[Unity Editor workflow](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md).

## Known limitations

- Compartments use a rectangle or one simple polygon footprint with flat floor
  and vertical walls, or an approximate baked representation. Polygon holes and
  self-intersections are unsupported.
- Baked capacity and centroid describe the retained samples, not the exact
  source mesh. Features below the selected resolution can disappear. The
  Inspector reports sample count, actual sample resolution, and an approximation
  volume indicator; that indicator is not a certified error bound.
- Baked `ContainsPoint` / `QueryPoint.IsInsideVolume` use occupied-cell
  approximation (`FloodContainmentPrecision.BakeApproximation`), not exact
  source-mesh winding.
- Baked arbitrary-plane queries scale linearly with occupied cell count. Use
  the coarsest resolution that preserves gameplay-relevant features.
- Simulation defaults to 10 fixed ticks per game second.
- Gravity-aligned surfaces represent instantaneous equilibrium; transient
  slosh, surge, delayed settling, and oscillation are not simulated.
- Stable registration order and inputs preserve tick/reconciliation ordering
  within one runtime. Cross-platform bit-identical floating-point and Rigidbody
  behavior are not guaranteed.
- Near-zero gravity retains the last valid compartment-local surface
  orientation because no unique settled plane exists.
- Connection pressure heads follow solved surfaces, but submerged opening area
  remains a rectangular-height approximation when an opening tilts.
- There is no automatic overflow edge or pipe-loss model beyond the configured
  discharge coefficient. Exterior exchange uses `ExternalFluidBoundary` plus
  `FloodConnection`; it is not arbitrary water generation.
- The optional Rigidbody adapter changes mass and center of mass only. It does
  not implement buoyancy, hydrodynamics, or vessel stability.
- `FloodDiagnostics` draws only while its GameObject is selected and reads
  current public state without advancing simulation or writing Rigidbody data.

## Samples

In **Window > Package Management > Package Manager**, select **Flooding**, open
**Samples**, and import the sample you want:

All imported scenes contain persistent, authored hierarchies. Cameras,
lights, demonstration objects, component references, and local material assets
are visible and editable before Play Mode; entering Play Mode adds only
transient simulation or presentation state.

- **Flood Mass Integration** imports to
  `Assets/Samples/Flooding/0.10.0/Flood Mass Integration`. Open
  `FloodMassRollPitch.unity` there and enter Play Mode. A cutaway
  four-compartment barge renders visible water with
  `FloodCubeSurfaceRenderer`, shifts Rigidbody center of mass through
  `FloodMassAggregator` + `RigidbodyFloodMassAdapter`, and uses sample-only
  `SampleVesselSupport` springs so roll/pitch is obvious. An auto-demo and
  keyboard presets (port/starboard/bow/stern) drive asymmetric loads. Game-view
  COM markers and a HUD show dry, flood, and combined centers. This is not
  production buoyancy.
- **Baked Geometry** imports to
  `Assets/Samples/Flooding/0.10.0/Baked Geometry`. Open `BakedGeometry.unity`
  there and enter Play Mode. A curved hull-section compartment ships its
  authoring source mesh and bake asset; optional fill/roll, a HUD, and **B**
  baked-cell visualization show why Baked Data is needed versus prism/extruded
  modes. Disable **Animate Fill** or **Animate Roll** independently. The
  `FloodBakedSurfaceRenderer` free-surface mesh is generated from the solved
  gravity plane intersected with the bake's presentation-boundary mesh (voxel
  contours remain the legacy fallback) and aligned to gravity.
- **Connected Compartments** imports to
  `Assets/Samples/Flooding/0.10.0/Connected Compartments`. Open
  `ConnectedCompartments.unity` there and enter Play Mode to see conserved,
  bidirectional pressure-driven flow equalize two finite compartments. Tune
  scheduling on `FloodSimulationManager`, dimensions and initial cubic meters
  on the two `FloodVolume` components, and opening behavior on
  `FloodConnection`. `FloodConnectionVisual` drives the live flow arrow; the
  sample bootstrap only updates water cubes and the Game-view readout.
- **Hull Breach** imports to `Assets/Samples/Flooding/0.10.0/Hull Breach`. Open
  `HullBreach.unity` there and enter Play Mode to watch ocean head drive
  inflow into an empty compartment, approach equalization, reverse to outflow
  when the interior is higher, and stop when the connection is closed.
  `FloodCubeSurfaceRenderer` keeps compartment water gravity-aligned when the
  hull rotates; `HullBreachBootstrap` only updates the ocean visual and
  Game-view readout. Tune the ocean Transform waterline on
  `ExternalFluidBoundary`, compartment state on `FloodVolume`, and opening
  fields on `FloodConnection`.
- **First Person Flooding** imports to
  `Assets/Samples/Flooding/0.10.0/First Person Flooding`. Open
  `FirstPersonFlooding.unity` for a rising enclosed-room flood from first
  person with `FloodCameraTracker`, optional URP waterline/underwater effects,
  audio muffling, and telemetry. Enable the URP Depth Texture and Flood
  Underwater Renderer Feature for the fullscreen waterline pass. Press **T** to
  tilt the room; waterline follows the authoritative surface plane.
- **Local Ingress** imports to `Assets/Samples/Flooding/0.12.0/Local Ingress`.
  Open `LocalIngress.unity` (rebuild via **Flooding > Internal > Build Local
  Ingress Sample** if needed). URP showcase with turbulent jet, soft droplet /
  mist / foam impact layers, and irregular shallow spread with edge foam.
  Toggle local ingress with **I** to compare against instant bulk equilibrium
  visuals. Details: `Documentation/local-ingress.md`.

The package folders under `Samples~` are the authoritative sample sources.
Package Manager copies them into `Assets/Samples` rather than synchronizing
them. Re-importing a sample or upgrading the package can overwrite an imported
scene, material, script, or other same-named file. Duplicate or move customized
copies outside the versioned imported sample folder before re-importing or
upgrading.

See:

- [Unity Editor workflow](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md)
  for getting started, scenario setups, scripting, migration, testing, and
  troubleshooting.
- [Package model and technical overview](https://github.com/kylebloom/Flooding/blob/main/Packages/com.rabbidwolf.com.kyle.flooding/Documentation/index.md).
- Repository-level
  [specification, architecture, and implementation plans](https://github.com/kylebloom/Flooding/tree/main/docs)

## License

This package is licensed under the [MIT License](https://github.com/kylebloom/Flooding/blob/main/LICENSE).
  for behavior, boundaries, delivery status, and refinement verification.