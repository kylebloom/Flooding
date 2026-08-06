# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Fixed

- Occupancy presentation no longer re-evaluates every occupied cell tetrahedron
  when rebuilding free-surface meshes: region/baked surface renderers use
  presentation-boundary plane ∩ mesh (`EvaluateFreeSurface`), snap instead of
  per-frame interpolation for baked geometry, and the surface solver quantity
  pass skips contour extraction (`EvaluateQuantities`). Region Stress sample
  bake resolution and tick rate were also coarsened for Play Mode usability.

## [0.14.3] - 2026-08-06

### Added

- **Phase 17:** Package Manager sample **Region Stress** — lean multi-compartment
  first-person integration scene with three `FloodRegion`s (Compartment A,
  multi-deck corridor/stair, Compartment B + irregular sloped baked niche),
  exterior breach, controllable door/hatch `OpenFraction`s, bilge `FloodSink`,
  region occupancy presentation-boundary bakes, and a sample HUD for fills,
  apertures, `FloodTickMetrics.ConservationError`, and closed-system conservation.
- Editor menu **Flooding > Internal > Build Region Stress Sample**.

### Fixed

- `FloodConnection` endpoint validation and snapshot resolution now support
  `FloodRegion` sides (not only `FloodVolume` / `ExternalFluidBoundary`). Fixes
  `InvalidCastException` when an ocean breach targets a region, and allows the
  documented region↔region door/hatch pattern.

### Notes

- Phase 17 proves composition (correctness / authoring friction / smoke
  presentation). Formal performance budgets remain Phase 19; Editor authoring
  UX remains Phase 18. Source-derived smooth boundaries are deferred until the
  sample shows they are necessary.

## [0.14.2] - 2026-08-06

### Added

- **Phase 16T:** `FloodRegionBaker` writes a format-2 occupancy **presentation
  boundary** from exterior faces of the deduplicated region cell union (internal
  shared faces omitted). `BakedFloodGeometry` uses plane ∩ that boundary for
  free-surface contours instead of per-cell voxel patches.
- `OccupancyPresentationBoundaryBuilder` and shared
  `FloodPlanarPolygonTriangulation` (ear clipping) for region and baked surface
  renderers. Hole loops remain unsupported; fan is a last-resort fallback.

### Changed

- Occupancy-backed region/baked presentation remains a **free-surface sheet**
  (not a closed submerged volume). Analytic extruded regions still use clipped
  solid meshes.
- `FloodRegion.QueryPoint` / `SurfacePlane` lazily initialize composite
  geometry when the region is active but not yet built, so edit-mode and
  early tooling queries match Play Mode once membership validates. Failed
  init still reports outside (no silent OR over raw members).
- Documented multi-room gameplay guidance: prefer `FloodRegion.QueryPoint` /
  `FillPercentage` over iterating member `FloodVolume`s
  (`Documentation/components/flood-region.md`,
  `Documentation/editor-workflow.md`, package README / docs index).
- Documented the Path to 1.0 publishing roadmap in repository
  `docs/IMPLEMENTATION_PLAN.md` (0.14 stress sample → 0.15 authoring UX →
  0.16 performance → 0.9x RC → 1.0), with links from package README and docs
  index.

## [0.13.0] - 2026-08-05

### Added

- `FloodSink` manager-mediated configured removal from a finite `FloodVolume`
  (water leaves the simulation). Shares finite supply with connection outflows;
  exposes `FlowRate` (configured max/request), `RequestedFlowRate`, and
  `CurrentFlowRate` (applied last tick).
- `FloodTickMetrics.ConfiguredSinkVolume` (applied) and conservation identity
  `after = before + extIn − extOut + sources − sinks`.
- Symmetrical `FloodSource.RequestedFlowRate` / `CurrentFlowRate` diagnostics.
- Hull Breach sample optional bilge pump (`B` toggles `IsActive`) with
  configured vs actual HUD readout.
