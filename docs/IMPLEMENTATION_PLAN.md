# Flooding Package Implementation Plan

## Purpose

This document is the durable implementation roadmap and status ledger for the
reusable Unity flooding package located at:

`Packages/com.rabbidwolf.com.kyle.flooding`

It should be updated whenever a milestone, architectural decision, assumption,
or implementation status changes. Detailed design intent remains in
`proposed-changes.md`; this document records the agreed delivery sequence and
current status.

Post–Phase 8 architectural refinements, API cleanup, invariants, diagnostics,
and future-boundary design are tracked in `ALTERATIONS_IMPLEMENTATION_PLAN.md`.
This document remains authoritative for feature-phase delivery.

## Project constraints

- Target Unity Editor 6.5 (`6000.5.6f1`).
- Use SI units: meters, cubic meters, seconds, kilograms, and kilograms per
  cubic meter.
- Model gameplay-relevant bulk fluid behavior, not computational fluid
  dynamics.
- Treat water volume as authoritative. Height, surface plane, mass, and center
  of mass are derived values.
- Keep simulation, presentation, authoring, and vessel physics responsibilities
  separated.
- Perform expensive geometry analysis in the Unity Editor and consume compact
  baked data at runtime.
- Prefer deterministic Edit Mode tests for simulation rules. Use Play Mode
  tests only for behavior requiring GameObjects, lifecycle, physics, or frame
  execution.
- Preserve working behavior while replacing one prototype assumption at a
  time.

## Status legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `[!]` Blocked or requires a decision

## Current status

- Current milestone: **Phase 12 gameplay query API implemented; Unity regression verification pending**
- Overall package status: **Gameplay-consumable simulation prototype**
- Current supported geometry: **Rotated prism or Editor-baked data**
- Current presentation: **Clipped prism volume, baked free-surface patches, connection visuals, and optional flow/fill audio**
- Current flow model: **Configured inflow, finite connections, and external boundaries**
- Current query surface: **Live read-only point queries over authoritative state**

Implementation status and verification status are tracked separately. Phases 7
through 10 are implemented but are not marked Unity-regression-verified until the
full Edit Mode and Play Mode suites run after those changes.
Refinement G's authored-sample implementation and package documentation are
complete; Package Manager re-import inspection, sample Play Mode behavior, and
the same regression suites remain pending.

## Agreed architectural boundaries

1. `FloodVolume` owns and exposes compartment water state.
2. Simulation code determines volume transfers and derived water state.
3. Geometry implementations answer capacity, submerged-volume, centroid, and
   surface-intersection queries.
4. Presentation components consume simulation state without mutating it.
5. Connections calculate flow; they do not directly commit transfers.
6. A simulation manager evaluates a shared snapshot and commits all transfers
   simultaneously.
7. Flooding reports mass contributions but does not control buoyancy, vessel
   movement, sinking, roll, or pitch.
8. Exterior water is represented as a fluid boundary, not as an arbitrary
   water generator.

## Phase 0 — Stabilize the package

Goal: establish a compiling, documented, tested baseline without changing the
prototype's observable flooding behavior.

- [x] Standardize runtime, Editor, and test assembly names and references under
  `Kyle.Flooding`.
- [x] Standardize namespaces under `Kyle.Flooding`.
- [x] Remove unrelated Unity package-template classes and tests.
- [x] Replace empty configuration scaffolding only when a real setting is
  required; otherwise remove it.
- [x] Correct package metadata and placeholder URLs.
- [x] Replace placeholder README, changelog, documentation, and third-party
  notice content.
- [x] Add deterministic Edit Mode tests for:
  - construction and capacity,
  - invalid dimensions,
  - initial-volume clamping,
  - volume addition and removal,
  - stepping inflow and outflow,
  - empty/full state,
  - fill percentage and derived height.
- [x] Correct `proposed-changes.md` to acknowledge that `FloodSimulation`
  already stores authoritative volume.
- [x] Verify package compilation and Edit Mode tests in Unity 6.5.
- [x] Verify the existing Play Mode prototype retains its previous behavior.

Acceptance criteria:

- The package compiles without assembly-reference or namespace errors.
- Existing rectangular-room prefabs retain their behavior.
- No unrelated template API remains in the shipped runtime assembly.
- Core rectangular simulation rules have meaningful Edit Mode coverage.
- Package documentation accurately describes the supported prototype.

