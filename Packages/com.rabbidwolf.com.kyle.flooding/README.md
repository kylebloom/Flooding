# Flooding

Flooding is a reusable, gameplay-focused flooding simulation package for Unity
6.5. The current `0.9.1` prototype models gravity-aligned water volume inside
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
- Aggregate child-compartment flood mass and optional owned-baseline Rigidbody
  mass and center-of-mass integration.
- Infinite exterior exchange through `ExternalFluidBoundary` (**External Fluid
  Body**) connected by `FloodConnection`.
- Exact accepted and rejected quantities for volume changes.
- Fixed-rate simulation with post-commit state publication.
- Configurable direct inflow through `FloodSource`.
- Bidirectional pressure-driven flow through `FloodConnection`.
- Simultaneous finite-volume transfer reconciliation.
- Replaceable, interpolated presentation driven by immutable state.
- Scaled-cube and generated polygon-mesh water renderers.
- Focused baked-data free-surface renderer.
- Optional `FloodConnectionVisual` and connection/source/volume audio consumers.
- Optional read-only Scene-view diagnostics for mass centers, gravity, solved
  surfaces, and connection flow.
- Basic transparent water material and example room prefabs.

## Prerequisites

- Unity Editor 6.5 (`6000.5.6f1`).
- The core simulation and geometry are render-pipeline independent.
- The included `Materials/Floodwater.mat` is a Universal Render Pipeline (URP)
  material. In Built-in, HDRP, or a custom render pipeline, assign your own
  transparent material to each water visual.

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

All four imported scenes contain persistent, authored hierarchies. Cameras,
lights, demonstration objects, component references, and local material assets
are visible and editable before Play Mode; entering Play Mode adds only
transient simulation or presentation state.

- **Flood Mass Integration** imports to
  `Assets/Samples/Flooding/0.9.1/Flood Mass Integration`. Open
  `FloodMassRollPitch.unity` there and enter Play Mode. Asymmetric compartment
  water shifts the Rigidbody center of mass and the sample-only spring support
  makes the resulting roll response visible. This sample demonstrates
  center-of-mass response; it intentionally does not render visible water and
  does not provide production buoyancy. Tune dry mass and dry center of mass on
  `RigidbodyFloodMassAdapter`, geometry and initial cubic meters on each
  `FloodVolume`, and the sample-only spring response on
  `FloodMassDemoBuoyancy`.
- **Baked Geometry** imports to
  `Assets/Samples/Flooding/0.9.1/Baked Geometry`. Open `BakedGeometry.unity`
  there and enter Play Mode. The authored retained-shape objects and material
  remain in the scene; the sample script provides only optional fill and roll
  behavior. Disable **Animate Fill** or **Animate Roll** independently. The
  `FloodBakedSurfaceRenderer` runtime free-surface mesh remains generated from
  immutable baked data and aligned to gravity.
- **Connected Compartments** imports to
  `Assets/Samples/Flooding/0.9.1/Connected Compartments`. Open
  `ConnectedCompartments.unity` there and enter Play Mode to see conserved,
  bidirectional pressure-driven flow equalize two finite compartments. Tune
  scheduling on `FloodSimulationManager`, dimensions and initial cubic meters
  on the two `FloodVolume` components, and opening behavior on
  `FloodConnection`. `FloodConnectionVisual` drives the live flow arrow; the
  sample bootstrap only updates water cubes and the Game-view readout.
- **Hull Breach** imports to `Assets/Samples/Flooding/0.9.1/Hull Breach`. Open
  `HullBreach.unity` there and enter Play Mode to watch ocean head drive
  inflow into an empty compartment, approach equalization, reverse to outflow
  when the interior is higher, and stop when the connection is closed. Tune the
  ocean Transform waterline on `ExternalFluidBoundary`, compartment state on
  `FloodVolume`, and opening fields on `FloodConnection`.

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
  [SPEC.md](https://github.com/kylebloom/Flooding/blob/main/SPEC.md),
  [ARCHITECTURE.md](https://github.com/kylebloom/Flooding/blob/main/ARCHITECTURE.md),
  [IMPLEMENTATION_PLAN.md](https://github.com/kylebloom/Flooding/blob/main/IMPLEMENTATION_PLAN.md),
  and
  [ALTERATIONS_IMPLEMENTATION_PLAN.md](https://github.com/kylebloom/Flooding/blob/main/ALTERATIONS_IMPLEMENTATION_PLAN.md)
  for behavior, boundaries, delivery status, and refinement verification.