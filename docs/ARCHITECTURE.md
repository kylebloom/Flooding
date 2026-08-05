# Flooding Package Architecture

## Purpose

This document defines responsibility boundaries and extension points for the
Unity flooding package. Observable behavior belongs in `SPEC.md`; delivery
status belongs in `IMPLEMENTATION_PLAN.md`.

## Assembly structure

### `Kyle.Flooding.Runtime`

Contains:

- deterministic simulation rules,
- immutable public state and mutation results,
- Unity scene adapters,
- pipeline-agnostic presentation components (`FloodCameraTracker`,
  `FloodUnderwaterProfile`, surface renderers, audio helpers).

The pure simulation classes must not depend on scene objects. Unity-facing
state may use Unity value types such as `Plane` and `Vector3`. This assembly
must not reference URP.

### `Kyle.Flooding.URP`

Optional Universal Render Pipeline presentation assembly:

- `FloodUnderwaterRendererFeature` / `FloodUnderwaterPass`
- `FloodUnderwaterCameraEffect`
- fullscreen underwater shader/material consumers

Depends on `Kyle.Flooding.Runtime` and
`Unity.RenderPipelines.Universal.Runtime`. The asmdef uses
`versionDefines` + `defineConstraints` (`KYLE_FLOODING_URP` when
`com.unity.render-pipelines.universal` ≥ 17) so the assembly is excluded when
URP is not installed. `Kyle.Flooding.Editor` and core Play Mode tests must not
hard-reference this assembly; optional URP Play Mode tests live in
`Tests/PlayMode.URP`. Camera effects read tracker/profile state only and never
mutate simulation. The underwater pass traces camera rays against
`SurfacePlane` (optical-path fog) and does not yet mask to volume bounds.

### `Kyle.Flooding.Editor`

Contains conditional Inspectors, validation presentation, Scene-view authoring
handles, source-mesh validation, and complex-geometry baking. Runtime assemblies
do not reference it.

### `Kyle.Flooding.Tests.Editor`

Contains deterministic Edit Mode tests for simulation rules and value
contracts.

### `Kyle.Flooding.Tests.PlayMode`

Contains only tests that require GameObjects, MonoBehaviour lifecycle, frame
execution, transforms, or physics.

## State ownership

`FloodSimulation` owns authoritative compartment volume and capacity clamping.
It has no dependency on GameObjects or presentation.

`FloodVolume` owns the `FloodSimulation` instance for one scene compartment. It
also owns an immutable `IFloodVolumeGeometry` and converts simulation, geometry,
and transform data into an immutable `FloodState`.

It caches the latest `FloodSurfaceSolution` by authoritative volume and local
surface normal. World transform changes reuse local clipped geometry when
possible while still producing updated world-space state.

Consumers receive snapshots. They must not mutate simulation internals.

Gameplay point queries (`ContainsPoint`, `IsPointSubmerged`, `QueryPoint`) are
read-only compositions over the same live authoritative volume, geometry
containment, and cached surface plane. They never enter the manager tick path
and never publish events. `FloodQueryResult.SurfaceSignedDistanceMeters` exposes
the plane distance sign convention (positive above, negative below) without
changing `SubmersionDepthMeters`. Containment for analytic prisms is exact;
baked geometry containment uses occupancy cells and exposes
`FloodContainmentPrecision.BakeApproximation` on the geometry contract.

## Mutation contract

Every core volume mutation returns `VolumeChangeResult`. The result uses signed
requested and applied changes:

- positive values represent addition,
- negative values represent removal,
- zero represents no change.

Rejected volume is an unsigned magnitude. It allows manager reconciliation to
enforce capacity and source availability without inferring loss from
floating-point state differences.

## Fixed-step orchestration

```text
Capture registered FloodState snapshots
        ↓
Evaluate FloodSource and FloodConnection requests
        ↓
Scale connection outflow by snapshot source availability
        ↓
Scale all inflow by snapshot destination capacity
        ↓
Build and commit one signed delta per volume
        ↓
Publish volume states
        ↓
Publish manager tick completion
```

`FloodSimulationManager` owns this phase order. `FloodSource` does not mutate
its target from `Update`, and `FloodVolume` does not poll for changes from
`LateUpdate`.

The manager also owns the gravity policy shared by its volumes and connections:
global `Physics.gravity` or one custom world-space vector. Near-zero fallback
orientation remains per volume because it depends on that volume's last valid
local surface direction.

Volumes, sources, and connections register with an explicitly assigned manager
or their nearest parent manager. Registration lists preserve registration order
within one runtime. Stable registration order, inputs, and tick durations
produce deterministic phase and reconciliation ordering. Cross-platform
bitwise floating-point identity and deterministic Unity Rigidbody behavior are
not architectural guarantees.