- Play Mode sink invariant suite (dry/limited supply, proportional sharing,
  connection competition, same-tick source/sink rules).

### Notes

- Intake submergence, power, and damage remain gameplay concerns that drive
  `IsActive` / `FlowRate`. No egress anchors or A→B pump destinations in this
  release.

## [0.12.0] - 2026-08-05

### Added

- Runtime opening control on `FloodConnection`: `OpenFraction` (0–1
  effective-aperture multiplier), plus `FullOpeningArea` /
  `EffectiveOpeningArea` helpers. Authored `OpeningWidth` / `OpeningHeight`
  remain fully-open geometry for position and submerged-head calculations;
  `IsOpen` remains the hard gate; `DischargeCoefficient` is unchanged.
- Local Ingress sample keys `4` / `5` / `6` set primary-breach aperture to
  25% / 50% / 100%. Ingress presentation continues to follow effective solver
  flow only.
- Edit Mode and Play Mode coverage for fraction 0 / 0.5 / 1, reverse flow,
  exterior depth sensitivity, authored-dimension immutability, and non-finite
  rejection.

### Notes

- Hydraulic sequence: authored geometry → submerged aperture → × `OpenFraction`
  → orifice flow. Suitable for doors, hatches, valves, debris, and damaged
  openings without implying a sliding-door width model.

## [0.11.0] - 2026-08-05

### Changed

- Local ingress visuals: replaced the stretched cube stream with a procedural
  ballistic tapered jet, gravity-curved trajectory, optional impact splash
  particles, directional elongated multi-lobe floor spread, and URP ingress
  jet/patch shaders with scrolling/noise/foam cues. Logical provider-owned patch
  architecture is unchanged.

### Added

- Local ingress presentation: factual `FloodIngressSample`, profile-independent
  `FloodIngressSampler`, `FloodIngressPresentationProfile`, deterministic
  `FloodIngressPresentationState` with Growing / Settling / Converging phases,
  `FloodLocalIngressPresenter`, and lightweight `FloodIngressStreamPresenter`.
- Optional presentation-only `IngressAnchor` on `FloodConnection` and
  `FloodSource`, plus `OpeningCenterWorld` / `IngressWorldPosition` helpers.
  Simulation ignores anchors.
- **Local Ingress** sample with ON/OFF comparison, breach/doorway/leak presets,
  and HUD diagnostics (`Flooding > Internal > Build Local Ingress Sample`).
- Edit Mode lifecycle tests and Play Mode presenter non-mutation tests.

### Notes

- Local ingress is a visual proxy only. Authoritative cubic meters remain on
  `FloodVolume`. Gameplay queries are unchanged. v1 fades local opacity during
  convergence without adding `VisualFillWeight` to `FloodSurfaceRenderer`.

## [0.10.0] - 2026-08-04

### Changed

- Package version and Package Manager sample import paths are now `0.10.0`
  (`Assets/Samples/Flooding/0.10.0/...`).
- `Kyle.Flooding.URP` is now gated with `defineConstraints` /
  `versionDefines` (`KYLE_FLOODING_URP` when Universal RP ≥ 17 is present) so
  the package compiles in Built-in / HDRP projects without URP installed.
  Editor and core Play Mode assemblies no longer hard-reference URP; optional
  underwater effect tests live in `Tests/PlayMode.URP`.
- Underwater fullscreen shader now uses camera-ray / `SurfacePlane`
  intersection with scene depth (open-view waterline) and optical-path-based
  fog/tint strength instead of classifying only the depth-buffer hit against
  the plane.

### Added

- `FloodQueryResult.SurfaceSignedDistanceMeters`: signed distance to the
  authoritative world-space flood surface plane (`> 0` above, `== 0` on,
  `< 0` below). Derived from the same `SurfacePlane` as submersion depth;
  reported even when the sample is outside the compartment.
  `SubmersionDepthMeters` semantics are unchanged.
