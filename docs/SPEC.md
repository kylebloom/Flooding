# Flooding Package Specification

## Scope

This specification defines externally observable behavior for the reusable
Unity flooding package. It records supported behavior and compatibility
expectations independently from implementation details.

The package simulates gameplay-relevant bulk water behavior. It does not
simulate waves, turbulence, viscosity, free-surface deformation, or
computational fluid dynamics.

Gravity-aligned planar surfaces represent the instantaneous equilibrium state
for the current volume, geometry, transform, and gravity. They do not model
transient slosh, surge, delayed settling, or oscillation caused by vessel
acceleration or rotation.

## Version and compatibility

- Target Unity version: `6000.5.6f1`.
- Current package maturity: pre-1.0 gameplay prototype (`0.13.0` vocabulary
  complete: source / connection / sink / volume + presentation).
- Public APIs may evolve before `1.0.0`, but changes should preserve serialized
  scene and prefab data when practical. The formal compatibility policy lands
  in the 0.9x / RC milestone (see repository
  `docs/IMPLEMENTATION_PLAN.md` → Path to 1.0).
- SI units are used throughout the API.

## Units

- Distance and height: meters
- Area: square meters
- Volume: cubic meters
- Flow rate: cubic meters per second
- Mass: kilograms
- Density: kilograms per cubic meter

## Authoritative state

Water volume is the authoritative mutable state of a floodable compartment.
Height, fill percentage, surface plane, water mass, and water center of mass
are derived values.

For vertical-wall extruded geometry:

```text
capacity = polygon footprint area × maximum height
equivalent height = current volume ÷ floor area
fill percentage = current volume ÷ capacity
water mass = current volume × fluid density
```

Volume must remain within the inclusive range from zero to capacity.

## Phase 1 state contract

`FloodState` is an immutable scene-facing snapshot containing:

- current volume,
- capacity,
- current height,
- fill percentage,
- empty and full state,
- the current world-space surface plane,
- water mass,
- world-space water center of mass.

`FloodState.Height` remains the equivalent level-fill height for compatibility;
it is not generally a point on a tilted surface. `SurfacePlane` is solved from
authoritative volume with a world-space normal opposite active gravity.
`WaterCenterOfMassWorld` is the centroid of the same clipped submerged geometry.
For baked geometry, equivalent height is fill percentage multiplied by baked
local-bounds Y size; it is not a mesh floor-relative depth.

For an empty compartment, the center-of-mass position is the world-space
center of the compartment floor and the water mass is zero.
For baked geometry without one canonical floor, it is the averaged retained
boundary point nearest the empty solved plane.

## Mass contribution and Rigidbody integration

`IMassContributor` reports non-negative mass in kilograms and a world-space
center of mass. `FloodVolume` implements this contract. `FloodMassAggregator`
discovers child flood volumes and combines them using a mass-weighted
world-space center.

`RigidbodyFloodMassAdapter` is optional. It owns a configured dry-body mass and
dry Rigidbody-local center of mass, then applies the composite dry-plus-flood
mass and center each physics step. Disabling the adapter restores the configured
dry-body values. The adapter does not mutate flood state or apply forces.

Another system must not write the same Rigidbody mass or center-of-mass
properties while the adapter is enabled.

## Volume mutations

Volume-addition, removal, assignment, and simulation-step operations report a
`VolumeChangeResult`.

The result records:

- requested signed volume change,
- applied signed volume change,
- rejected unsigned volume,
- previous volume,
- resulting volume.

Positive signed changes add water. Negative signed changes remove water.
Capacity and availability can cause the applied magnitude to be less than the
requested magnitude.

Non-positive values passed to explicit add or remove operations remain no-ops
for compatibility with the prototype. Non-finite numeric input is invalid and
must be rejected with an argument exception.

## Fixed-step simulation

`FloodSimulationManager` is the authoritative runtime scheduler for managed
scene components. Its default frequency is 10 ticks per scaled game second.

