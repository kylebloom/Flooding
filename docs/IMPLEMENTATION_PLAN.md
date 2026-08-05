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

- Current milestone: **Phase 16R FloodRegion composition landed (tests pending open-Editor regression); Phase 17 stress sample next**
- Overall package status: **Gameplay-consumable simulation prototype**
- Simulation vocabulary complete for v1 flooding gameplay:
  `FloodSource` (add), `FloodConnection` (transfer + `OpenFraction`),
  `FloodSink` (remove), `FloodVolume` (store/query), plus presentation layers
- Current supported geometry: **Rotated prism or Editor-baked data**
- Current presentation: **Clipped prism volume, baked free-surface patches, connection visuals, optional flow/fill audio, camera/underwater, and local ingress**
- Current flow model: **Configured inflow/outflow sinks, finite connections with open-fraction aperture control, and external boundaries**
- Current query surface: **Live read-only point queries over authoritative state**
- Path to publish: see [Path to 1.0](#path-to-10--gameplay-ready-package)

Implementation status and verification status are tracked separately. Phases 7
through 10 are implemented but are not marked Unity-regression-verified until the
full Edit Mode and Play Mode suites run after those changes.
Refinement G's authored-sample implementation and package documentation are
complete; Package Manager re-import inspection, sample Play Mode behavior, and
the same regression suites remain pending.

## Agreed architectural boundaries

1. `FloodVolume` authors floodable geometry and exposes gameplay queries.
   Standalone volumes own water state; region members delegate water state to
   their `FloodRegion`.
2. `FloodRegion` owns independently simulated / equilibrium water state for
   one or more explicit member volumes (`InitialVolume`, `CurrentVolume`,
   shared surface plane).
3. Simulation code determines volume transfers and derived water state.
4. Geometry implementations answer capacity, submerged-volume, centroid, and
   surface-intersection queries. Region unions use `CompositeFloodGeometry`
   with pluggable strategies (analytic two-box prototype; occupancy bake later).
5. Presentation components consume simulation state without mutating it.
   Composed regions use region-level surface presentation, not stacked member
   renderers.
6. Connections calculate flow between independently simulated regions; they do
   not directly commit transfers. Same-region endpoints are an authoring error.
7. A simulation manager evaluates a shared snapshot and commits all transfers
   simultaneously. Mutations targeting a member volume resolve to the owning
   region via effective-boundary resolution.
8. Flooding reports mass contributions but does not control buoyancy, vessel
   movement, sinking, roll, or pitch.
9. Exterior water is represented as a fluid boundary, not as an arbitrary
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
- [x] Gate `Kyle.Flooding.URP` with `KYLE_FLOODING_URP` defineConstraints so the
  package compiles without Universal RP installed; decouple Editor / core Play
  Mode from hard URP references (`Tests/PlayMode.URP` for effect tests).
- [x] Upgrade underwater shader to camera-ray / `SurfacePlane` intersection with
  optical-path fog; document infinite-plane (no volume screen mask) limitation.
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
- Package installs/compiles without Universal RP; underwater assembly appears
  only when URP ≥ 17 is present.
- Auto-discover manager resolution retries while null and after scene loads.
- Package version `0.10.0` for the first-person / camera presentation feature
  set.

## Phase 14 — Local ingress presentation

Goal: present early localized water entry without CFD or a second authoritative
volume.

- [x] Add factual profile-independent `FloodIngressSample` and
  `FloodIngressSampler`.
- [x] Add optional presentation-only `IngressAnchor`, `OpeningCenterWorld`, and
  `IngressWorldPosition` on `FloodConnection` / `FloodSource`.
- [x] Add `FloodIngressPresentationProfile` and pure
  `FloodIngressPresentationState` with Growing / Settling / Converging phases,
  one patch per provider, and bounded reuse without cross-provider merge.
- [x] Add `FloodLocalIngressPresenter` (shared unit disc + MPB, floor plane,
  diagnostics) and lightweight `FloodIngressStreamPresenter`.
- [x] Do not add `VisualFillWeight` to `FloodSurfaceRenderer`; fade local
  opacity during convergence and document residual overlap.
- [x] Add Edit Mode lifecycle tests and Play Mode non-mutation tests.
- [x] Add Local Ingress sample, package docs, and version `0.11.0`.
- [x] Final regression: Edit Mode 104/104 and Play Mode 66/66 passed
  (Unity 6000.5.6f1), including 14 ingress Edit Mode and 2 presenter Play Mode
  tests.
- [x] URP visual-quality pass: layered impact particles (droplets/mist/foam),
  upgraded jet/patch shaders with edge foam, showcase-tuned Local Ingress
  sample (simulation/state architecture unchanged).

Acceptance criteria:

- Presentation never mutates `FloodVolume` or solver transfers.
- Sampling does not require a presentation profile.
- Reversed connection flow selects the correct destination and flips
  `DirectionWorld` into that volume.
- One provider updates one patch across frames; stop transitions through
  settle/converge without popping.
- Core runtime remains free of URP dependencies for local ingress.
- URP showcase aims for single-digit / low-teens draw calls per active major
  ingress with bounded particle counts; no CFD.

## Phase 15 — Runtime opening / flow controls (0.12.0)

Goal: let gameplay restrict connection aperture without mutating authored
opening geometry or overloading discharge coefficient.

- [x] Add `FloodConnection.OpenFraction` as a 0–1 effective-aperture multiplier.
- [x] Apply fraction after submerged aperture in `FloodFlowCalculator`
  (`authored geometry → submerged aperture → × OpenFraction → orifice flow`).
- [x] Keep `IsOpen` as a hard gate; keep `OpeningWidth` / `OpeningHeight` as
  fully-open authored geometry; do not overload `DischargeCoefficient`.
- [x] Expose `FullOpeningArea` / `EffectiveOpeningArea` helpers.
- [x] Add Edit Mode and Play Mode tests for 0 / 0.5 / 1, reverse flow, exterior
  depth sensitivity, authored-dimension immutability, and non-finite rejection.
- [x] Document SPEC / ARCHITECTURE / editor workflow door pattern; optional
  Local Ingress aperture keys; package version `0.12.0`.
- [x] Unity regression: Edit Mode 115/115 and Play Mode 73/73 passed
  (Unity 6000.5.6f1) after OpenFraction.

Acceptance criteria:

- [x] `OpenFraction = 1` preserves prior hydraulic behavior.
- [x] `OpenFraction = 0` with `IsOpen = true` yields zero flow.
- [x] `OpenFraction = 0.5` halves requested flow under identical heads.
- [x] Reverse flow and exterior depth sensitivity remain correct.
- [x] Changing `OpenFraction` does not mutate authored width/height.
- [x] Non-finite runtime values are rejected deterministically.
- [x] Presentation continues to consume applied flow, not `OpenFraction` directly.

## Phase 16 — Pumps / drains / sinks (0.13.0)

Goal: introduce manager-mediated finite-volume sinks as the canonical primitive
for gameplay-driven water removal.

- [x] Add `FloodSink` (`Target`, `FlowRate`, `IsActive`, `RequestedFlowRate`,
  `CurrentFlowRate`) as the inverse of `FloodSource`.
- [x] Register sinks with `FloodSimulationManager`; request removal; share
  finite supply scaling with connection outflows.
- [x] Preserve same-tick snapshot rules (sinks do not free capacity; sources do
  not supply sinks).
- [x] Add `FloodTickMetrics.ConfiguredSinkVolume` (applied) and update
  conservation identity.
- [x] Add `FloodSource.CurrentFlowRate` / `RequestedFlowRate` for API symmetry.
- [x] Play Mode tests for dry/limited supply, proportional sharing, connection
  competition, source+sink same-tick rules, and metrics.
- [x] Hull Breach sample bilge pump (`B` toggle) + docs; package `0.13.0`.
- [x] Unity regression: Edit Mode 115/115 and Play Mode 86/86 passed
  (Unity 6000.5.6f1) after FloodSink.

Acceptance criteria:

- [x] Active sink removes configured amount when supply allows.
- [x] Inactive / zero-rate / dry target produce zero applied flow.
- [x] Volume never goes negative.
- [x] Multiple sinks and connection outflows share supply proportionally.
- [x] Source additions do not provide same-tick sink supply; sink removals do not
  free same-tick source capacity.
- [x] Conservation uses applied sink volume.
- [x] No egress anchors, destinations, power, or intake physics in the package.

## Phase 16R — FloodRegion composition (0.14.x)

Goal: allow multiple explicit `FloodVolume` members to compose one independently
simulated region with correct union capacity and continuous first-person
presentation, without automatic overlap-based merging.

Core model:

```text
FloodVolume = authored floodable geometry
FloodRegion = independently simulated / equilibrium body of water
FloodConnection = hydraulic restriction between FloodRegions
FloodSimulationManager = orchestration / conservation
```

- [x] Phase A — Ownership: `FloodRegion`, one-member parity, `InitialVolume`,
  effective-boundary resolution, manager/public API compatibility.
- [x] Phase B — `CompositeFloodGeometry` + `TwoBoxAnalyticUnionStrategy`
  (prototype only; not long-term IE architecture).
- [x] Phase C — Two-member end-to-end: region queries, region-level surface
  renderer, source/sink/connection routing, same-region connection ERROR,
  tilt tests.
- [x] Phase D — Design only: region-local occupancy / `FloodRegionData` bake
  documented in `docs/FLOOD_REGION_OCCUPANCY_DESIGN.md`.
  No runtime mesh CSG; no silent analytic voxelization.

Acceptance criteria (vertical slice):

- [x] One-member region behaviorally matches standalone `FloodVolume` (tests added).
- [x] Two-member union capacity; overlap and partial-fill counted once (Edit Mode).
- [x] Face-touching continuity accepted; disconnected members validated.
- [x] One region-owned `CurrentVolume` and one equilibrium `SurfacePlane`.
- [x] Member `ContainsPoint` = member geometry; water/depth from region.
- [x] Continuous region-level surface via `FloodRegionSurfaceRenderer`.
- [ ] Standalone volume and existing connection regressions pass (Unity suite
  pending — project locked by open Editor).
- [x] Rotated/tilted region preserves gravity-aligned surface (Play Mode test).
- [x] Same-region `FloodConnection` is an authoring error.

## Path to 1.0 — gameplay-ready package

Publishing roadmap after the core flooding vocabulary landed. Version numbers
mark delivery sequence; SemVer may insert patch releases between milestones.

| Milestone | Goal | Status |
| --- | --- | --- |
| **0.11** | Polished local ingress presentation | [x] Phase 14 complete |
| **0.12** | Runtime flow-control / opening controls (`OpenFraction`) | [x] Phase 15 complete |
| **0.13** | Pumps / drains / sinks (`FloodSink`) | [x] Phase 16 complete |
| **0.14** | Complex multi-compartment stress sample | [ ] Phase 17 |
| **0.14.x** | FloodRegion composition (ownership → two-box union → region surface) | [~] Phase 16R |
| **0.15** | Authoring / debug UX pass | [ ] Phase 18 |
| **0.16** | Performance / profiling pass | [ ] Phase 19 |
| **0.9x / RC** | API stabilization + docs freeze candidate | [ ] Phase 20 |
| **1.0** | Stable gameplay-ready package | [ ] Phase 21 |

**Done enough for gameplay foundation:** authoritative volume flooding,
connections, exterior breach inflow, gravity-aware surfaces, rotated/baked
geometry, gameplay queries, manager-driven ticks, FP camera / underwater /
waterline, audio/telemetry hooks, local ingress presentation, `OpenFraction`,
and `FloodSink`.

**Explicitly not required for 1.0:** real CFD (Navier–Stokes / SPH / FLIP),
true sloshing as authoritative sim, physically accurate waves, perfect floor
propagation, foam fluid sim, pressure waves, turbulent flow solvers, or
real-time arbitrary mesh fluid occupancy beyond the baked/analytic approach.

Future work after 1.0 is optional fidelity and tooling, not fundamental
capability gaps.

## Phase 17 — Complex multi-compartment stress sample (0.14)

Goal: prove the full vocabulary together in a moderately complex network, not
isolated features.

Target shape (example, not a fixed topology):

```text
Deck A: Room1 ── Door ── Corridor ── Door ── Room2
                 │                    │
               breach              Stairwell
                                      │
Deck B: Engine Room ── Hatch ── Boiler Room
```

Suggested scale:

- 10–20 `FloodVolume` compartments
- 15–30 `FloodConnection` openings (doors, hatches, vents as metaphors)
- 2 external breaches
- several runtime-controllable doors (`IsOpen` / `OpenFraction`)
- at least one `FloodSink` pump and optional `FloodSource` leak
- player or camera moving between compartments for 15–30 minutes of Play Mode

Checklist:

- [ ] Author the stress sample (Package Manager sample + builder if needed).
- [ ] Exercise breach → propagation → door/hatch control → pump mitigation.
- [ ] Observe volume conservation / `FloodTickMetrics.ConservationError`.
- [ ] Watch for unstable oscillation, weird reverse-flow, event churn,
  presentation popping, overlapping volumes, and authoring pain.
- [ ] Note CPU cost qualitatively; detailed budgets belong in Phase 19.
- [ ] Document setup, controls, expected behavior, and known limits.
- [ ] Package version `0.14.0`.

Acceptance criteria:

- Network runs stably for a long Play Mode session without NaNs, negative
  volumes, or unbounded conservation drift.
- Gameplay controls (open/close aperture, pump on/off) visibly change outcomes.
- Failures and authoring sharp edges are filed as Phase 18 / 19 follow-ups,
  not silently ignored.

## Phase 18 — Authoring / debug UX pass (0.15)

Goal: make large environments authorable without fighting Unity.

Editor / debug tooling targets:

```text
Create FloodVolume
Bake geometry
Create connection between selected volumes
Assign breach / ingress anchor
Visualize connection direction, opening area, fill %, surface plane, flow
Show validation warnings
```

Example warnings:

```text
Connection Side A/B not assigned
Opening lies outside FloodVolume
Overlapping FloodVolumes detected
Capacity is zero
External body density mismatch
```

Checklist:

- [ ] Audit existing Inspectors, gizmos, and `FloodDiagnostics` gaps.
- [ ] Add or harden creation / wiring menus for volumes and connections.
- [ ] Surface actionable validation for common authoring mistakes.
- [ ] Improve Scene-view visualization of openings, flow, fill, and surfaces.
- [ ] Document the authoring pass in editor-workflow + troubleshooting.
- [ ] Package version `0.15.0`.

Acceptance criteria:

- A new developer can wire a multi-compartment ship from the docs without
  undocumented Inspector rituals.
- Invalid setups produce clear warnings before or during Play Mode.
- Debug overlays answer “where is water / how is it flowing?” without custom
  scripts.

## Phase 19 — Performance / profiling pass (0.16)

Goal: confirm the scalar-compartment architecture stays cheap at stress-sample
scale and document budgets for presentation.

Expected cost profile:

- Core hydraulics: few compartments + connections (should remain inexpensive
  even toward ~100 volumes / ~200 connections).
- GPU / presentation: transparent surfaces, underwater pass, ingress shaders,
  particles — the likely hot path.

Checklist:

- [ ] Profile the 0.14 stress sample (CPU sim tick, GC, presentation draw calls).
- [ ] Record budgets / guidance for compartment counts and ingress particle caps.
- [ ] Fix only hotspots that threaten gameplay-scale use; avoid premature CFD.
- [ ] Add or update profiling notes in docs (limits, recommended scales).
- [ ] Package version `0.16.0`.

Acceptance criteria:

- Documented guidance for “safe” network size on a reference machine.
- No unexplained per-tick spikes or unbounded allocations in the stress sample.
- Presentation budgets (especially ingress) remain in the previously intended
  single-digit / low-teens draw-call band per major active ingress where
  applicable.

## Phase 20 — API stabilization + docs (0.9x / RC)

Goal: freeze the public gameplay surface for a release candidate.

Checklist:

- [ ] Inventory public runtime APIs; mark obsolete or remove pre-1.0 debt.
- [ ] Write / update public API compatibility policy in `SPEC.md`.
- [ ] Stabilize serialized field names and migration paths.
- [ ] Docs freeze candidate: README, index, editor-workflow, local-ingress,
  samples, SPEC, ARCHITECTURE all consistent with shipped behavior.
- [ ] Full Edit Mode + Play Mode regression on Unity `6000.5.6f1`.
- [ ] Package Manager sample re-import smoke for every sample.
- [ ] Changelog release notes for RC; version `0.9.0` (or next `0.9.x`).

Acceptance criteria:

- No known blocking gameplay API gaps relative to the 0.11–0.13 vocabulary.
- Compatibility policy states what may break after 1.0 vs what must not.
- RC is installable from the repo / Package Manager with samples that run.

## Phase 21 — Stable gameplay-ready package (1.0.0)

Goal: ship a stable flooding **gameplay** simulator with convincing presentation —
not a general fluid simulator.

Checklist:

- [ ] Promote RC after soak on the stress sample and sample suite.
- [ ] Version `1.0.0`; finalize CHANGELOG and package metadata.
- [ ] Confirm 1.0 non-goals remain explicit in SPEC (no CFD requirement).
- [ ] Tag release; publish notes for consumers.

Acceptance criteria:

- Core loop is reliable: breach → rooms flood → openings propagate → player
  sees/feels water → doors/valves/pumps change outcomes → survival/failure
  conditions can react.
- Documentation is sufficient for a third party to build a multi-compartment
  flooding prototype without reading internal design chats.
- Remaining work is optional fidelity / tooling, not missing primitives.

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
5. Public API compatibility policy before package version `1.0.0` — owned by
   Phase 20 (0.9x / RC); must land in `SPEC.md` before tagging `1.0.0`.

## Progress log

### 2026-08-05

- Completed Phase 14 / 0.11 local ingress (including URP visual-quality pass).
- Completed Phase 15 / 0.12 `OpenFraction` runtime aperture control.
- Completed Phase 16 / 0.13 `FloodSink` pumps/drains with applied-flow
  diagnostics and Hull Breach bilge sample.
- Documented the Path to 1.0 publishing roadmap (Phases 17–21: stress sample,
  authoring UX, performance, RC API freeze, 1.0 release) with explicit 1.0
  non-goals.

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