- `FloodSimulationManager.RegisteredVolumes`: live read-only view of registered
  volumes in registration order. Registration/unregistration remains
  manager-owned.
- `FloodCameraTracker` presentation component with explicit or sticky
  auto-discover volume selection, underwater hysteresis, and C# events
  (`EnteredFloodVolume`, `ExitedFloodVolume`, `EnteredWater`, `ExitedWater`,
  `ActiveVolumeChanged`). Overlapping volumes are ambiguous and not merged.
- `FloodUnderwaterProfile` ScriptableObject for shared underwater presentation
  settings (tint, fog, grading, distortion, transition duration) with pure
  depth/tint/fog evaluation helpers and no runtime state.
- Optional `Kyle.Flooding.URP` assembly with `FloodUnderwaterRendererFeature`,
  `FloodUnderwaterPass`, `FloodUnderwaterCameraEffect`, and
  `Kyle/Flooding/Underwater` fullscreen shader for camera-ray waterline
  crossing, tint/fog/distortion, and optical-path intensity. Requires URP depth
  texture; does not reference URP from the core runtime assembly. Documents
  that the active surface is still an infinite plane (no FloodVolume screen
  masking yet).
- `FloodUnderwaterAudio` presentation component that smooths exposed
  AudioMixer low-pass / volume parameters from `FloodCameraTracker`.
- Framework-neutral `FloodVolumeTelemetry` and `FloodCameraTelemetry` adapters
  for UI bindings (no TextMeshPro dependency).
- **First Person Flooding** sample: enclosed rising flood, first-person
  controls, waterline/underwater presentation wiring, telemetry HUD, and room
  tilt demo (`Flooding > Internal > Build First Person Flooding Sample`).
- Camera/underwater docs: Scenario 9, symptom→field tuning cheat sheet, index
  deep links, and First Person sample look-tuning section.
- MIT License at the repository root; `package.json` declares `"license": "MIT"`.

### Fixed

- `FloodCameraTracker` auto manager resolution no longer permanently gives up
  when no `FloodSimulationManager` exists at first resolve. It retries about
  twice per second and resets on scene load so late-loaded simulation scenes
  are discovered. Enter/exit underwater threshold setters now enforce
  `enter <= exit` at runtime (not only in `OnValidate`).