Each tick performs these phases in order:

1. Capture every active registered volume state.
2. Evaluate active registered sources and connections from that shared snapshot.
3. Scale finite-volume connection outflows by source availability.
4. Aggregate external and internal inflow for each destination.
5. Scale inflow by destination capacity captured at tick start.
6. Build and commit one signed delta for every affected volume.
7. Publish changed volume states.
8. Publish tick completion.

Sources, connections, and all referenced volumes must belong to the same
manager. Components use an explicit Inspector assignment when present and
otherwise use their nearest parent manager.

Automatic advancement uses scaled `Time.deltaTime`. Manual callers may use
`Advance` to accumulate time at the configured rate or `SimulateTick` to execute
exactly one supplied duration.

Each manager selects either global `Physics.gravity` or a custom world-space
gravity vector. The same selected gravity direction drives its volume surfaces,
and its magnitude drives connection flow calculations.

Given the same initial state, registration order, inputs, and tick durations,
the manager guarantees the same tick phase order and reconciliation order
within one runtime. The package does not guarantee bit-identical
floating-point results across platforms, architectures, Unity versions, or
compiler configurations. Unity Rigidbody motion and collision response are
also outside the determinism guarantee.

The manager limits catch-up work per rendered frame. Whole ticks beyond that
limit are discarded and counted rather than creating an unbounded backlog.

## Fluid connections

`FloodConnection` represents a rectangular opening between two managed
compartments. Compartment footprints may be rectangular or polygonal and may
rotate relative to gravity. Its Transform position is the opening bottom
center. Local X defines width, local Y defines height, and local forward
indicates positive A-to-B flow direction.

For each side, pressure head is the non-negative water depth above the opening
bottom. Authored `OpeningWidth` / `OpeningHeight` define the fully-open geometry
used for opening position and submerged-height / head calculations.
`OpenFraction` ∈ [0, 1] is an effective-aperture multiplier applied after the
submerged aperture is computed. `IsOpen == false` is a hard gate that forces
zero flow regardless of fraction. `DischargeCoefficient` remains the orifice
factor and is not used as openness.

Signed unconstrained flow is:

```text
A_submerged = opening width × min(source head, opening height)
A = A_submerged × OpenFraction
Q = Cd × A × √(2 × g × |headA - headB|)
```

The sign is positive when side A has greater head and negative when side B has
greater head. `SubmergedOpeningArea` diagnostics report the effective area `A`
(after open fraction). `FullOpeningArea` is the authored rectangle;
`EffectiveOpeningArea` is that rectangle scaled by `IsOpen` and `OpenFraction`.

Pressure depth follows the solved gravity-aligned planes. Submerged opening
area still treats configured opening height as aligned with pressure depth, so
it is an approximation when the opening's local Y axis tilts away from the
gravity-opposing direction.

Multiple connection requests from one source are scaled proportionally when
their combined request exceeds source volume. All external and connection
inflows into one destination are scaled proportionally when their combined
request exceeds capacity available at tick start.

Capacity freed by outgoing flow is intentionally unavailable to incoming flow
until the next tick. This conservative rule avoids cyclic same-tick dependency
resolution.

Internal connection transfers must conserve total volume. External
`FloodSource` inflow is not volume-conserving because it represents an infinite
configured boundary.

## Generalized fluid boundaries

Connections evaluate two `IFluidBoundary` endpoints from manager-captured
immutable snapshots. Each snapshot provides:

- manager ownership,
- a world-space fluid surface plane,
- fluid density in kilograms per cubic meter,
- whether supply is finite and the available volume when it is,
- whether receiving capacity is finite and the remaining capacity when it is,
- whether the endpoint accepts committed volume deltas,
- and whether the boundary was enabled at capture time.

Infinite quantities are expressed with capability flags rather than numeric
infinity. Connections remain request calculators. They compare hydrostatic
heads at the submerged-opening centroid, choose a direction, and report a
requested transfer; the manager reconciles and commits all finite deltas
simultaneously. Infinite boundaries neither deplete nor receive a runtime
volume mutation.