Connection requests are scaled proportionally first by finite source volume and
then by destination capacity. Destination capacity uses the tick-start
snapshot; outgoing transfers do not free same-tick incoming capacity. This
avoids cyclic dependency solving while preserving conservation and preventing
overdraw or overfill.

The manager uses `Update` only as a lightweight elapsed-time scheduler. All
simulation work runs in discrete ticks. A configurable per-frame catch-up limit
discards and counts excess whole ticks to prevent a spiral of death.

## Connection boundary

`FloodFlowCalculator` is the pure deterministic hydraulic rule. It receives two
opening-bottom pressure heads, authored opening dimensions, discharge
coefficient, gravity magnitude, and an open-fraction aperture multiplier;
derives submerged area, scales it by open fraction, evaluates centroid heads,
and returns signed unconstrained flow diagnostics.

`FloodConnection` is the scene adapter. It:

- resolves two `IFluidBoundary` endpoints through `FluidBoundaryReference`,
- reads pressure heads from manager-captured boundary snapshots,
- exposes runtime `IsOpen` (hard gate) and `OpenFraction` (effective aperture),
- identifies source and destination from flow sign,
- reports a requested transfer to the manager,
- receives the manager-constrained applied rate after reconciliation,
- never mutates either endpoint.

Supported endpoints are `FloodVolume` and `ExternalFluidBoundary`. Two external
boundaries cannot connect to each other in this phase. Densities must match.

### Fluid-boundary seam

```text
Finite FloodVolume ─┐
                    ├─► immutable boundary snapshots
ExternalFluidBoundary ─┘         │
                                  ▼
                         FloodConnection request
                                  │
                                  ▼
                      manager reconciliation/commit
```

`FloodConnection` does not special-case ocean inflow. Finite endpoints
participate in source and destination scaling. Infinite endpoints bypass
depletion or capacity scaling only for the capability explicitly declared
infinite. Commit routing uses the manager registration table; snapshots carry
immutable facts only.

## Geometry boundary

`IFloodVolumeGeometry` is responsible for:

- capacity,
- submerged volume beneath a plane,
- submerged centroid,
- local bounds,
- surface intersection data.

`ExtrudedFloodVolumeGeometry` implements shared flat-floor, vertical-wall
arbitrary-plane queries. `RectangularPrismFloodGeometry` supplies a centered
four-point footprint. `ExtrudedPolygonFloodGeometry` validates, normalizes, and
triangulates one simple concave or convex footprint. Geometry instances are
immutable after construction.

`FloodExtrudedGeometryQueries` decomposes each triangulated prism into
tetrahedra, clips them against a candidate plane, and accumulates exact volume
and centroid. It independently clips boundary faces and stitches their segments
into surface contours. `FloodSurfaceSolver` bounds plane offset from projected
local bounds and performs deterministic binary search to the documented volume
or position tolerance.

`FloodVolumeAuthoring` holds source references and settings but performs no mesh
analysis itself. `FloodVolumeBaker` is Editor-only: it validates one closed
manifold source mesh, transforms it into volume-local space, center-samples a
bounded grid, and writes immutable `FloodVolumeData`. A source/settings
fingerprint drives missing and stale diagnostics.

An internal baked-geometry implementation consumes only `FloodVolumeData`. Its
serialized representation and clipping strategy remain runtime implementation
details behind `IFloodVolumeGeometry`. Capacity and arbitrary-plane queries are
exact for that retained representation, so runtime query accuracy and
source-mesh approximation error remain separate concerns. Runtime never depends
on the authoring Mesh Filter.

`FloodPlaneUtility` transforms planes with inverse-transpose normals. This keeps
the negative submerged half-space consistent through transform rotation and
scale.

`FloodState` describes the result, not the geometry implementation. Renderers
that need actual surface topology read `IExtrudedFloodVolumeGeometry` from
their source volume while remaining presentation-only. Future baked geometry
implements the same base contract.

## Presentation boundary

Presentation reads `FloodState` or compatibility events and never writes
simulation state.

`FloodSurfaceRenderer` owns state subscription and interpolation. Concrete
renderers implement only the operation that applies a displayed immutable
state to presentation objects.

`FloodCubeSurfaceRenderer` is the rectangular compatibility implementation. If
its child has a Mesh Filter it generates an exact clipped mesh; otherwise it
retains level-fill transform scaling for legacy and test objects.

`FloodPolygonSurfaceRenderer` consumes normalized footprint and solved plane
data to generate a closed clipped child mesh. It supports either current
extruded geometry mode and owns only its transient runtime mesh.

`FloodBakedSurfaceRenderer` is the focused complex-geometry path. It requests
surface intersections through `IFloodVolumeGeometry`, triangulates the returned
free-surface contours, and does not reconstruct source walls. This preserves
the base presentation contract without depending on a concrete baked format.