## Phase 1 — Establish the public simulation API

Goal: create a stable state contract before adding more simulation features.

- [x] Introduce an immutable `FloodState`.
- [x] Expose volume, capacity, fill, surface plane, water mass, and center of
  mass through stable read-only APIs.
- [x] Introduce state and volume change events emitted once per publication
  step. Phase 3 now publishes from the fixed simulation tick.
- [x] Report accepted and rejected volume when capacity limits a mutation.
- [x] Store initial state as volume rather than height.
- [x] Define API compatibility and serialization expectations.
- [x] Add tests for state snapshots, events, and capacity-limited mutations.
- [x] Verify Phase 1 Edit Mode and Play Mode tests in Unity 6.5.
- [x] Verify the existing Play Mode example retains its expected behavior.
- [x] Document practical Unity Editor setup, scripting, migration, testing, and
  troubleshooting.

Acceptance criteria:

- Height remains derived from volume.
- Consumers can observe state without depending on internal implementation.
- Capacity-limited operations report exact accepted and rejected quantities.

## Phase 2 — Separate simulation and presentation

Goal: make the current visual implementation replaceable.

- [x] Replace `FloodWaterVisual` with a presentation-only
  `FloodSurfaceRenderer` contract/component.
- [x] Preserve the scaled-cube renderer as the first implementation.
- [x] Ensure renderers only consume state.
- [x] Add interpolation between simulation states.
- [x] Keep audio, effects, and materials outside simulation logic.
- [x] Preserve existing scenes through a `FloodWaterVisual` compatibility shim.
- [x] Document practical Editor setup, extension, migration, and
  troubleshooting.
- [x] Verify Phase 2 Edit Mode and Play Mode tests in Unity 6.5.

Acceptance criteria:

- Flood simulation works with no renderer present.
- Replacing or disabling the renderer does not affect water state.
- The current prefab remains visually functional.

## Phase 3 — Add fixed-step simulation orchestration

Goal: remove frame-order dependence and prepare for multiple connected volumes.

- [x] Add `FloodSimulationManager`.
- [x] Configure a simulation frequency, defaulting to 10 Hz.
- [x] Capture registered volume state, transforms, and current surface data at
  tick start.
- [x] Calculate and aggregate configured source inflows from the same snapshot.
- [x] Limit incoming source volume by destination capacity.
- [x] Commit all destination changes before publishing any state.
- [x] Recalculate derived state and publish events after commit.
- [x] Add manual ticking, catch-up limits, and discarded-tick diagnostics.
- [x] Remove per-frame source mutation and volume event polling.
- [x] Add the manager to the included prefab.
- [x] Document setup, migration, manual advancement, fields, units, and
  troubleshooting.
- [x] Verify Phase 2 and Phase 3 Edit Mode and Play Mode tests in Unity 6.5.

Outgoing source-availability reconciliation and internal transfer conservation
remain Phase 4 work because Phase 3 has no connections between finite volumes.

Tick order:

1. Capture registered immutable volume states.
2. Evaluate active configured sources.
3. Aggregate requested inflow by destination.
4. Reconcile destination capacity from the captured snapshot.
5. Commit all destination changes.
6. Capture and publish changed states.
7. Publish tick completion.

Acceptance criteria:

- Results do not depend on MonoBehaviour `Update()` order.
- Multiple sources cannot overfill a volume.
- Every changed volume publishes only after the tick commits.

## Phase 4 — Connect stationary rectangular volumes

Goal: support deterministic bidirectional flow between level rectangular rooms.

- [x] Add `FloodConnection`.
- [x] Represent opening position, width, height, discharge coefficient, and
  open/closed state.
- [x] Calculate submerged opening area and pressure-head difference.
- [x] Implement signed bidirectional orifice flow.
- [x] Expose requested/applied flow rate, direction, area, and head diagnostics.
- [x] Retain `FloodSource` as an explicit infinite configured boundary.
- [x] Reconcile multiple finite-source outflows and shared destination capacity
  before committing one delta per volume.
- [x] Add an opening gizmo and complete Inspector tooltips.
- [x] Document practical connection setup, scripting, migration, limitations,
  and troubleshooting.