For matching-density endpoints, submerged depth at a sample point is:

```text
depth = max(0, -surfacePlane.GetDistanceToPoint(samplePoint))
```

Opening-bottom depths determine submerged opening height and area. Orifice head
is evaluated at the centroid of that submerged portion. Absolute pressure-head
differences at or below `1e-6 m` produce no flow.

Connected fluids must use matching density within `0.001 kg/m³` absolute or
`1e-6` relative tolerance. Density mismatch is an authoring validation failure
and yields zero runtime transfer. Density-changing inflow and mixed fluids
remain unsupported.

Supported endpoint pairs:

- `FloodVolume ↔ FloodVolume` — finite internal transfer with conservation.
- `ExternalFluidBoundary ↔ FloodVolume` — pressure-driven exchange with an
  infinite exterior. External inflow and outflow are tracked in tick metrics
  and participate in the finite-volume accounting identity:

```text
after = before + external inflow - external outflow
      + configured sources - configured sinks
```

where configured source/sink terms are **applied** volumes after capacity or
supply scaling, not unconstrained demand.

`FloodSource` remains a configured injection path that does not model pressure
equilibrium. `FloodSink` is the inverse configured removal path: it extracts
water from a finite `FloodVolume` into nowhere (leaves the simulation), shares
finite supply with connection outflows, and does not free same-tick capacity for
inflows.

## Component events

`FloodVolume` exposes:

- `StateChanged(FloodState)` when any published state value changes,
- `VolumeChanged(double)` when volume changes,
- the existing `WaterHeightChanged(float)` compatibility event.

Events are **post-commit/publish notifications**. They fire after all changes
in one manager tick have committed. No event is emitted merely because a
listener subscribes; listeners read `CurrentState` for the initial value.

Transform movement or rotation can change the surface plane and center of mass,
so it emits `StateChanged` without emitting `VolumeChanged`.

Direct public volume mutations change stored state immediately but publish on
the next manager tick.

## Gameplay point queries

`FloodVolume` and `FloodRegion` expose read-only world-space queries:

- `ContainsPoint(Vector3 worldPoint)`
- `IsPointSubmerged(Vector3 worldPoint)`
- `QueryPoint(Vector3 worldPoint)` → `FloodQueryResult`

`FloodVolume` containment uses that volume's authored geometry.
`FloodRegion` containment uses the composite union. Prefer region queries for
gameplay that must work across every member of a composed water body.
`FloodRegion` queries lazily initialize composite geometry when the region is
active but not yet built; failed init reports outside (no silent member OR).

`FloodQueryResult` contains:

- `IsInsideVolume`
- `IsSubmerged` (inside volume and below the current surface plane)
- `SubmersionDepthMeters` (`max(0, -SurfacePlane.GetDistanceToPoint)`; zero
  when not submerged)
- `SurfaceSignedDistanceMeters` (signed distance to the same authoritative
  world-space `SurfacePlane`: `> 0` above the surface, `== 0` on the
  surface, `< 0` below the surface; reported even when outside the
  compartment)
- `SurfacePoint` (closest point on the current surface plane)
- `SurfaceNormal`

Query contract:

- Values are derived from the volume's current authoritative state at the
  moment of the call (same family as `CurrentVolume` / `CurrentState`).
- Queries never advance, reconcile, or publish simulation state.
- Direct property reads remain live; events remain published notifications.
  There is no separate published read model for queries.

Containment precision is reported by
`IFloodVolumeGeometry.ContainmentPrecision`:

- `Exact` for rectangular prism and extruded polygon footprints
- `BakeApproximation` for baked occupancy cells (resolution-dependent)

Floor-to-surface water-column depth is not part of this contract.

## Camera flood tracking (presentation)

`FloodCameraTracker` is an optional presentation consumer. It does not
participate in simulation ticks and does not mutate flood state.