- `FloodConnection` **Side A**/**Side B** now accept boundary GameObjects from
  the object picker and resolve them to `FloodVolume` or **External Fluid
  Body** components automatically.
- Play Mode `ExternalFluidBoundaryTests`: equalization uses a coarse fill then
  fine settle so orifice flow can reach the head deadband; multiple-breach
  capacity scaling keeps the exterior waterline above the nearly-full tank so
  both openings request inflow.
- Hull Breach sample now presents compartment water with
  `FloodCubeSurfaceRenderer` so rotated compartments keep a gravity-aligned
  free surface. `HullBreachBootstrap` no longer scales a local-Y fill cube and
  reports connection pressure-head difference instead of comparing equivalent
  height to ocean world Y.

### Added

- Gameplay query API on `FloodVolume`: `ContainsPoint`, `IsPointSubmerged`,
  and `QueryPoint` returning `FloodQueryResult` (inside/submerged flags,
  submersion depth, surface point/normal). Queries read live authoritative
  state and never advance, reconcile, or publish simulation.
- `IFloodVolumeGeometry.ContainsLocalPoint` and
  `FloodContainmentPrecision` (`Exact` for prism/polygon,
  `BakeApproximation` for occupancy-cell baked geometry).
- Edit Mode containment coverage and Play Mode `QueryPoint` coverage.
- `FloodConnectionVisual` for optional Transform/particle/mesh presentation
  driven by applied connection flow, submerged area, and direction.
- `FloodConnectionAudio`, `FloodSourceAudio`, and `FloodVolumeAudio` for
  optional spatialized flow and fill ambience driven by measured simulation
  diagnostics.
- `FloodPresentationUtility` intensity helpers and Edit/Play Mode coverage
  proving presentation cannot mutate simulation.
- `ExternalFluidBoundary` (Inspector: **External Fluid Body**) for infinite
  ocean/reservoir endpoints with Transform-authored waterlines and density.
- `IFluidBoundary`, `FluidBoundarySnapshot`, `FluidBoundaryReference`, and
  generalized `FloodConnection` side slots that accept `FloodVolume` or
  `ExternalFluidBoundary`.
- Centroid-based orifice head evaluation, matching-density validation, and
  `FloodTickMetrics` external inflow/outflow accounting on
  `FloodSimulationManager`.
- Hull Breach Package Manager sample demonstrating breach inflow, equalization,
  outflow reversal, and closed-connection behavior.
- Edit Mode density/flow coverage and Play Mode external-boundary coverage.

### Changed

- Baked free surfaces prefer an Editor-baked presentation-boundary mesh
  (format `2`) intersected with the solved gravity plane via shared
  `FloodMeshPlaneIntersection`. Occupancy voxels still drive capacity and plane
  height; format `1` assets keep the voxel-contour fallback.
- Replaced the Baked Geometry stepped chamber with a closed elliptical bowl /
  hull-section source mesh (curved horizontal waterlines), shipped
  `FloodVolumeAuthoring` bake path, `HullSectionFloodVolumeData`, Play Mode
  baked-cell toggle (**B**), pause/roll keys, and a Game-view HUD for capacity,
  fill, resolution, and retained cells.
- Redesigned Flood Mass Integration into a cutaway four-compartment barge with
  visible `FloodCubeSurfaceRenderer` water, Game-view dry/flood/combined COM
  markers, keyboard presets and auto-demo, and HUD mass/attitude readout.
  Renamed sample-only `FloodMassDemoBuoyancy` to `SampleVesselSupport` and
  documented that it is artificial restoring force scaffolding, not buoyancy.
- Expanded package documentation with Getting Started paths and eight
  step-by-step scenario guides (leak, doorway, hull breach, polygon, baked
  geometry, vessel mass, visuals/audio, diagnostics) in the Editor workflow,
  and linked them from the README and documentation index.
- Converted package samples to persistent authored scenes whose
  hierarchies, component references, cameras, lights, and local materials are
  editable before Play Mode.
- Moved Flood Mass Integration tuning to
  `RigidbodyFloodMassAdapter`, the child `FloodVolume` components, and the
  sample-only `SampleVesselSupport`.
- Baked Geometry sample fill/roll remain independently optional; presentation
  now uses the authored hull source mesh plus optional retained-cell mesh, while
  only the gravity-aligned free-surface mesh is generated at runtime.
- Limited Connected Compartments sample scripts to state-driven water and
  readout updates. Flow-direction presentation now uses package
  `FloodConnectionVisual` on the authored connection.
- Documented `Samples~` as authoritative and warned that sample re-import or
  package upgrade can overwrite customized imported copies under
  `Assets/Samples`.
- Improved the **Connected Compartments** sample with transparent and lowered
  walls, stronger water contrast, an elevated orthographic camera, and a live
  world-space applied-flow direction indicator.

### Fixed

- Normalized authored sample camera rotations to prevent
  `QuaternionToEuler` warnings while their Transforms are inspected in Play
  Mode.
- Deferred `FloodDiagnosticsEditor` label-style creation until Scene GUI
  drawing, avoiding editor type-initialization failures before
  `EditorStyles` is ready.

## [0.9.1] - 2026-08-03

### Added

- Optional `FloodDiagnostics` snapshot component and Editor-only Scene-view
  visualization for water/dry/combined centers of mass, active gravity, solved
  surface planes, and connection head/requested/applied flow.
- Deterministic diagnostic derivation tests and Play Mode coverage proving that
  snapshot queries do not change water, connection, or Rigidbody state.
- Repeated-tick invariant coverage for finite-volume bounds, internal-network
  conservation, finite public diagnostics, deterministic trajectories, and
  requested/applied transfer reconciliation.
- Importable **Baked Geometry** sample demonstrating immutable pre-baked
  complex-compartment data and gravity-aligned free-surface presentation.
- Importable **Connected Compartments** sample demonstrating conserved,
  bidirectional pressure-driven flow between two finite volumes.
- Package installation, prefab quick-start, sample import, baked-data,
  connection, diagnostics, and troubleshooting onboarding documentation.

### Changed

- Renamed `FloodGeometryMode.BakedCells` to `BakedData` while preserving its
  serialized integer value `2`; existing scenes and prefabs continue to load.
- Made baked runtime geometry representation-neutral and internal. Gameplay now
  consumes `FloodVolumeData`, `IFloodVolumeGeometry`, and `FloodState`.
- Replaced cell-format asset diagnostics with `SampleCount`,
  `SampleResolution`, and `EstimatedApproximationVolume`; grid and occupied-index
  details are internal.
- `FloodBakedSurfaceRenderer` now consumes `IFloodVolumeGeometry` without a
  concrete baked-geometry cast.
- Specification now defines gravity-aligned surfaces as instantaneous
  equilibrium and limits determinism guarantees to stable tick and
  reconciliation ordering within one runtime.
- Registered Flood Mass Integration, Baked Geometry, and Connected Compartments
  in the Package Manager Samples panel and documented their exact `0.9.1`
  import destinations and Play Mode workflows.

### Removed

- Removed the pre-`1.0.0` public `BakedCellFloodGeometry` source API and the
  `BakedCells` enum source name. See the Editor workflow migration section;
  neither change requires rebaking usable assets.

## [0.9.0] - 2026-08-03

### Added

- Immutable `FloodVolumeData` assets and baked runtime geometry.
- `FloodVolumeAuthoring` with Editor-only closed-mesh validation, center-sampled
  cell baking, stale detection, safety limits, and selected-cell visualization.
- Exact arbitrary-plane volume and centroid clipping for the baked cell union.
- `FloodBakedSurfaceRenderer` for focused free-surface presentation.
- Baked geometry, validation, integration, and deterministic 512-cell solver
  coverage.

### Changed

- `FloodVolume` now supports Baked Data mode without changing serialized
  rectangle or polygon enum values.
- Documentation defines runtime immutability, source-mesh failure behavior,
  cell-resolution semantics, and the non-certified boundary error indicator.

## [0.8.0] - 2026-08-03

### Added

- `IMassContributor`, immutable aggregate results, and deterministic
  mass-weighted center-of-mass calculation.
- `FloodMassAggregator` for child compartment water contributions.
- Optional `RigidbodyFloodMassAdapter` with an authored dry-body baseline and
  restore-on-disable behavior.
- Custom fluid-density configuration API and mass integration tests.
- Importable Flood Mass Integration sample with sample-only spring support.

### Changed

- Package documentation now distinguishes mass-property integration from
  buoyancy and vessel-stability forces.

## [0.7.0] - 2026-08-03

### Added

- Exact arbitrary-plane submerged volume, centroid, and surface-contour queries
  for rectangular and concave polygon prisms.
- Bounded `FloodSurfaceSolver` with absolute, relative, position, and iteration
  tolerances.
- Gravity source selection on `FloodSimulationManager`, using
  `Physics.gravity` or a custom world-space vector.
- Stable near-zero-gravity behavior that retains each volume's last valid local
  surface orientation.
- Plane transformation utilities that preserve half-spaces through rotated and
  non-uniformly scaled transforms.
- Edit Mode solver and clipping tests plus Play Mode rotation, custom gravity,
  zero-gravity, and clipped-rendering tests.

### Changed

- `FloodVolume` now derives its surface plane and submerged centroid from
  authoritative volume and active gravity.
- Connection flow uses the manager's selected gravity magnitude.
- Cube and polygon renderers generate meshes clipped to interpolated
  gravity-aligned surface planes when a Mesh Filter is available.

## [0.6.0] - 2026-08-03

### Added

- `IFloodVolumeGeometry` and immutable submerged-volume, centroid, bounds, and
  surface-contour query results.
- Rectangular-prism and concave extruded-polygon geometry implementations.
- Polygon validation for finite points, duplicate points, winding, area, and
  self-intersections, with documented numerical tolerances.
- Conditional `FloodVolume` Inspector, actionable validation messages, polygon
  reset action, and draggable Scene-view footprint handles.
- `FloodPolygonSurfaceRenderer` for generated polygon water-volume meshes.
- Edit Mode geometry tests and Play Mode polygon integration and presentation
  tests.

### Changed

- `FloodVolume` capacity, floor area, bounds, and center of mass now run through
  the selected geometry abstraction.
- `FloodCubeSurfaceRenderer` is limited to rectangular geometry.

## [0.5.0] - 2026-08-03

### Added

- Bidirectional `FloodConnection` openings between managed rectangular volumes.
- Deterministic orifice-flow calculation from pressure-head difference,
  submerged opening area, discharge coefficient, and gravity.
- Signed requested and applied connection flow diagnostics.
- Scene gizmos and Inspector tooltips for connection authoring.
- Edit Mode flow tests and Play Mode conservation, reversal, closure,
  overdraw, and capacity tests.

### Changed

- `FloodSimulationManager` now reconciles finite-volume outflow and shared
  destination capacity before committing all external and internal transfers.

## [0.4.0] - 2026-08-03

### Added

- `FloodSimulationManager` with a configurable fixed simulation rate.
- Explicit registration for managed flood volumes and sources.
- Snapshot-based source aggregation, capacity reconciliation, simultaneous
  commit, and post-commit state publication.
- Manual tick and elapsed-time advancement APIs for deterministic tests and
  external orchestration.
- Catch-up limits and discarded-tick diagnostics.

### Changed

- `FloodSource` now requests flow during manager ticks instead of mutating its
  target every rendered frame.
- `FloodVolume` publishes state after manager commits rather than polling from
  `LateUpdate`.
- The included flooding prefab now contains a configured simulation manager.

## [0.3.0] - 2026-08-03

### Added

- Presentation-only `FloodSurfaceRenderer` base component.
- Interpolated `FloodCubeSurfaceRenderer` implementation for rectangular
  compartments.
- Play Mode coverage for rendering, interpolation, and simulation/presentation
  separation.

### Changed

- The included flooding prefab now uses `FloodCubeSurfaceRenderer`.
- Rendering consumes immutable `FloodState` snapshots instead of height events.

### Deprecated

- `FloodWaterVisual` remains as a compatibility shim for existing scenes. New
  authoring should use `FloodCubeSurfaceRenderer`.

## [0.2.0] - 2026-08-03

### Added

- Immutable `FloodState` snapshots with volume, capacity, surface, mass, and
  center-of-mass data.
- Exact requested, applied, and rejected quantities for volume mutations.
- Coalesced state and volume change events.
- Configurable water density and volume-based initial state.
- Play Mode coverage for component state and event publication.

### Changed

- Initial water authoring now uses cubic meters instead of height.
- Legacy serialized initial-height values migrate to equivalent volume.
- Core numeric inputs reject non-finite values.

## [0.1.0] - 2026-08-03

### Added

- Volume-authoritative simulation for rectangular compartments.
- Scene components for compartment state, configured inflow, and basic water
  visualization.
- Example flooding and room prefabs.
- URP-compatible transparent floodwater material.
- Edit Mode coverage for core simulation rules.