- [x] Verify Phase 2 through Phase 4 Edit Mode and Play Mode tests in Unity 6.5.

The exterior-ocean boundary remains Phase 9 work; Phase 4 connections only join
two finite `FloodVolume` instances.

Required tests:

- [x] Equal pressure heads produce no flow.
- [x] Flow travels from greater head to lower head.
- [x] Flow reverses when conditions reverse.
- [x] Closed connections transfer nothing.
- [x] Connections conserve volume.
- [x] Capacity and source-availability reconciliation remains deterministic.

## Phase 5 — Introduce reusable container geometry

Goal: decouple flood state from rectangular dimensions.

- [x] Define a geometry contract for:
  - capacity,
  - submerged volume beneath a plane,
  - submerged centroid,
  - local bounds,
  - surface intersection data.
- [x] Implement rectangular-prism geometry.
- [x] Implement an extruded polygon footprint with vertical walls.
- [x] Validate polygon winding, area, self-intersections, and unsupported input.
- [x] Define numerical tolerances and failure behavior.
- [x] Add conditional Inspector authoring, validation feedback, selected-object
  gizmos, and draggable polygon points.
- [x] Add generated polygon-water presentation.
- [x] Verify all Edit Mode and Play Mode tests in Unity 6.5.

Phase 5 originally introduced local-XZ-parallel queries. Phase 6 now extends
the same contract to exact arbitrary-plane evaluation.

Acceptance criteria:

- Existing rectangular behavior runs through the geometry abstraction.
- Concave supported footprints produce stable capacity and fill results.
- Invalid authoring data is rejected with actionable Editor feedback.

## Phase 6 — Add gravity-aligned water surfaces

Goal: make water settle relative to gravity when compartments rotate.

- [x] Derive the surface normal from global or manager-specific gravity.
- [x] Transform the candidate plane between world and container-local space.
- [x] Solve plane position from volume using bounded binary search.
- [x] Calculate submerged centroid from exact clipped geometry.
- [x] Retain the last valid local surface orientation near zero gravity.
- [x] Interpolate and render clipped surface movement.
- [x] Verify all Edit Mode and Play Mode tests in Unity 6.5.

Acceptance criteria:

- Rotating a compartment does not change stored water volume.
- The surface remains perpendicular to gravity.
- Solved submerged volume remains within the documented tolerance.

## Phase 7 — Configure and integrate reported water mass

Goal: aggregate the mass state established in Phase 1 and optionally integrate
it with Rigidbody properties without coupling flooding to vessel behavior.

Phase 1 already exposed water mass and world-space center of mass. Phase 7
builds on that state rather than introducing it.

- [x] Add configurable fluid density, defaulting to `1000 kg/m³`.
- [x] Add `IMassContributor`.
- [x] Add deterministic multi-compartment `FloodMassAggregator`.
- [x] Add an optional Rigidbody mass-contribution adapter.
- [x] Add a demonstration scene for roll/pitch response.

Acceptance criteria:

- Flooding reports mass without directly moving or rotating its parent.
- Multiple compartments aggregate mass and center of mass correctly.
- Physics integration can be removed without changing flood simulation.

## Phase 8 — Bake complex three-dimensional geometry

Goal: support sloped floors, curved hulls, and uneven interiors without runtime
mesh analysis.

- [x] Add `FloodVolumeAuthoring`.
- [x] Add immutable baked `FloodVolumeData`.
- [x] Add Editor bake, validation, and visualization tools.
- [x] Support configurable sample resolution and report approximation
  diagnostics.
- [x] Store compact capacity and centroid data for runtime queries.
- [x] Detect stale or missing bake data.

Acceptance criteria:

- Runtime performs no source-mesh analysis.
- Bake resolution and expected error are visible to designers.
- Complex geometry uses the same simulation and presentation contracts.

## Phase 9 — Add external fluid boundaries

Goal: model ocean or reservoir inflow through the same connection system.

Flood connections transfer matching-density fluid bidirectionally between finite
flood volumes and infinite external fluid boundaries using snapshot-based
hydrostatic head calculations, shared reconciliation, and finite-only commit
semantics.