- **Explicit** mode tracks one assigned `FloodVolume`.
- **Auto Discover Registered** mode reads
  `FloodSimulationManager.RegisteredVolumes` (live read-only registry view;
  registration remains manager-owned).
- Active-volume selection is sticky while the current volume still contains
  the viewpoint, independent of underwater state.
- When reselection is required and multiple registered volumes contain the
  viewpoint: prefer submerged candidates, then greatest submersion depth,
  then registration order.
- Overlapping compartments are ambiguous and are not physically merged.
- Auto-discover resolves `FloodSimulationManager` from a parent or
  `FindAnyObjectByType`, retries about twice per second while null, and
  retries again after scene loads.
- `IsUnderwater` uses configurable signed-distance hysteresis (defaults
  enter `-0.02` m, exit `+0.02` m) and is never true outside the active
  compartment. Threshold setters keep `enter <= exit`.

## URP underwater presentation

Optional `Kyle.Flooding.URP` types apply fullscreen underwater presentation:

- Consume `FloodCameraTracker` + `FloodUnderwaterProfile` only.
- The `Kyle.Flooding.URP` assembly is gated with a package `versionDefines` /
  `defineConstraints` pair (`KYLE_FLOODING_URP`) so it compiles only when
  Universal RP ≥ 17 is installed. Core runtime never references URP.
- For each pixel, reconstruct the camera ray and scene depth, intersect the ray
  with the authoritative `FloodVolume.SurfacePlane`, and tint/fog using the
  underwater optical path along that ray (works for rotated / tilted volumes;
  not a world-Y water level). Open sky / far-plane views still receive a
  camera-aware waterline.
- The surface is treated as an infinite plane for screen-space effects; effects
  are not yet clipped to FloodVolume bounds (openings may show infinite-plane
  artifacts on exterior geometry below the same plane).
- Do not mutate simulation state.

## Underwater audio and telemetry

Optional presentation consumers:

- `FloodUnderwaterAudio` smooths exposed `AudioMixer` low-pass cutoff (Hz) and
  optional volume (dB) from `FloodCameraTracker.IsUnderwater`.
- `FloodVolumeTelemetry` reports fill percentage, volume (m³), capacity (m³),
  and optional connection flow (m³/s).
- `FloodCameraTelemetry` reports camera inside/underwater flags, signed
  distance, and submersion depth.
- These types do not depend on TextMeshPro and do not mutate simulation.

## Container geometry

`IFloodVolumeGeometry` is expressed in `FloodVolume` local space and provides:

- total capacity,
- axis-aligned local bounds,
- containment precision and local-point containment,
- submerged volume in a plane's negative half-space,
- submerged centroid,
- ordered free-surface contours.

Phase 5 implements `RectangularPrismFloodGeometry` and
`ExtrudedPolygonFloodGeometry`. Both use a flat floor at local Y zero, vertical
walls, and a configured maximum local-Y height. Polygon footprints:

- contain one perimeter with at least three finite local XZ points,
- may be convex or concave,
- may use clockwise or counter-clockwise authored winding,
- are normalized to counter-clockwise winding internally,
- must have area of at least `0.00000001 m²`,
- reject points within `0.000001 m` of one another,
- reject self-intersecting edges,
- do not support holes or disconnected regions.

Geometry queries support every finite plane with a non-zero normal. Each
footprint triangle is extruded into a triangular prism, decomposed into
tetrahedra, and clipped against the plane. Submerged volume and centroid are
accumulated from the clipped polyhedra. Boundary intersections are stitched
into one or more ordered contours.

`FloodSurfaceSolver` projects local bounds onto the requested surface normal and
uses bounded binary search to solve plane position. It stops after at most `64`
iterations or when either:

- absolute or capacity-relative volume error reaches the larger of
  `0.000001 m³` and `capacity × 0.000001`, or
- the remaining plane-position interval reaches `0.000001 m`.