`FloodWaterVisual` is a compatibility shim for serialized scenes authored
before version `0.3.0`; it inherits the cube renderer and is hidden from the
Add Component menu.

Optional flow and fill presentation lives in focused consumers outside the
surface-renderer contract:

- `FloodConnectionVisual` reads connection diagnostics and drives authored
  Transform, ParticleSystem, and MeshRenderer targets.
- `FloodConnectionAudio`, `FloodSourceAudio`, and `FloodVolumeAudio` drive
  `AudioSource` volume and pitch from applied flow, configured source rate, or
  fill percentage.
- `FloodCameraTracker` relates a viewpoint to registered or explicit volumes
  using read-only `QueryPoint` / `RegisteredVolumes` data, sticky active-volume
  selection, and underwater hysteresis. Auto-discover manager lookup retries
  while unresolved and after scene loads. It raises presentation events only
  and never mutates simulation.
- `FloodUnderwaterProfile` is a shareable ScriptableObject of underwater
  presentation settings (tint, fog, grading, distortion, transitions). It holds
  no runtime state and does not reference URP.
- `FloodUnderwaterAudio` drives exposed `AudioMixer` parameters from tracker
  underwater state.
- `FloodVolumeTelemetry` / `FloodCameraTelemetry` expose framework-neutral
  values for UI; they do not depend on TextMeshPro.
- Local ingress presentation (`FloodIngressSample`, `FloodIngressSampler`,
  `FloodIngressPresentationState`, `FloodLocalIngressPresenter`, optional
  `FloodIngressStreamPresenter`) reads applied connection/source flow and
  presentation anchors only. It never writes volume, and does not replace
  `FloodSurfaceRenderer`. Patches are a visual proxy with Growing / Settling /
  Converging phases; gameplay queries remain solver-based.
- Optional `IngressAnchor` on `FloodConnection` / `FloodSource` is
  presentation-only. Connection fallback position is `OpeningCenterWorld`.

`FloodSimulationManager.RegisteredVolumes` exposes a live read-only view of
registered volumes for presentation discovery. Membership remains
manager-owned through internal register/unregister paths.

These components poll public diagnostics in `LateUpdate`, never register as
simulation participants, and must not mutate flooding state. Missing clips or
unassigned visual targets fail soft. Overlapping flood volumes are ambiguous
for camera selection and are not physically merged.

## Physics boundary

`FloodVolume` implements `IMassContributor`. `FloodMassAggregator` combines
child compartment contributions without changing them. The optional
`RigidbodyFloodMassAdapter` owns an explicit dry-body baseline and writes the
resulting composite mass and local center of mass during physics steps.

Simulation remains independent of the adapter. Disabling the adapter restores
its dry baseline and does not alter any flood volume. Flooding does not apply
forces, calculate buoyancy or stability, or directly move a vessel. The package
sample uses a separate sample-only spring support solely to demonstrate the
physical response to an off-center mass.

## Serialization

Current geometry mode, rectangular dimensions, polygon footprint, maximum
height, optional baked-data reference, and initial volume are serialized on
`FloodVolume`. `BakedData = 2` follows existing rectangle and polygon enum
values, preserving all serialized integers, including baked scenes previously
written as value `2`. No source-level alias is retained. Legacy initial-height
data is stored temporarily only for migration.

`FloodVolumeData` is an asset-level immutable runtime input. Only the Editor
baker and test assemblies can initialize its private serialized payload.
Gameplay may select another valid pre-baked asset but cannot alter or rebuild
one. Its public diagnostics are representation-neutral: format version,
usability, bounds, capacity, centroid, sample count/resolution, and estimated
approximation volume. Grid dimensions, occupied indices, concrete cell size,
boundary sample counts, and sample-center helpers remain internal.

Migration rules:

1. Read the legacy height.
2. Clamp it to the configured maximum height.
3. Multiply by floor area.
4. Store and clamp the resulting initial volume.
5. Mark legacy data as migrated.

Runtime state is not written back into authoring fields.

## Dependency direction

```text
Presentation ───────► FloodState + read-only geometry / queries
URP underwater ─────► FloodCameraTracker + FloodUnderwaterProfile
Scene components ───► Simulation + geometry
Simulation manager ─► scene components + immutable snapshots
Connections ────────► simulation manager request/commit phases
Editor tooling ─────► scene authoring components
Optional physics ───► Mass-contribution interface
Simulation ─────────► no presentation, URP, or vessel physics
Kyle.Flooding.Runtime ─✗──► URP
```

Dependencies must not point from simulation into presentation, audio, effects,
or vessel-specific systems.

These boundaries are acceptance criteria for every future phase: flooding
reports state and mass, presentation renders read-only state, connections
calculate requests, the manager commits transfers, and vessel physics controls
movement.