- [x] Add `ExternalFluidBoundary` (Inspector display name **External Fluid Body**).
- [x] Expose Transform-driven exterior surface plane and density.
- [x] Introduce `IFluidBoundary`, `FluidBoundarySnapshot`, and generalized
  `FloodConnection` endpoint slots.
- [x] Connect external boundaries through `FloodConnection`.
- [x] Account for breach depth, opening area, and centroid pressure-head
  difference.
- [x] Support flow reversal when interior head exceeds exterior head.
- [x] Track external inflow/outflow explicitly in `FloodTickMetrics`.
- [x] Add Hull Breach Package Manager sample.

Acceptance criteria:

- A breach does not generate arbitrary water.
- Inflow changes when breach depth or exterior waterline changes.
- Exterior and interior pressure can reach equilibrium.

## Phase 10 — Extend presentation

Goal: provide optional visual and audio consumers driven by measured flow state.

- [x] Add `FloodConnectionVisual`.
- [x] Add connection, source, and volume audio components.
- [x] Drive effects from flow rate, submerged area, fill, and direction.
- [x] Add selected-object diagnostics for surfaces, flow, gravity, volume, and
  centers of mass. Delivered early by the architecture refinement.

Acceptance criteria:

- Effects are spatially associated with their physical source.
- Presentation can be disabled without changing simulation results.

## Phase 11 — Complete package delivery

Goal: make the package suitable for reuse in future Unity projects.

- [x] Add Package Manager sample declarations and functional samples.
- [x] Document public APIs, units, setup, limitations, and extension points.
- [ ] Maintain changelog and upgrade guidance.
- [ ] Validate import into a clean Unity 6.5 project.
- [ ] Verify Edit Mode and targeted Play Mode test suites.
- [ ] Review package contents for unnecessary dependencies and assets.

The documentation-onboarding subset is tracked as Refinement F in
`ALTERATIONS_IMPLEMENTATION_PLAN.md`. Persistent, Inspector-editable sample
authoring is tracked as Refinement G in the same companion plan.

## Phase 12 — Gameplay query API

Goal: make existing simulation state consumable by other game systems without
new simulation behavior.

- [x] Add `FloodQueryResult` and `FloodContainmentPrecision`.
- [x] Add `IFloodVolumeGeometry.ContainsLocalPoint` with exact prism/polygon
  containment and bake-cell approximation for baked geometry.
- [x] Add `FloodVolume.ContainsPoint`, `IsPointSubmerged`, and `QueryPoint`
  over live authoritative state and the cached surface plane.
- [x] Add `FloodQueryResult.SurfaceSignedDistanceMeters` (positive above,
  zero on, negative below the authoritative surface plane) without changing
  `SubmersionDepthMeters` semantics.
- [x] Document live-read vs post-publish event contract and baked containment
  precision.
- [x] Add Edit Mode containment / query-result tests and Play Mode query tests
  (including rotated volume and tilted surface plane coverage).
- [ ] Verify the new suites in Unity Editor regression.

Acceptance criteria:

- Point queries never advance, reconcile, or publish simulation.
- `IsSubmerged` requires both containment and plane depth.
- Baked containment precision is explicit on the geometry contract.
- Existing fill/volume/state properties remain the canonical state reads
  (`FillPercentage`, `CurrentVolume`, `CurrentState`, `StateChanged`).

## Phase 13 — Camera / presentation tracking

Goal: expose reusable camera flood-state tracking without coupling rendering
or URP into the core simulation assembly.

- [x] Add `FloodSimulationManager.RegisteredVolumes` read-only registry view.
- [x] Add `FloodCameraTracker` with explicit and sticky auto-discover selection,
  underwater hysteresis, and presentation events.
- [x] Document overlap policy, signed-distance hysteresis, and Editor setup.
- [x] Add Edit Mode hysteresis tests and Play Mode tracker / selection tests.
- [x] Add `FloodUnderwaterProfile` ScriptableObject (presentation settings only).
- [x] Add optional `Kyle.Flooding.URP` assembly with underwater renderer feature,
  camera effect bridge, fullscreen waterline shader, and depth-texture setup docs.
- [x] Add `FloodUnderwaterAudio` and framework-neutral telemetry adapters.
- [x] Add First Person Flooding sample (builder, bootstrap, README, package entry).
- [x] Final regression: Edit Mode 90/90 and Play Mode 64/64 passed (Unity 6000.5.6f1).