World/local plane transformation uses inverse-transpose normal transformation,
so rotation and non-uniform scale preserve the represented half-space.

When selected gravity magnitude is below `0.00001 m/s²`, no unique settled
surface exists. Each `FloodVolume` retains its last valid compartment-local
surface orientation and re-solves only its offset from current volume. A volume
that has never observed valid gravity falls back to local Y.

Invalid pure geometry construction throws an argument exception.
`FloodVolume` displays an actionable Inspector error while invalid and disables
itself if invalid geometry reaches Play Mode. Runtime calls to
`ConfigureRectangularGeometry` or `ConfigurePolygonGeometry` validate first,
preserve current volume, and clamp that volume to the new capacity.

### Editor-baked complex geometry

`FloodVolumeData` publicly exposes version/usability, local bounds, capacity,
full-volume centroid, generic sample count and XYZ resolution, an estimated
approximation volume, and whether a presentation boundary is present. Serialized
grid, occupied-index, and presentation-boundary mesh details are internal.
Format version `1` is occupancy-only; format version `2` adds an optional
volume-local presentation-boundary triangle mesh. `IsUsable` accepts both.
An internal implementation answers `IFloodVolumeGeometry` queries for every
finite non-zero local plane. Source mesh vertices and triangles are never read
from live Mesh Filters at runtime.

The Editor baker transforms one readable source Mesh Filter into the target
`FloodVolume` local space. The source must be a closed manifold triangle mesh:
every undirected edge appears exactly twice and no triangle is degenerate.
Open, non-manifold, unreadable, empty-at-resolution, or over-limit input is
rejected without replacing the previous successful asset.
Self-intersecting meshes are unsupported. The baker validates manifold edge
topology but does not perform exhaustive triangle-triangle intersection tests;
such a source can produce a deterministic but semantically invalid cell bake.

The requested Cell Resolution is the maximum cell edge in meters. Grid counts
are the ceiling of source-bounds size divided by that value; actual per-axis
cell dimensions evenly divide the bounds and may be smaller. A cell is occupied
when its center is inside the mesh. Therefore capacity and centroid are exact
for the baked union but approximate the source mesh. Features thinner than a
cell may disappear.

The reported boundary approximation indicator is the number of sampled grid
cells with mixed inside/outside corners multiplied by actual cell volume. It is
useful for comparing resolutions but is not a certified source-mesh error bound,
because unsampled sub-cell features may exist.

Changing the source mesh dependency, source-to-volume Transform, or requested
resolution marks the bake stale in the Editor. Missing or unsupported data is
invalid in Baked Data mode. Runtime may switch to another previously baked
asset with `ConfigureBakedGeometry`; runtime mutation or rebaking is unsupported.

Baked free-surface contours prefer plane ∩ presentation-boundary mesh (format
`2`), stitched into one or more closed contours by the shared mesh-plane
intersection helper. Occupancy voxels still determine capacity, submerged
volume, and the solved surface plane. Therefore the visible footprint can be
more accurate than the voxel volume approximation. Format `1` assets without a
presentation boundary fall back to one ordered contour per intersected occupied
cell. `FloodRegionBaker` writes format `2` presentation boundaries from exterior
occupancy faces (internal shared faces omitted; silhouette remains stepped at
cell resolution). Volume bake may instead copy an authored closed source mesh.
`FloodBakedSurfaceRenderer` and occupancy-backed `FloodRegionSurfaceRenderer`
consume those contours as a free-surface sheet (ear-clipped; not a closed
submerged solid). Hole loops (inner contours) are a known triangulation
limitation.

## Presentation

Presentation is optional and must not mutate flooding state.

`FloodSurfaceRenderer` is the presentation contract. It:

- subscribes to one `FloodVolume`,
- reads immutable `FloodState` snapshots,
- interpolates between published states over a configurable duration,
- exposes an immediate snap operation,
- delegates actual visual changes to a concrete renderer.

