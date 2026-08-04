# Unity Editor workflow

This guide describes how to use Flooding `0.9.1` in Unity
`6000.5.6f1`.

Use this document as the practical how-to. Start with
[Getting started](#getting-started), then pick a
[scenario](#choose-your-scenario). Later sections document each component field,
scripting, upgrades, and troubleshooting in detail.

## How the Unity pieces relate

This guide uses Unity terms precisely:

- A **prefab asset** is a reusable asset in the Project window. Dragging it into
  a scene creates a prefab instance made of GameObjects.
- A **GameObject** is an item in the scene Hierarchy. It always has a
  `Transform` component and can hold additional components.
- A **component** is attached to a GameObject through
  **Inspector > Add Component**. `FloodSimulationManager`, `FloodVolume`,
  `FloodSource`, `FloodConnection`, `ExternalFluidBoundary`,
  `FloodCubeSurfaceRenderer`, and `FloodConnectionVisual` are script
  components, not GameObjects.
- A **child GameObject** is nested beneath another GameObject in the Hierarchy.
  The prototype water mesh is a child GameObject.
- A **material asset** is assigned to a Renderer component. `Floodwater.mat` is
  a material asset, not a flooding component.

Inspector component names include spaces, such as **Flood Volume**. Their C#
type names omit spaces, such as `FloodVolume`.

## Getting started

Pick one path. Every path ends with Play Mode verifying that water state
changes.

### Path A — Prefab smoke test (about 60 seconds)

Best when you only need to confirm the package runs.

1. Open any scene, or create one with **File > New Scene**.
2. In the Project window, open
   `Packages/com.rabbidwolf.com.kyle.flooding/Runtime/Prefabs`.
3. Drag `Room.prefab` into the Hierarchy.
4. Enter Play Mode.

Expected result: the nested source fills the rectangular room and the
transparent water cube rises. If water is pink or invisible, your project is
not using URP for the included material—assign any transparent material to the
nested `WaterVisual` Mesh Renderer.

Use `Flooding.prefab` instead of `Room.prefab` when the scene already has walls
and floors.

### Path B — Import a working sample

Best when you want a complete authored hierarchy to inspect and edit.

1. Open **Window > Package Management > Package Manager**.
2. Select **Flooding** in the package list.
3. Open the **Samples** tab and import one sample.
4. Open the imported scene under `Assets/Samples/Flooding/0.9.1/...`.
5. Enter Play Mode, then stop and edit Inspector fields before playing again.

| Sample | What it teaches |
| --- | --- |
| Flood Mass Integration | Aggregate water mass moving a Rigidbody center of mass |
| Baked Geometry | Editor-baked complex interior + gravity-aligned free surface |
| Connected Compartments | Conserved doorway flow between two finite rooms |
| Hull Breach | Ocean waterline exchanging water with one compartment |

Re-importing a sample can overwrite your copy under `Assets/Samples`. Duplicate
customized copies first.

### Path C — Build your first compartment from components

Best when you are wiring Flooding into your own level.

1. Create an empty GameObject named `Flood System`.
2. **Add Component > Flood Simulation Manager**.
3. Create a child GameObject named `Compartment`.
4. On `Compartment`, **Add Component > Flood Volume**.
5. Set **Geometry Mode** to **Rectangular Prism**, **Width** `5`, **Length**
   `5`, **Maximum Height** `3`, **Initial Volume** `0`.
6. Create a child of `Compartment` named `Water Visual`.
7. On `Water Visual`, add Mesh Filter + Mesh Renderer (or use a Cube primitive
   and remove its Collider). Assign a transparent material.
8. On `Compartment`, **Add Component > Flood Cube Surface Renderer**. Assign
   **Water Visual** to that child Transform.
9. Create a sibling under `Flood System` named `Leak`.
10. On `Leak`, **Add Component > Flood Source**. Assign **Target** to the
    `FloodVolume`, set **Flow Rate** to `1`, enable **Active**.
11. Enter Play Mode and confirm water rises.

Detailed field descriptions for each step are in
[Create the manager GameObject](#create-the-manager-gameobject),
[Create a flood-volume GameObject](#create-a-flood-volume-gameobject),
[Add the rectangular renderer and visual GameObject](#add-the-rectangular-renderer-and-visual-gameobject),
and [Create a source GameObject](#create-a-source-gameobject).

## Choose your scenario

Each scenario lists the GameObjects to create, the components to attach, the
required Inspector assignments, and the expected Play Mode result. Densities
use kilograms per cubic meter; volumes use cubic meters; rates use cubic meters
per second.

### Scenario 1 — Single room filling from a leak

**Goal:** One compartment receives configured inflow (not pressure-driven).

**Hierarchy:**

```text
Flood System
  FloodSimulationManager
  Compartment
    FloodVolume
    FloodCubeSurfaceRenderer
    Water Visual
  Leak
    FloodSource
```

**Steps:**

1. Follow [Path C](#path-c--build-your-first-compartment-from-components).
2. Leave **Flood Source > Active** enabled.
3. Optionally set **Flood Volume > Initial Volume** to a non-zero start.
4. Enter Play Mode.

**Expected:** Volume increases each tick until the compartment is full.
`FloodSource` does not reverse or equalize; it injects at the authored rate
until capacity rejects further inflow.

**When to use `FloodSource`:** broken pipe, sprinkler, debug faucet, or any
gameplay-controlled injection that should ignore pressure head.

### Scenario 2 — Two rooms equalizing through a doorway

**Goal:** Conserved bidirectional flow between two finite `FloodVolume`
compartments.

**Hierarchy:**

```text
Flood System
  FloodSimulationManager
  Compartment A
    FloodVolume (Initial Volume = 6)
    FloodCubeSurfaceRenderer
    Water Visual
  Compartment B
    FloodVolume (Initial Volume = 1)
    FloodCubeSurfaceRenderer
    Water Visual
  Door Connection
    FloodConnection
    FloodConnectionVisual (optional)
```

**Steps:**

1. Create `Flood System` with `FloodSimulationManager`.
2. Create two child compartment GameObjects. On each, attach `FloodVolume`
   (rectangular), a cube renderer, and a water-visual child.
3. Give both the same density (`1000`). Give A more **Initial Volume** than B.
4. Create `Door Connection` under `Flood System`.
5. Place its Transform at the bottom center of the physical opening. Local X =
   width, local Y = up the opening, local forward = positive A→B.
6. **Add Component > Flood Connection**.
7. Assign **Side A** = compartment A `FloodVolume`, **Side B** = compartment B
   `FloodVolume`, same **Simulation Manager**.
8. Set **Opening Width**, **Opening Height**, **Discharge Coefficient**
   (try `0.62`), enable **Is Open**.
9. Optional: **Add Component > Flood Connection Visual** and assign a child
   arrow Transform to **Flow Indicator**.
10. Enter Play Mode.

**Expected:** Water flows from higher head to lower head. Total water in A+B
stays constant. Flow approaches zero as levels equalize. Closing **Is Open**
stops transfer immediately.

**Sample shortcut:** import **Connected Compartments**.

### Scenario 3 — Hull breach against an ocean waterline

**Goal:** Pressure-driven exchange with an infinite exterior boundary.

**Hierarchy:**

```text
Flood System
  FloodSimulationManager
  External Ocean
    ExternalFluidBoundary   (Inspector: External Fluid Body)
  Breached Compartment
    FloodVolume
    FloodCubeSurfaceRenderer
    Water Visual
  Hull Breach
    FloodConnection
    FloodConnectionVisual (optional)
```

**Steps:**

1. Create `Flood System` with `FloodSimulationManager`.
2. Create `External Ocean`. Place its Transform so **position** is on the
   waterline and **up** opposes gravity (usually world up).
3. **Add Component > External Fluid Body**. Set **Density** to match the
   compartment (`1000`). Enable **Boundary Enabled**.
4. Create a rectangular `FloodVolume` compartment partially below that
   waterline. Start with **Initial Volume** `0`.
5. Create `Hull Breach` at the opening bottom center.
6. **Add Component > Flood Connection**. Assign **Side A** = External Fluid
   Body, **Side B** = compartment `FloodVolume`. Matching density is required.
7. Set opening size, enable **Is Open**, enter Play Mode.

**Expected:**

| Action | Result |
| --- | --- |
| Breach below ocean surface, empty room | Inflow into the compartment |
| Interior level near ocean waterline | Flow approaches zero |
| Raise interior above ocean (or lower ocean) | Outflow to exterior |
| Disable **Is Open** or **Boundary Enabled** | No transfer |

**Do not use `FloodSource` for this.** Sources ignore pressure equilibrium.
Exterior exchange must use `ExternalFluidBoundary` + `FloodConnection`.

**Sample shortcut:** import **Hull Breach**.

### Scenario 4 — Non-rectangular floor plan (extruded polygon)

**Goal:** Custom footprint with vertical walls and flat floor.

**Steps:**

1. Create manager + compartment as in Scenario 1.
2. On `FloodVolume`, set **Geometry Mode** to **Extruded Polygon**.
3. Expand **Polygon Footprint** and enter at least three local XZ points in
   perimeter order (Unity shows the second coordinate as `Y` on `Vector2`).
4. With the compartment selected, drag Scene-view handles to match the floor.
5. Fix any red validation message (duplicates, zero area, self-intersections).
6. Replace `FloodCubeSurfaceRenderer` with `FloodPolygonSurfaceRenderer`.
7. Give the water child a Mesh Filter and Mesh Renderer with a transparent
   material.
8. Enter Play Mode with a source or initial volume.

**Expected:** Capacity equals footprint area × **Maximum Height**. Water mesh
follows the polygon and clips to the gravity-aligned surface.

### Scenario 5 — Sloped or uneven interior (baked data)

**Goal:** Complex closed mesh interior without runtime mesh analysis.

**Steps:**

1. Create the compartment GameObject and attach `FloodVolume`.
2. On the same GameObject, **Add Component > Flood Volume Authoring**.
3. Create a child with a readable, closed, manifold Mesh Filter. Assign it to
   **Source Mesh Filter**. Assign **Target Volume**.
4. Choose **Cell Resolution** and **Maximum Grid Cells**, then click
   **Bake Closed Mesh To Flood Volume Data** and save the asset.
5. Confirm `FloodVolume` switched to **Baked Data** and references the asset.
6. **Add Component > Flood Baked Surface Renderer** and a child Mesh
   Filter/Renderer for the free-surface patches.
7. Enter Play Mode.

**Expected:** Runtime never reads the source mesh. Re-bake after moving the
source relative to the volume, changing the mesh, or changing resolution.
Stale bakes are flagged in the authoring Inspector.

**Sample shortcut:** import **Baked Geometry**.

### Scenario 6 — Flood mass affecting a vessel Rigidbody

**Goal:** Report water mass and center of mass to a Rigidbody without the
package applying buoyancy forces.

**Hierarchy:**

```text
Vessel (Rigidbody)
  RigidbodyFloodMassAdapter
  FloodMassAggregator
  FloodSimulationManager (or shared parent manager)
  Compartment A / B / ...
    FloodVolume
    FloodCubeSurfaceRenderer (optional presentation)
    Water Visual
```

**Steps:**

1. Put a `Rigidbody` on the vessel root.
2. **Add Component > Flood Mass Aggregator** so it can see child volumes (or
   assign contributors explicitly).
3. **Add Component > Rigidbody Flood Mass Adapter**. Set **Dry Mass** and
   **Dry Center Of Mass Local** for the empty vessel.
4. Ensure child `FloodVolume` components share one manager and have water.
5. Optionally attach `FloodCubeSurfaceRenderer` so flood water is visible while
   the adapter owns mass/COM only.
6. Enter Play Mode and move water between asymmetric compartments.

**Expected:** Rigidbody mass and center of mass update from dry baseline +
water. The vessel does not automatically float or right itself—add your own
buoyancy if needed. Presentation components observe `FloodState`; they do not
cause the COM shift.

**Sample shortcut:** import **Flood Mass Integration** (cutaway barge with
visible water, COM markers, presets, and sample-only `SampleVesselSupport`).

### Scenario 7 — Flow visuals and audio

**Goal:** Optional presentation driven by measured diagnostics.

**Connection visual:**

1. Select the connection GameObject.
2. **Add Component > Flood Connection Visual**.
3. Assign **Connection**.
4. Assign any combination of **Flow Indicator** (Transform), **Flow
   Particles** (ParticleSystem), and **Flow Mesh** (MeshRenderer).
5. Tune **Low/High Flow Threshold** in m³/s.

**Audio:**

1. On the same opening GameObject (or a child at the opening),
   **Add Component > Flood Connection Audio** (adds `AudioSource`).
2. Assign a looping water clip to the AudioSource or to **Flow Clip**.
3. Set Spatial Blend near `1` for 3D sound at the breach/door.
4. For a leak: use **Flood Source Audio** on the source GameObject.
5. For room ambience: use **Flood Volume Audio** on the compartment.

**Expected:** Visuals/audio respond to applied flow or fill. Disabling these
components must not change water volume or tick metrics. The package does not
ship clips or particle assets.

### Scenario 8 — Scene-view diagnostics while tuning

**Goal:** Read-only overlay for surfaces, gravity, flow, and centers of mass.

1. On `Flood System`, **Add Component > Flood Diagnostics**.
2. Enable **Discover Children**, or assign volumes/connections explicitly.
3. Enable the overlays you need (**Show Surface Planes**, **Show
   Connections**, and so on).
4. Keep the diagnostics GameObject selected and the Scene view visible in Play
   Mode.

**Expected:** Labels and gizmos update from public state. Diagnostics never
advance a tick or write Rigidbody/simulation data.

## Quick references

### Prefab chooser

- Use
  `Packages/com.rabbidwolf.com.kyle.flooding/Runtime/Prefabs/Room.prefab` for the
  fastest playable result. It includes floor and wall geometry plus a nested
  configured `Flooding.prefab` instance.
- Use
  `Packages/com.rabbidwolf.com.kyle.flooding/Runtime/Prefabs/Flooding.prefab` when
  your scene already has room geometry. It contains the manager, rectangular
  flood volume, active source, and water visual but no surrounding room.
- Build from components when you need polygon, baked, connected, exterior, or
  vessel-specific setups. See [Choose your scenario](#choose-your-scenario).

### Renderer chooser

- **Rectangular Prism**: `FloodCubeSurfaceRenderer` and a child cube Transform.
- **Extruded Polygon**: `FloodPolygonSurfaceRenderer` and a child GameObject
  with Mesh Filter and Mesh Renderer.
- **Baked Data**: `FloodBakedSurfaceRenderer` and a child GameObject with Mesh
  Filter and Mesh Renderer.

The core simulation does not depend on a render pipeline. The included
`Materials/Floodwater.mat` requires URP; use your own transparent material with
Built-in, HDRP, or a custom pipeline.

### Feature → scenario map

| Feature | Start here |
| --- | --- |
| Prefab room + leak | [Path A](#path-a--prefab-smoke-test-about-60-seconds) / [Scenario 1](#scenario-1--single-room-filling-from-a-leak) |
| Doorway between rooms | [Scenario 2](#scenario-2--two-rooms-equalizing-through-a-doorway) |
| Ocean / lake breach | [Scenario 3](#scenario-3--hull-breach-against-an-ocean-waterline) |
| Custom floor outline | [Scenario 4](#scenario-4--non-rectangular-floor-plan-extruded-polygon) |
| Sloped / curved interior | [Scenario 5](#scenario-5--sloped-or-uneven-interior-baked-data) |
| Rigidbody mass response | [Scenario 6](#scenario-6--flood-mass-affecting-a-vessel-rigidbody) |
| Flow VFX / SFX | [Scenario 7](#scenario-7--flow-visuals-and-audio) |
| Debug overlays | [Scenario 8](#scenario-8--scene-view-diagnostics-while-tuning) |

### Sample import

In **Window > Package Management > Package Manager**, select **Flooding**, open
**Samples**, and import one or more of these samples:

All four imported scenes expose their useful GameObjects, components,
references, cameras, lights, and local material assets before Play Mode. Play
Mode starts simulation and transient presentation updates; it does not
regenerate the authored hierarchy.

- **Flood Mass Integration**: Unity copies it to
  `Assets/Samples/Flooding/0.9.1/Flood Mass Integration`. Open
  `FloodMassRollPitch.unity` there and enter Play Mode. A cutaway
  four-compartment barge renders gravity-aligned water with
  `FloodCubeSurfaceRenderer`, aggregates flood mass into a Rigidbody COM, and
  uses sample-only `SampleVesselSupport` springs for visible roll/pitch. An
  auto-demo plus keyboard presets show port/starboard/bow/stern loads; Game-view
  markers and a HUD display dry/flood/combined COM. On **Flood Mass Demo
  Vessel**, tune **Dry Mass** and **Dry Center Of Mass Local** on
  `RigidbodyFloodMassAdapter`, support response on `SampleVesselSupport`, and
  compartment geometry on each child `FloodVolume`.
- **Baked Geometry**: Unity copies it to
  `Assets/Samples/Flooding/0.9.1/Baked Geometry`. Open `BakedGeometry.unity`
  there and enter Play Mode. The sample shows a closed elliptical bowl /
  hull-section interior (curved horizontal waterlines) with its authored source
  mesh, `FloodVolumeAuthoring`, and `HullSectionFloodVolumeData`.
  `BakedGeometrySampleBootstrap` supplies optional fill/roll, a Game-view HUD,
  **Space** pause, **B** baked-cell toggle, and **R** roll toggle. Clear
  **Animate Fill** or **Animate Roll** independently. `FloodBakedSurfaceRenderer`
  generates the free-surface mesh from the solved gravity plane intersected with
  the bake's presentation-boundary mesh when present (voxel-cell contours remain
  the format-1 fallback). Runtime does not analyze a live source mesh. HUD lines
  separate voxel simulation geometry from surface-boundary presentation.
- **Connected Compartments**: Unity copies it to
  `Assets/Samples/Flooding/0.9.1/Connected Compartments`. Open
  `ConnectedCompartments.unity` there and enter Play Mode. Water moves from the
  initially higher-head compartment toward the lower-head compartment while
  total finite water volume is conserved. Tune scheduling on the root
  `FloodSimulationManager`, dimensions and initial cubic meters on the two
  `FloodVolume` components, and opening dimensions, discharge, and open state
  on `FloodConnection`. `FloodConnectionVisual` drives the live flow arrow.
  `ConnectedCompartmentsBootstrap` only updates water cubes and the Game-view
  readout.
- **Hull Breach**: Unity copies it to
  `Assets/Samples/Flooding/0.9.1/Hull Breach`. Open `HullBreach.unity` there
  and enter Play Mode. An `ExternalFluidBoundary` waterline exchanges water
  with one finite compartment through a `FloodConnection`.
  `FloodCubeSurfaceRenderer` presents gravity-aligned compartment water;
  `HullBreachBootstrap` only updates the ocean visual and Game-view readout.
  Move the ocean Transform on world Y, close the connection, raise interior
  water, or rotate the compartment to see inflow, equalization, outflow,
  closure, and gravity-aligned surfaces.

The package's `Samples~` folders are authoritative. Package Manager import
creates a writable copy under `Assets/Samples` but does not synchronize that
copy with the package source. Re-importing a sample or upgrading the package can
overwrite edited scenes, scripts, materials, and other same-named imported
files. Before either operation, duplicate or move each customized copy outside
its versioned `Assets/Samples/Flooding/0.9.1/<Sample Name>` folder, or preserve
it in version control.

### Baked Data minimum checklist

1. Put `FloodVolume` and `FloodVolumeAuthoring` on the compartment GameObject.
2. Assign **Target Volume** and a child **Source Mesh Filter** whose mesh is
   readable, closed, manifold, and non-degenerate.
3. Choose **Cell Resolution** and **Maximum Grid Cells**, then bake.
4. Assign `FloodBakedSurfaceRenderer` and a child Mesh Filter/Mesh Renderer.
5. Confirm **Baked Data**, **Baked Volume Data**, **Water Mesh Filter**, and a
   transparent material are assigned before Play Mode.

### Connection minimum checklist

1. Put both endpoints and the `FloodConnection` under one enabled
   `FloodSimulationManager`, or explicitly assign that same manager.
2. Assign different `IFluidBoundary` endpoints to **Side A** and **Side B**
   (`FloodVolume` or **External Fluid Body**). Densities must match.
3. Place the connection Transform at the opening's bottom center.
4. Set positive **Opening Width**, **Opening Height**, and **Discharge
   Coefficient**, then enable **Is Open**.
5. Enter Play Mode to inspect requested and applied flow.

### Exterior boundary minimum checklist

1. Create an **External Fluid Body** GameObject. Position = waterline point;
   up = surface normal (oppose gravity for open water).
2. Match **Density** to connected `FloodVolume` densities.
3. Connect with `FloodConnection` (**Side A**/**Side B** = exterior + volume).
4. Do not connect two exteriors to each other in this version.

## Recommended scene hierarchy

One manager GameObject can coordinate several compartment, source, connection,
exterior, and presentation components:

```text
Flood System                         GameObject
├── FloodSimulationManager           component on Flood System
├── FloodDiagnostics                 optional component on Flood System
├── Compartment A                    GameObject
│   ├── FloodVolume                  component on Compartment A
│   ├── FloodCubeSurfaceRenderer     component on Compartment A
│   ├── FloodVolumeAudio             optional component on Compartment A
│   └── Water Visual                 child GameObject
│       ├── Transform                built-in component
│       ├── Mesh Filter              built-in component
│       └── Mesh Renderer            built-in component using Floodwater.mat
├── Compartment B                    GameObject
│   ├── FloodVolume                  component on Compartment B
│   ├── FloodCubeSurfaceRenderer     component on Compartment B
│   └── Water Visual                 child GameObject
├── External Ocean                   GameObject
│   └── ExternalFluidBoundary        component (External Fluid Body)
├── Leak                             GameObject
│   ├── FloodSource                  component on Leak
│   └── FloodSourceAudio             optional component on Leak
└── Door Connection                  GameObject
    ├── FloodConnection              component on Door Connection
    ├── FloodConnectionVisual        optional component on Door Connection
    └── FloodConnectionAudio         optional component on Door Connection
```

This is a conceptual hierarchy. Unity's Hierarchy window displays only the
GameObject rows; select a GameObject to see the listed components in its
Inspector. The names on the left are examples. Component arrangement and
Inspector references determine behavior.

## Try the included prefab

1. In the Project window, open
   `Packages/com.rabbidwolf.com.kyle.flooding/Runtime/Prefabs`.
2. Drag the `Room.prefab` asset into the active scene. This creates a `Room`
   prefab-instance GameObject in the Hierarchy.
3. Expand the instance and select its nested `Flooding` GameObject.
4. In the Inspector, observe the `FloodSimulationManager`, `FloodVolume`, and
   `FloodCubeSurfaceRenderer` components attached to that GameObject.
5. Select the child `Leak` GameObject and observe its `FloodSource` component.
6. Select the child `WaterVisual` GameObject and observe its Transform, Mesh
   Filter, and Mesh Renderer components.
7. Enter Play Mode and confirm the transparent water cube rises while the
   source component is active.

The included setup is a functional rectangular-room example. It does not
require baking or generated data. `Room.prefab` provides the visible floor and
walls around a nested `Flooding.prefab` instance. Drag `Flooding.prefab` alone
when you want the configured simulation without that environment shell.

## Create the manager GameObject

Every actively simulated hierarchy requires a GameObject with a
`FloodSimulationManager` component.

1. In the Hierarchy, select **Create > Empty** and name the new GameObject
   `Flood System`.
2. Select `Flood System`, then choose
   **Inspector > Add Component > Flood Simulation Manager**. This attaches the
   `FloodSimulationManager` script component; it does not create another
   GameObject.
3. Configure:
   - **Ticks Per Second**: fixed flooding updates per game second. The default
     is `10`.
   - **Maximum Ticks Per Frame**: catch-up limit that prevents a long frame from
     creating an unbounded simulation backlog.
   - **Simulate Automatically**: advances from scaled game time when enabled.
   - **Gravity Mode**: **Physics Gravity** uses global `Physics.gravity`;
     **Custom** uses this manager's vector.
   - **Custom Gravity**: world-space acceleration in meters per second squared.
     It is used only in **Custom** mode. `(0, -9.81, 0)` matches normal Earth
     gravity.
4. Parent GameObjects containing `FloodVolume`, `FloodSource`,
   `FloodConnection`, or `ExternalFluidBoundary` components beneath
   `Flood System`. Alternatively, assign the `FloodSimulationManager`
   component explicitly in each **Simulation Manager** field.

When **Simulation Manager** is empty, a flooding component looks for the nearest
parent GameObject that has a `FloodSimulationManager` component. A source,
connection, exterior boundary, and all referenced volumes must use the same
manager component.

### Gravity and rotated compartments

The water surface normal points opposite the manager's active gravity vector.
With normal Unity gravity, it remains horizontal in world space while a parent
ship, room, or vehicle rotates. Stored water volume does not change merely
because a Transform rotates.

If active gravity magnitude falls below `0.00001 m/s²`, there is no physically
unique settled surface. Each volume retains its last valid compartment-local
surface orientation. A volume created while gravity is already near zero starts
with a local-Y surface. Restoring non-zero gravity immediately supplies a new
gravity-aligned target on the next simulation tick.

Use one gravity policy per manager. Volumes and connections under different
managers may intentionally use different custom gravity vectors.

Runtime code can change that policy:

```csharp
simulationManager.GravityMode = FloodGravityMode.Custom;
simulationManager.CustomGravity = new Vector3(0f, -3.71f, 0f);
```

`FloodVolume.SurfacePlane` and `FloodState.SurfacePlane` are world-space.
`FloodVolume.LocalSurfacePlane` is available for local geometry and debugging.
`SurfaceVolumeError` and `SurfaceSolveIterations` expose solver diagnostics.

## Create a flood-volume GameObject

1. In the Hierarchy, create an empty child GameObject beneath `Flood System`.
2. Name the child for the compartment, such as
   `Engine Room Flood Volume`.
3. Position this GameObject's Transform origin at the center of the compartment
   floor.
4. Keep the GameObject's local Y axis along the authored prism walls, from floor
   toward ceiling. Water surface orientation comes from manager gravity, not
   this axis.
5. With the compartment GameObject selected, choose
   **Inspector > Add Component > Flood Volume**. This attaches the
   `FloodVolume` script component to the selected GameObject.
6. Configure the `FloodVolume` component:
   - **Simulation Manager**: manager that advances and publishes this volume.
     Leave empty to use the nearest parent manager.
   - **Geometry Mode**: choose **Rectangular Prism** for a centered box,
     **Extruded Polygon** for a custom floor outline, or **Baked Data** for a
     previously baked asset.
   - **Width** and **Length**: rectangular dimensions in meters. These appear
     only in **Rectangular Prism** mode.
   - **Polygon Footprint**: ordered local XZ perimeter points. This appears only
     in **Extruded Polygon** mode.
   - **Baked Volume Data**: immutable `FloodVolumeData` asset. This appears only
     in **Baked Data** mode.
   - **Maximum Height**: maximum fill height in meters. This appears only in
     rectangular and polygon modes; baked capacity and bounds come from the
     asset.
   - **Water Density**: kilograms per cubic meter. Fresh water normally uses
     `1000`.
   - **Initial Volume**: starting water in cubic meters.

With the compartment selected and the Scene view **Gizmos** control enabled,
the selected-object gizmo shows the simulated walls. Rectangular capacity is:

```text
width × length × maximum height
```

Initial volume is clamped to that capacity when Play Mode begins.

### Author an extruded polygon

1. Set **Geometry Mode** to **Extruded Polygon**.
2. Expand **Polygon Footprint**. Each element is one local `(X, Z)` perimeter
   point; Unity displays the second coordinate as `Y` because `Vector2` uses
   `X/Y`, but the package maps it to local Z.
3. Enter at least three points in perimeter order. Either clockwise or
   counter-clockwise order is accepted and normalized internally.
4. With the compartment GameObject selected, drag the numbered Scene-view
   handles to match the floor outline. Handles remain on local Y zero.
5. Read the validation message at the bottom of the component. A valid outline
   reports capacity. Invalid outlines identify duplicate points, zero area, or
   crossing edge indices.
6. Use **Reset To 5 m Rectangle** to recover a valid four-point outline.

Concave outlines are supported. One component represents one perimeter; holes,
disconnected islands, and self-intersecting outlines are not supported.
Polygon capacity is footprint area multiplied by **Maximum Height**.

Do not add or repeat the first point at the end of the list. Unity closes the
last edge back to point `0` automatically.

### Bake a complex three-dimensional compartment

Use Baked Data for sloped floors, curved hull interiors, or uneven ceilings.
The source is one closed triangle mesh; the runtime representation is an
immutable union of axis-aligned cells in the `FloodVolume` GameObject's local
space.

1. Create or select the compartment GameObject that contains the `FloodVolume`
   component.
2. Add the `FloodVolumeAuthoring` script component to that same GameObject.
   This is an authoring component, not the baked asset.
3. Create or select a source child GameObject with built-in **Mesh Filter**.
   Assign its Mesh Filter component to **Source Mesh Filter**. Its mesh asset
   must be readable, closed, manifold, and made from non-degenerate triangles.
4. Assign the compartment's `FloodVolume` component to **Target Volume**.
5. Set **Cell Resolution** in meters. This is a requested maximum cell edge;
   the baker divides each bounds axis evenly, so **Actual Sample Resolution** may be
   slightly smaller.
6. Set **Maximum Grid Cells** as a bake safety limit. This limits inspected
   cells, including empty cells, before any asset is written.
7. Click **Bake Closed Mesh To Flood Volume Data**. On the first successful
   bake, choose an asset path in the Project window and save the new
   `FloodVolumeData` asset. Later bakes update that assigned asset in place and
   do not ask for another path.
8. Confirm the target `FloodVolume` now uses **Baked Data** and references that
   asset. Keep **Visualize Bake** enabled to inspect retained samples while the
   authoring GameObject is selected.

Cell centers determine occupancy. Baked capacity equals occupied cell count
multiplied by actual cell volume; baked centroid and all runtime plane queries
use that same cell union. The result is deterministic and exact for the baked
union, not for the source mesh. Features thinner than a cell can disappear.
The Inspector's **Approximation Indicator** is the implementation's
resolution-dependent approximation volume. It helps compare resolutions but is
not a certified upper error bound because sub-resolution features may not be
sampled.

The bake is marked stale when the source mesh dependency, source-to-volume
Transform, or requested resolution changes. Re-bake before Play Mode. Open,
non-manifold, unreadable, degenerate, empty, and over-limit sources fail with an
actionable message and do not overwrite the previous successful data.
Self-intersecting meshes are unsupported and are not exhaustively detected;
repair them in the modeling tool before baking.

`FloodVolumeData` is immutable gameplay input. Runtime code may select a
different previously baked asset through `ConfigureBakedGeometry`, but cannot
modify or regenerate a bake. Player code never reads source mesh vertices.

```csharp
// Both assets were created previously by FloodVolumeAuthoring in the Editor.
public FloodVolumeData damagedHullBake;

void ApplyDamagedHull(FloodVolume compartment)
{
    compartment.ConfigureBakedGeometry(damagedHullBake);
}
```

For presentation:

1. Create a child GameObject named `Water Surface`.
2. Add built-in **Mesh Filter** and **Mesh Renderer** components to that child.
3. Assign a transparent material asset to its Mesh Renderer.
4. Add `FloodBakedSurfaceRenderer` to the compartment GameObject.
5. Assign the child Mesh Filter to **Water Mesh Filter** and the compartment
   `FloodVolume` to **Source Volume**.
6. Set **Minimum Visible Volume** in cubic meters. This renderer uses a volume
   threshold, unlike the cube and polygon renderers' height threshold.

This focused renderer draws only free-surface patches clipped per occupied
cell. It does not reconstruct curved source walls, and patch seams may be
visible with materials that render internal edges. It intentionally hides the
free-surface mesh when volume is at or below **Minimum Visible Volume** and when
the compartment is within the solver's absolute volume tolerance of full,
because a distinct interior free surface does not exist at full capacity.

## Add the rectangular renderer and visual GameObject

1. Select the compartment GameObject that already has the `FloodVolume`
   component.
2. Choose **Inspector > Add Component > Flood Cube Surface Renderer**. This
   attaches `FloodCubeSurfaceRenderer` to the same GameObject.
3. In the Hierarchy, create **3D Object > Cube** as a child of the compartment
   GameObject and name this child GameObject `Water Visual`.
4. Reset the `Water Visual` GameObject's local position and rotation.
5. Remove or disable the Cube Collider component unless gameplay requires it.
6. On the child GameObject's Mesh Renderer component, assign the
   `Materials/Floodwater.mat` material asset, or another transparent material.
7. Return to the parent compartment GameObject. On its
   `FloodCubeSurfaceRenderer` component, drag the child `Water Visual`
   GameObject's Transform into **Water Visual**.
8. Confirm **Source Volume** references the `FloodVolume` component on the
   parent compartment GameObject.
9. Set **Interpolation Duration** in seconds. Use `0` for immediate updates.
10. Set **Minimum Visible Height** if very shallow water should remain hidden.

When the assigned child has its normal Cube Mesh Filter, the renderer replaces
that instance's mesh at runtime with closed geometry clipped to the interpolated
gravity-aligned plane. A child without a Mesh Filter retains the old local-Y
transform-scaling fallback. Presentation never changes simulation volume.

`FloodCubeSurfaceRenderer` works only with **Rectangular Prism** geometry and
hides its child visual if the source changes to **Extruded Polygon**.

### Add the polygon renderer

1. Select the compartment GameObject containing `FloodVolume`.
2. Choose **Inspector > Add Component > Flood Polygon Surface Renderer**.
3. In the Hierarchy, create an empty child GameObject named
   `Polygon Water Visual`.
4. Add built-in **Mesh Filter** and **Mesh Renderer** components to that child
   GameObject. Do not add `FloodVolume` to the child.
5. Assign `Materials/Floodwater.mat` to the child Mesh Renderer.
6. On the parent GameObject's `FloodPolygonSurfaceRenderer`, drag the child's
   Mesh Filter component into **Water Mesh Filter**.
7. Confirm **Source Volume** references the parent `FloodVolume`.
8. Configure **Interpolation Duration** and **Minimum Visible Height**.

The script component creates a transient closed water mesh in Play Mode from
the normalized footprint and interpolated gravity-aligned plane. It does not
create a mesh asset in the Project window. The same renderer also works with
rectangular geometry.

`FloodSurfaceRenderer` is an abstract C# component base class. It does not
appear as an attachable component in **Add Component**. Future concrete script
components can inherit it and consume the same state without changing flooding
logic.

To create another presentation, inherit the contract and implement
`ApplyState`:

```csharp
using Kyle.Flooding;
using UnityEngine;

public sealed class FloodDebugRenderer : FloodSurfaceRenderer
{
    protected override void ApplyState(FloodState state)
    {
        Debug.Log($"Displayed water height: {state.Height:F2} m");
    }
}
```

The base component handles source subscription, initial state, and
interpolation. A concrete renderer should only update presentation objects.
After saving the script, attach **Flood Debug Renderer** to a GameObject through
**Add Component**, then assign its inherited **Source Volume** field. Call
`SnapToCurrentState()` when an immediate visual refresh is required.

## Create a source GameObject

1. In the Hierarchy, create an empty GameObject beneath `Flood System`.
2. Name the GameObject for the inflow, such as `Engine Room Leak`.
3. Select that GameObject and choose
   **Inspector > Add Component > Flood Source**. This attaches the
   `FloodSource` script component to the source GameObject.
4. On the `FloodSource` component, confirm **Simulation Manager** references the
   same `FloodSimulationManager` component as the target volume.
5. Drag the target compartment GameObject's `FloodVolume` component into
   **Target**. Dragging the GameObject is also accepted when Unity can resolve
   the required component.
6. Set **Flow Rate** in cubic meters per second.
7. Enable **Active**.
8. Enter Play Mode and confirm the target's current volume and visual height
   increase.

`FloodSource` is an infinite configured injection in this version. It does not
model opening pressure, exterior water level, or conservation with another
compartment. For pressure-driven ocean or reservoir exchange, use
**External Fluid Body** (`ExternalFluidBoundary`) with a `FloodConnection`
instead.

In gameplay code, read or set `FloodSource.IsActive`. **Active** is the
serialized Inspector label; the public API property is named `IsActive`.

`FloodSource` no longer mutates its target from its own `Update()`. It submits a
request during each manager tick, allowing all requests to be calculated before
any destination is changed.

## Create a connection GameObject

Connections support rotated rectangular or polygon-prism compartments and
optional infinite exterior endpoints. The opening itself remains rectangular.

1. Confirm both endpoints belong to the same `FloodSimulationManager`.
2. For an exterior breach, create an empty GameObject for the ocean or
   reservoir, attach **External Fluid Body** (`ExternalFluidBoundary`), and
   place its Transform so position is on the waterline and up opposes gravity.
3. In the Hierarchy, create an empty GameObject beneath the `Flood System`
   manager GameObject for the opening.
4. Name the new GameObject for the opening, such as
   `Engine Room Door Connection` or `Hull Breach`.
5. Position this connection GameObject's Transform at the bottom center of the
   physical opening.
6. Keep local Y vertical. Local X represents opening width, and local forward
   represents positive flow from side A toward side B.
7. With the connection GameObject selected, choose
   **Inspector > Add Component > Flood Connection**. This attaches the
   `FloodConnection` script component.
8. Configure the `FloodConnection` component:
   - **Simulation Manager**: the shared `FloodSimulationManager` component.
   - **Side A** and **Side B**: each must reference a `FloodVolume` or
     **External Fluid Body**. Densities must match. Connecting two external
     bodies is unsupported.
   - **Opening Width**: horizontal opening width in meters.
   - **Opening Height**: vertical opening height in meters above the Transform.
   - **Discharge Coefficient**: dimensionless restriction from `0` to `1`.
     `0.62` is a useful initial doorway approximation.
   - **Is Open**: whether the connection currently permits flow.
9. Select the connection GameObject, enable the Scene view **Gizmos** control,
   and verify its wireframe opening gizmo matches the intended doorway, vent,
   breach, or overflow opening.
10. Enter Play Mode with different water levels or waterlines and confirm flow
    moves from greater pressure head to lower pressure head.

Pressure head follows each gravity-aligned surface. Opening-bottom depth sets
submerged area; orifice head is evaluated at the submerged-portion centroid.
Differences within `1e-6 m` produce no flow. Absolute opening area does not
change when the opening Transform rotates; moving or rotating the exterior
waterline does change depth and therefore flow.

Use `FloodSource` only for configured injection that should ignore pressure
equilibrium. Use `ExternalFluidBoundary` + `FloodConnection` for oceans,
lakes, and reservoirs.

The simplified flow model is:

```text
Q = Cd × A × √(2 × g × |headA - headB|)
```

where heads are centroid depths derived from opening-bottom submersion.

The manager limits requested flow by available finite source volume and
finite destination capacity. Multiple openings sharing one finite compartment
are scaled proportionally. Infinite exterior endpoints skip supply or capacity
scaling for the infinite side only. After each tick, read
`FloodSimulationManager.LastTickMetrics` for internal transfer, external
inflow/outflow, configured source volume, and conservation residual.

### Read or control a connection from code

```csharp
using Kyle.Flooding;
using UnityEngine;

public sealed class WatertightDoor : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Flood connection controlled by this door.")]
    private FloodConnection floodConnection;

    public void SetDoorOpen(bool open)
    {
        floodConnection.IsOpen = open;
    }

    public double CurrentFlowRate => floodConnection.CurrentFlowRate;

    public Vector3 FlowDirection => floodConnection.FlowDirectionWorld;
}
```

`RequestedFlowRate` reports unconstrained hydraulic demand.
`CurrentFlowRate` reports the flow actually applied after source and destination
limits. `SubmergedOpeningArea` is in square meters, and
`PressureHeadDifference` is in meters.

### Optional connection visual and audio

Attach presentation consumers to the same opening GameObject or a child:

1. **Inspector > Add Component > Flood Connection Visual**.
2. Assign **Connection** to the `FloodConnection` component.
3. Optionally assign:
   - **Flow Indicator**: a Transform arrow or mesh to orient and scale,
   - **Flow Particles**: a ParticleSystem whose emission scales with flow,
   - **Flow Mesh**: a MeshRenderer enabled only while flowing.
4. Tune **Low Flow Threshold** and **High Flow Threshold** in cubic meters per
   second for intensity banding.
5. For sound, add **Flood Connection Audio** with an `AudioSource`, assign a
   looping water clip, and keep Spatial Blend near 1 for 3D placement at the
   opening.

Likewise:

- **Flood Source Audio** on a `FloodSource` GameObject for configured injection
  sound.
- **Flood Volume Audio** on a compartment for fill-driven ambience.

These components only read public diagnostics. Disabling them cannot change
simulation volume, flow, or tick metrics. The package does not ship audio clips
or particle assets; assign project content in the Inspector.

## Advance simulation manually

Disable **Simulate Automatically** when another system, replay, or test controls
simulation time:

```csharp
using Kyle.Flooding;
using UnityEngine;

public sealed class ManualFloodDriver : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Flooding manager advanced by this external driver.")]
    private FloodSimulationManager simulationManager;

    public void AdvanceBy(float elapsedSeconds)
    {
        simulationManager.Advance(elapsedSeconds);
    }

    public void ExecuteOneTick(float tickDuration)
    {
        simulationManager.SimulateTick(tickDuration);
    }
}
```

`Advance` uses the configured tick rate and catch-up limit. `SimulateTick`
executes exactly one tick with the supplied duration.

## Read state from gameplay code

`CurrentState` returns an immutable snapshot. It is safe for consumers to keep
the snapshot without gaining mutation access to the compartment.

```csharp
using Kyle.Flooding;
using UnityEngine;

public sealed class FloodStateDisplay : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Flood volume observed by this display.")]
    private FloodVolume floodVolume;

    private void OnEnable()
    {
        if (floodVolume != null)
            floodVolume.StateChanged += HandleStateChanged;
    }

    private void Start()
    {
        if (floodVolume != null)
            HandleStateChanged(floodVolume.CurrentState);
    }

    private void OnDisable()
    {
        if (floodVolume != null)
            floodVolume.StateChanged -= HandleStateChanged;
    }

    private static void HandleStateChanged(FloodState state)
    {
        Debug.Log(
            $"Flood volume: {state.Volume:F2} / {state.Capacity:F2} m³, "
            + $"mass: {state.WaterMass:F0} kg");
    }
}
```

Read `CurrentState` once during initialization because subscribing does not
immediately emit an initial event.

## Integrate flood mass with a Rigidbody

Use this optional layer only when one component can own the Rigidbody's mass
and center-of-mass properties.

1. Select the vessel root GameObject that has the built-in **Rigidbody**
   component.
2. Add the package script component **Flood Mass Aggregator** to that same
   vessel root.
3. Make each compartment GameObject containing a package **Flood Volume**
   component a child of the vessel root. Enable **Include Inactive** only when
   disabled compartments should still contribute.
4. Add the package script component **Rigidbody Flood Mass Adapter** to the
   vessel root.
5. Assign the vessel root's **Flood Mass Aggregator** component to **Flood
   Mass**.
6. Set **Dry Mass** in kilograms to the vessel mass without flood water.
7. Set **Dry Center Of Mass Local** in Rigidbody-local meters to the dry
   vessel's center of mass.

During Play Mode, the adapter writes:

```text
Rigidbody mass = dry mass + aggregate flood mass
Rigidbody center of mass = weighted dry and flood center
```

Disabling the adapter restores its configured dry values. Do not let another
component write `Rigidbody.mass` or `Rigidbody.centerOfMass` while it is
enabled. The adapter does not provide buoyancy or restoring forces.

To inspect a working setup, import **Flood Mass Integration** from **Window >
Package Management > Package Manager > Flooding > Samples**, then open
`Assets/Samples/Flooding/0.9.1/Flood Mass Integration/FloodMassRollPitch.unity`
and enter Play Mode. Its `SampleVesselSupport` component is sample-only
spring scaffolding, not production buoyancy or vessel stability. The sample
renders visible compartment water and Game-view COM markers so the chain from
flood location → COM shift → roll/pitch is obvious.

Runtime-created volumes can configure density before adding water:

```csharp
floodVolume.ConfigureFluidDensity(1025f); // seawater, kg/m³
```

## Inspect flooding diagnostics in the Scene view

Use the optional package **Flood Diagnostics** component for read-only
presentation and debugging. It does not advance a manager tick, alter a
connection, change water volume, or write Rigidbody mass properties.

1. Select the vessel root GameObject or the shared manager root GameObject.
2. Choose **Inspector > Add Component > Flood Diagnostics**. Attach one
   diagnostic component to the root; do not add one to every compartment.
3. Leave **Discover Children** enabled when the root contains the relevant
   **Flood Volume** and **Flood Connection** components. Child discovery
   includes inactive GameObjects.
4. If the hierarchy is not shared, disable **Discover Children** and assign
   every observed **Flood Volume** to **Volumes** and every observed **Flood
   Connection** to **Connections**.
5. Assign **Simulation Manager** when it is not below the diagnostic root. This
   supplies active gravity in meters per second squared.
6. Assign **Mass Adapter** and **Target Rigidbody** when they are not below the
   root. The adapter supplies configured dry mass and dry local COM; the
   built-in Rigidbody supplies its current combined mass and world COM.
7. Set marker radius, gravity-arrow length, surface-plane size, and flow-arrow
   length in world-space meters. Configure visibility and colors as needed.
8. Select the root GameObject containing **Flood Diagnostics**. The Scene view
   displays:
   - aggregate water COM and mass in kilograms,
   - configured dry COM and mass in kilograms,
   - current combined Rigidbody COM and mass in kilograms,
   - active gravity direction and magnitude in meters per second squared,
   - each solved surface plane and volume/capacity in cubic meters,
   - each connection's flow direction, signed head in meters, and signed
     requested/applied rates in cubic meters per second.
9. Enter Play Mode to inspect live values. A requested rate with a smaller or
   zero applied rate indicates source-availability or destination-capacity
   reconciliation.

Connection arrows use the applied-flow sign when flow was applied and fall back
to the requested-flow sign when reconciliation reduced the applied rate to
zero. Removing or disabling **Flood Diagnostics** has no simulation or physics
effect. The visualization is Editor-only; player builds retain only the small
snapshot component and contain no `Handles` or label code.

## Change volume from gameplay code

Mutation methods report how much of a request could be applied:

```csharp
VolumeChangeResult result = floodVolume.AddWater(12f);

Debug.Log(
    $"Requested {result.RequestedChange:F2} m³, "
    + $"applied {result.AppliedChange:F2} m³, "
    + $"rejected {result.RejectedVolume:F2} m³");
```

Use:

- `AddWater` for an immediate addition.
- `RemoveWater` for an immediate removal.
- `AddWaterOverTime` for a rate applied over a time interval.
- `RemoveWaterOverTime` for a removal rate applied over a time interval.

Positive signed changes add volume. Negative signed changes remove volume.
Rejected volume is always an unsigned magnitude.

## Configure geometry from gameplay code

Runtime-created compartments can switch between the two Phase 5 geometry modes:

```csharp
floodVolume.ConfigurePolygonGeometry(
    new[]
    {
        new Vector2(0f, 0f),
        new Vector2(4f, 0f),
        new Vector2(4f, 2f),
        new Vector2(1f, 2f),
        new Vector2(1f, 5f),
        new Vector2(0f, 5f),
    },
    newMaximumHeight: 3f);
```

`ConfigureRectangularGeometry` accepts width, length, and maximum height.
Configuration validates before replacing the current geometry. Existing water
volume is preserved when possible and clamped if the new capacity is smaller.
Use these operations between manager ticks rather than from a state-change
callback.

## Choose an event

- `StateChanged` responds to any published state change, including transform
  movement that changes world-space surface or center-of-mass data.
- `VolumeChanged` responds only to water-volume changes.
- `WaterHeightChanged` remains available for the prototype renderer.

The manager publishes after all changes for a fixed tick have committed. Do not
depend on event callbacks occurring immediately inside a direct mutation call.

## Upgrade from initial height

Scenes and prefabs authored before `0.2.0` stored `Initial Water Height`.
`FloodVolume` migrates that value to cubic meters using the configured floor
area:

```text
initial volume = width × length × legacy initial height
```

After upgrading:

1. Allow Unity to finish script compilation and asset import.
2. Select important flood-volume prefabs and confirm **Initial Volume**.
3. Save migrated scenes and prefabs.
4. Run the package Edit Mode and Play Mode tests.

No manual conversion should be performed before Unity imports the old
serialized value.

## Upgrade from FloodWaterVisual

`FloodWaterVisual` remains as a hidden compatibility component so scenes from
before `0.3.0` continue to load. New authoring should use
`FloodCubeSurfaceRenderer`.

To migrate an existing scene manually:

1. Select the GameObject containing the old `FloodWaterVisual` component.
2. Record its existing **Flood Volume**, **Water Visual**, and
   **Minimum Visible Height** assignments.
3. Remove the `FloodWaterVisual` component from that GameObject.
4. Attach a `FloodCubeSurfaceRenderer` component to the same GameObject through
   **Add Component**.
5. Restore the recorded assignments.
6. Choose an **Interpolation Duration**.
7. Enter Play Mode and verify the visible water reaches the same final height.

The package's included prefabs are already migrated.

## Upgrade to fixed-step simulation

Scenes authored before `0.4.0` might not contain a simulation manager.

1. Select or create the common parent GameObject for each related set of
   compartments, sources, and connections.
2. Attach a `FloodSimulationManager` component to that parent GameObject.
3. Confirm the **Simulation Manager** assignment on every `FloodVolume` and
   `FloodSource` component.
4. Confirm each source component uses the same manager component as its target
   volume component.
5. Start with **Ticks Per Second** set to `10`.
6. Enter Play Mode and verify source flow and presentation.

Without a manager, direct volume methods still change stored volume, but sources
do not advance and state-change events are not published on a fixed tick.

## Add connections to an existing scene

Upgrading to `0.5.0` does not infer neighboring compartments or create
connections automatically. Create one connection GameObject and attach one
`FloodConnection` component for each doorway, vent, pipe, or overflow opening
that should transfer water. Existing source-only scenes continue to work
without connection GameObjects.

There is no separate connection-manager component to add or migrate.
`FloodSimulationManager` discovers and reconciles `FloodConnection` components.
If an earlier prototype or local integration used a connection manager, remove
that component, move each connection under the shared flood-system hierarchy
or assign **Simulation Manager** explicitly, and verify both endpoint volumes
use that same manager.

## Upgrade to reusable geometry

Upgrading to `0.6.0` preserves existing `FloodVolume` components as
**Rectangular Prism** because that enum value is the serialized default. Width,
length, maximum height, initial volume, cube renderer assignments, and capacity
remain unchanged.

To convert an existing compartment:

1. Record its current capacity and initial volume.
2. Change **Geometry Mode** to **Extruded Polygon**.
3. Edit the footprint and resolve any red validation message.
4. Replace `FloodCubeSurfaceRenderer` with
   `FloodPolygonSurfaceRenderer` and configure the child Mesh Filter.
5. Confirm the new reported capacity and adjust **Initial Volume** if needed.
6. Enter Play Mode and verify source and connection behavior.

## Upgrade to gravity-aligned surfaces

Version `0.7.0` defaults existing managers to **Physics Gravity**. Existing
volume, geometry, source, connection, and renderer references remain serialized.

1. Select each `FloodSimulationManager` and confirm **Gravity Mode**.
2. Keep **Physics Gravity** unless this flooding hierarchy intentionally uses a
   different acceleration.
3. Confirm each rectangular water child still has its Mesh Filter. The cube
   renderer uses that component for clipped tilted geometry.
4. Reset child water visual local position, rotation, and scale if it was
   manually offset.
5. Rotate a compartment in Play Mode and verify its surface remains
   perpendicular to gravity without changing current volume.
6. Recheck connection flow at large tilt angles because opening-area
   calculation remains approximate.

## Upgrade a prism compartment to baked data

Version `0.9.0` appends baked data to `FloodGeometryMode`, so existing
serialized rectangle (`0`) and polygon (`1`) values are unchanged.

1. Keep the existing `FloodVolume` and record current volume and capacity.
2. Add `FloodVolumeAuthoring`, assign the closed source Mesh Filter, and bake.
3. Replace the prism renderer with `FloodBakedSurfaceRenderer`.
4. Compare baked capacity with the previous capacity. The baker assigns the new
   asset and clamps current or initial volume only if it exceeds that capacity.
5. Recheck opening placement and pressure behavior because sloped geometry can
   change the solved surface and water center of mass.

## Migrate pre-1.0 baked-geometry source

The pre-`1.0.0` API intentionally makes a clean source break while preserving
serialized assets and scenes:

1. Replace `FloodGeometryMode.BakedCells` with
   `FloodGeometryMode.BakedData`. Both use serialized integer `2`, so existing
   scene and prefab values load without manual reassignment. No source alias is
   retained.
2. Remove construction, type checks, or casts involving
   `BakedCellFloodGeometry`; that concrete runtime implementation is no longer
   public. Configure a `FloodVolume` with `ConfigureBakedGeometry(data)`, then
   consume `FloodVolumeData`, `IFloodVolumeGeometry`, or `FloodState`.
3. Replace public asset diagnostics `CellCount`, `CellSize`, and
   `EstimatedBoundaryVolume` with `SampleCount`, `SampleResolution`, and
   `EstimatedApproximationVolume`.
4. Remove gameplay use of grid dimensions, occupied indices, boundary sample
   counts, and sample-center helpers. Those serialized format details are now
   internal to the runtime, Editor, and tests.
5. Existing usable `FloodVolumeData` assets do not require rebaking solely for
   this rename. Re-bake only when normal stale-data diagnostics require it.

## Run package tests

1. Open **Window > General > Test Runner**.
2. Select **EditMode** and run the `Kyle.Flooding.Tests.Editor` assembly.
3. Select **PlayMode** and run the `Kyle.Flooding.Tests.PlayMode` assembly.
4. Confirm the example room still rises visually in normal Play Mode.

Edit Mode tests validate deterministic volume, flow, polygon validation,
triangulation, arbitrary-plane clipping, solver tolerance, capacity, and
centroid and diagnostic-derivation rules. Play Mode tests validate GameObject
lifecycle, gravity policy, rotation, state snapshots, fixed-step orchestration,
repeated-tick bounds and internal-network conservation, transfer
reconciliation, diagnostic read-only behavior, clipped rendering, and
post-commit publication.

Baked-geometry Edit Mode coverage also exercises immutable data, arbitrary-plane
cell clipping, open-mesh rejection, and a deterministic 512-cell solver guard.

## Troubleshooting

### Water does not rise

- Confirm a `FloodSimulationManager` is enabled.
- Confirm the volume and source reference the same manager.
- Confirm **Simulate Automatically** is enabled, or manually advance the
  manager.
- Confirm **Active** is enabled in the Inspector or `FloodSource.IsActive` is
  `true` in code.
- Confirm its **Target** references the intended `FloodVolume`.
- Confirm **Flow Rate** is greater than zero.
- Confirm the compartment is not already at capacity.

### Simulation changes but water is invisible

- For rectangular presentation, confirm
  `FloodCubeSurfaceRenderer.Water Visual` references the child Transform.
- For polygon presentation, confirm
  `FloodPolygonSurfaceRenderer.Water Mesh Filter` references the child Mesh
  Filter and the child also has a Mesh Renderer.
- Confirm the renderer type matches the selected geometry mode.
- Confirm **Source Volume** references the intended `FloodVolume`.
- Confirm the child has an enabled Mesh Renderer and transparent material.
- For cube or polygon presentation, confirm current height exceeds **Minimum
  Visible Height**.
- For baked presentation, confirm current volume exceeds **Minimum Visible
  Volume** and is not effectively full. The baked renderer hides its
  free-surface-only mesh at near-full capacity.

### Polygon geometry shows a red validation message

- Do not repeat the first point as the last list element.
- Ensure every point is unique and at least `0.000001 m` from every other point.
- Reorder points so the perimeter proceeds continuously without crossing an
  earlier edge.
- Ensure the outline encloses at least `0.00000001 m²`.
- Use **Reset To 5 m Rectangle**, then move or add points incrementally while
  watching the validation message.

### Baked geometry is missing, stale, or fails to bake

- Assign both **Target Volume** and a Mesh Filter with a shared mesh.
- Enable **Read/Write** in the source model's import settings.
- Repair holes, non-manifold edges, and degenerate triangles in the modeling
  tool; the baker requires every undirected triangle edge exactly twice.
- Increase **Cell Resolution** if the grid exceeds **Maximum Grid Cells**.
- Decrease **Cell Resolution** if thin features contain no occupied centers.
- Re-bake after changing the mesh, the source-to-volume Transform, or requested
  resolution. A prior successful asset is not overwritten when validation
  fails.

### Water does not flow through a connection

- Confirm **Is Open** is enabled.
- Confirm **Side A** and **Side B** resolve to different active boundaries with
  matching density.
- Confirm the connection and both endpoints reference the same manager.
- Confirm the Transform is at the opening bottom rather than its center or top.
- Confirm at least one water surface is above the opening bottom.
- Confirm **Opening Width**, **Opening Height**, and
  **Discharge Coefficient** are greater than zero.
- Compare **Requested Flow Rate** with **Current Flow Rate**. A non-zero request
  with zero applied flow indicates an empty finite source or full finite
  destination.
- Read the connection Inspector help box / `ValidationMessage` for density
  mismatch, unresolved endpoints, or unsupported exterior↔exterior pairing.

### Exterior breach does not exchange water

- Confirm the ocean GameObject uses **External Fluid Body**, not `FloodSource`.
- Confirm **Boundary Enabled** is on and the component is active.
- Confirm ocean **Density** matches the compartment within tolerance.
- Confirm the ocean Transform waterline is above the opening bottom for inflow
  (or below interior head for outflow).
- Confirm `FloodSimulationManager.LastTickMetrics` shows external inflow or
  outflow after a tick.

### Connection visual or audio does nothing

- Confirm the presentation component is enabled and references the correct
  connection, source, or volume.
- For visuals, assign at least one of **Flow Indicator**, **Flow Particles**,
  or **Flow Mesh**.
- For audio, assign a looping clip to the `AudioSource` (or the component clip
  field). Missing clips remain silent by design.
- Confirm measured flow or fill is above the component's idle/silent thresholds.
- Disable the presentation component and re-run a tick: water volume must be
  unchanged, proving simulation is independent of presentation.

### Initial water amount looks different after upgrading

- Initial state is now displayed in cubic meters rather than meters of height.
- Check that width and length are correct before migration.
- For a rectangular compartment, divide volume by `width × length` to recover
  the equivalent height.

### Surface does not align with gravity when the room rotates

- Confirm the volume references the intended manager.
- Confirm that manager's **Gravity Mode** and **Custom Gravity**.
- Confirm gravity is not near zero; near-zero gravity intentionally retains the
  last valid compartment-local orientation.
- For `FloodCubeSurfaceRenderer`, confirm the child has a Mesh Filter. A
  meshless child uses the legacy local-Y scaling fallback.
- For `FloodPolygonSurfaceRenderer`, confirm **Water Mesh Filter** references
  the intended child.

### Flood diagnostics are missing or incomplete

- Select the GameObject containing **Flood Diagnostics**; its handles and
  labels draw only while that component is selected.
- Confirm the Scene view **Gizmos** control is enabled.
- Keep **Discover Children** enabled only when the observed volumes and
  connections are descendants of the diagnostic root.
- Otherwise disable discovery and populate **Volumes**, **Connections**,
  **Simulation Manager**, **Mass Adapter**, and **Target Rigidbody** explicitly.
- A water COM marker is omitted when all observed compartments contain zero
  kilograms of water.
- Connection requested/applied flow values are meaningful only in Play Mode
  after the manager has simulated a tick. In Edit Mode, the opening, endpoint,
  and static geometry aids can still draw, but live flow diagnostics remain
  zero or retain no runtime calculation.