Acceptance criteria:

- Tracker never mutates simulation state.
- Auto-discover does not call scene-wide object search every frame.
- Active volume selection stays sticky while the current volume contains the
  viewpoint, even when dry.
- Overlapping volumes remain ambiguous / not merged.
- Core runtime remains free of URP and TextMeshPro dependencies.

## Documentation roadmap

Before Phase 1 implementation:

- [x] Add `SPEC.md` for externally observable behavior, units, tolerances,
  supported cases, and explicit non-goals.
- [x] Add `ARCHITECTURE.md` for assembly boundaries, state ownership, tick
  ordering, geometry contracts, flow reconciliation, and extension points.

For every implementation phase:

- Update package documentation in the same change as user-facing behavior.
- Document exact Unity Editor creation, attachment, assignment, baking, and
  configuration steps.
- Distinguish assets, scene GameObjects, child GameObjects, built-in components,
  and package script components in every practical workflow.
- Describe Inspector fields and units introduced or changed by the phase.
- Add or update concise, unit-aware `[Tooltip]` text for every Inspector-facing
  `MonoBehaviour` field.
- Include practical public-API examples where gameplay code is expected.
- Document migration, limitations, testing, and troubleshooting.
- Link detailed workflow documentation from the package README and
  documentation index.

These items are recurring completion criteria rather than a one-time milestone.

## Deferred decisions

These must be resolved in `SPEC.md` or `ARCHITECTURE.md` before the affected
phase begins:

1. [Resolved refinement] Deterministic tick phase and reconciliation ordering
   are guaranteed for stable registration order within one runtime.
   Cross-platform bit-identical floating-point results and identical Rigidbody
   behavior are explicitly not guaranteed.
2. Whether multiple fluid densities may coexist in one compartment.
3. Save/load and network synchronization requirements.
4. [Resolved Phase 8] Baked geometry is immutable at runtime. Gameplay may
   select another valid pre-baked asset but cannot mutate or regenerate data.
5. Public API compatibility policy before package version `1.0.0`.

## Progress log

### 2026-08-03

- Adopted the phased implementation plan.
- Confirmed volume-authoritative simulation as the baseline.
- Began Phase 0 package stabilization.
- Standardized assembly and namespace structure under `Kyle.Flooding`.
- Removed package-template runtime, Editor, sample, configuration, and Play Mode
  test scaffolding.
- Added Edit Mode coverage for the rectangular `FloodSimulation`.
- Replaced placeholder package metadata and documentation.
- Verified all Edit Mode tests pass in Unity 6.5.
- Verified the Play Mode prototype retains its previous behavior.
- Completed Phase 0.
- Added `SPEC.md` and `ARCHITECTURE.md`.
- Implemented Phase 1 state snapshots, exact mutation results, volume-based
  initial authoring, legacy serialization migration, and coalesced events.
- Added Phase 1 Edit Mode and Play Mode coverage.
- Verified all Phase 1 tests pass and the Play Mode example retains its expected
  behavior.
- Added a comprehensive Unity Editor workflow covering setup, Inspector fields,
  scripting, migration, testing, and troubleshooting.
- Added Inspector tooltips to all current runtime component fields and made
  tooltip coverage a recurring documentation requirement.
- Completed Phase 1.
- Implemented the presentation-only `FloodSurfaceRenderer` contract and
  interpolated `FloodCubeSurfaceRenderer`.
- Migrated the included prefab while preserving `FloodWaterVisual` as a
  compatibility shim.
- Added Phase 2 Play Mode coverage and practical renderer documentation.
- Implemented fixed-rate orchestration with snapshot-based source aggregation,
  capacity reconciliation, commit, and post-commit publication.
- Migrated the included prefab and removed per-frame source mutation and volume
  polling.
- Added Phase 3 scheduling, aggregation, validation, and regression coverage
  plus practical manager setup and migration documentation.
- Confirmed Phase 2 and Phase 3 behavior in Unity during later regression
  verification.
- Implemented bidirectional finite-volume connections with pure orifice-flow
  calculation and signed runtime diagnostics.
- Extended manager reconciliation to prevent source overdraw and destination
  overfill while conserving internal transfer volume.