`FloodCubeSurfaceRenderer` is the rectangular compatibility implementation. It
uses exact clipped geometry when its child has a Mesh Filter and retains its
older transform-scaling fallback for meshless child objects.

`FloodPolygonSurfaceRenderer` generates a closed water-volume mesh clipped to
the interpolated surface plane. `FloodCubeSurfaceRenderer` does the same for a
rectangular source when its assigned child has a Mesh Filter; its transform-only
fallback remains level-fill compatibility behavior. Presentation writes no
simulation state.

Setting interpolation duration to zero applies each published state
immediately. Disabling or removing any renderer must not change simulation
volume or event publication.

`FloodConnectionVisual` is an optional presentation consumer for one
`FloodConnection`. It reads applied flow rate, submerged opening area, and
world flow direction, then drives authored Transform, ParticleSystem, and/or
MeshRenderer targets. It never mutates connection or volume state.

Local ingress presentation is an optional visual proxy for early compartment
entry. `FloodIngressSampler` builds factual `FloodIngressSample` values
(provider id, destination volume, world position, direction into the
destination, effective flow rate) without requiring a presentation profile.
`FloodLocalIngressPresenter` expands bounded provider-owned patches that settle
and converge toward the bulk free surface. Local patches do not affect
`QueryPoint` / gameplay depth and do not store authoritative cubic meters.
Optional `IngressAnchor` fields on `FloodConnection` / `FloodSource` are ignored
by simulation.

`FloodConnectionAudio`, `FloodSourceAudio`, and `FloodVolumeAudio` are optional
3D audio consumers. They drive an `AudioSource` from applied connection flow,
configured source rate, or compartment fill percentage. Missing clips fail soft
(remain silent). Disabling any audio component must not change simulation
results.

`FloodWaterVisual` is retained only as a compatibility shim for scenes authored
before package version `0.3.0`.

## Initial state serialization

New authoring uses initial volume in cubic meters. Existing serialized
`initialWaterHeight` data is migrated by multiplying the legacy height by the
selected geometry footprint area and clamping the result to capacity.

Migration must not reinterpret a height value directly as cubic meters.

## Current supported behavior

- Rectangular-prism and simple concave polygon-prism compartments.
- Editor-baked approximations of closed complex 3D compartments.
- Volume-authoritative storage.
- Gravity-aligned planar water surfaces for rotated compartments.
- Global or manager-specific gravity and stable near-zero-gravity fallback.
- Configured direct inflow and outflow.
- Bidirectional pressure-driven connections using gravity-aligned pressure
  heads.
- Derived water mass using configurable density.
- Aggregate child-compartment flood mass and optional owned-baseline Rigidbody
  mass and center-of-mass integration.
- Immutable state snapshots and post-commit events.
- Configurable fixed-rate orchestration.
- Optional interpolated scaled-cube or generated polygon-mesh presentation.
- Optional focused baked-data free-surface presentation.
- Optional connection flow visuals and connection/source/volume audio consumers.
- Optional local ingress stream/spread presentation converging to the bulk
  free surface (visual proxy only), including optional URP jet/patch shaders,
  shader edge foam, and layered impact particle systems.

## Current non-goals

- Automatic overflow-edge discovery.
- Runtime source-mesh analysis, bake mutation, or runtime rebaking.
- Buoyancy, hydrodynamics, or vessel-stability forces. The optional Rigidbody
  adapter only applies reported mass and center of mass.
- Multiple mixed fluids in one compartment.
- Save/load and network synchronization guarantees.
- Transient slosh, surge, delayed settling, or oscillating free surfaces as
  authoritative simulation behavior. Local ingress presentation may approximate
  early localized spread visually without becoming a second fluid solver.
- CFD, SPH/FLIP, collision-based spray/foam, or pressure-shaped local surfaces.
- Large bundled VFX libraries. Presentation components expose authored slots;
  the Local Ingress URP sample may ship a minimal soft-particle texture and
  materials for showcase quality.