- Added Phase 4 Edit Mode and Play Mode coverage plus complete connection
  authoring, scripting, migration, and troubleshooting documentation.
- Confirmed Phase 2 through Phase 4 behavior in Unity during later regression
  verification.
- Reworked the Editor workflow around an explicit scene hierarchy and separate
  GameObject, component, prefab, and material terminology.
- Confirmed the Phase 2 through Phase 4 implementation works in Unity.
- Implemented immutable geometry contracts, exact horizontal submersion
  queries, rectangular geometry, and validated concave polygon-prism geometry.
- Added conditional polygon authoring, actionable validation, Scene-view
  handles, runtime geometry configuration, and generated polygon presentation.
- Added Phase 5 Edit Mode and Play Mode coverage and updated the package
  specification, architecture, changelog, and practical workflow.
- Verified all Phase 5 Edit Mode and Play Mode tests and practical behavior.
- Implemented exact arbitrary-plane clipping, bounded surface solving, and
  submerged centroid calculation for current extruded geometry.
- Added global/custom manager gravity policy and stable near-zero-gravity
  fallback using the last valid local surface orientation.
- Updated rectangular and polygon renderers to build closed meshes clipped to
  interpolated gravity-aligned planes.
- Added Phase 6 solver, rotation, custom-gravity, zero-gravity, and rendering
  coverage plus complete specification and Editor workflow updates.
- Verified all Phase 6 Edit Mode and Play Mode tests pass in Unity 6.5.
- Verified an actively rocking compartment preserves volume while its rendered
  water surface remains aligned against gravity.
- Reconciled stale Phase 2, Phase 3, and Phase 7 status with the verified
  implementation.
- Added `IMassContributor`, deterministic multi-compartment aggregation, and an
  optional owned-baseline Rigidbody adapter.
- Added custom density and mass-integration coverage plus an importable
  roll/pitch sample with sample-only spring support.
- Completed Phase 7 implementation; final full Unity regression verification
  remains part of package-delivery validation.
- Implemented Phase 8 immutable occupied-cell assets, runtime arbitrary-plane
  clipping, geometry-mode integration, and focused free-surface presentation.
- Added Editor-only closed-mesh validation, bounded baking, stale detection,
  approximation diagnostics, and selected-cell visualization.
- Added baked geometry and integration coverage, including a deterministic
  512-cell solver guard, and updated package/repository documentation.
- Phase 8 static and Unity regression verification remains pending while the
  project is open in another Unity process.
- Created the architecture-refinement companion ledger.
- Clarified Phase 7 scope, equilibrium-surface behavior, and practical
  determinism guarantees.
- Added repeated-tick simulation invariants and optional read-only Scene-view
  diagnostics.
- Replaced cell-specific baked public APIs with representation-neutral
  pre-`1.0.0` contracts while preserving serialized geometry mode value `2`.
- Specified the future generalized fluid-boundary seam without adding Phase 9
  runtime code.
- Converted all three package samples to persistent, pre-Play authored
  hierarchies with editable component wiring and local materials, and updated
  package-wide sample ownership and overwrite guidance.
- Completed Refinement G implementation and documentation while leaving sample
  re-import inspection, runtime behavior, and Unity test verification pending.
- Implemented Phase 9 external fluid boundaries:
  `ExternalFluidBoundary`, `IFluidBoundary` snapshots, generalized
  `FloodConnection` endpoints, centroid orifice heads, matching-density
  validation, infinite supply/capacity reconciliation flags, and
  `FloodTickMetrics`.
- Added Edit Mode density/flow coverage, Play Mode external-boundary coverage,
  and the Hull Breach sample.
- Phase 9 Unity regression verification remains pending.
- Baked free surfaces use presentation-boundary mesh ∩ plane (format 2) while
  occupancy voxels remain the quantity solver; format 1 keeps voxel contours.
- Implemented Phase 10 optional presentation consumers:
  `FloodConnectionVisual`, `FloodConnectionAudio`, `FloodSourceAudio`, and
  `FloodVolumeAudio`, plus intensity mapping helpers and Play Mode
  non-mutation coverage.
- Wired `FloodConnectionVisual` into the Connected Compartments and Hull Breach
  samples.

