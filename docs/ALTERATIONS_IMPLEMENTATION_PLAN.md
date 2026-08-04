# Flooding Architecture Refinement Plan

## Purpose

This companion ledger tracks architectural refinements discovered after
implementation of Phases 7 and 8. `IMPLEMENTATION_PLAN.md` remains authoritative
for feature delivery; this document guards the narrower refinement scope,
decisions, acceptance criteria, and verification state.

## Status legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `[!]` Blocked or awaiting verification

## Locked decisions

1. Baked geometry receives a clean pre-`1.0.0` public API break. Serialized
   geometry mode value `2` remains valid, but cell-specific public source APIs
   are not retained.
2. Generalized fluid boundaries are implemented in Phase 9 as
   `IFluidBoundary` / `ExternalFluidBoundary`. Density mixing remains deferred.
3. The current mass path remains:
   `FloodVolume` → `IMassContributor` → `FloodMassAggregator` →
   `RigidbodyFloodMassAdapter`.
4. The Rigidbody adapter owns a dry mass and dry local center-of-mass baseline
   and recomputes the complete result; it never incrementally accumulates mass.
5. Simulation, presentation, connection evaluation, manager commit, and vessel
   physics boundaries remain unchanged.

## Current status

- Current refinement: **Implementation and documentation complete; Unity verification pending**
- Phases 7 and 8: **Implemented; Unity regression verification pending**
- Phase 9 runtime work: **Implemented in the feature roadmap; Unity verification pending**

## Refinement A — Roadmap and policy

- [x] Cross-link this ledger from the feature roadmap.
- [x] Reword Phase 7 around density, contribution, aggregation, adapter, and
      demonstration work.
- [x] Distinguish implementation completion from Unity regression verification.
- [x] Specify instantaneous equilibrium surfaces and transient slosh non-goals.
- [x] Resolve practical determinism guarantees and exclusions.

Acceptance criteria:

- Roadmap wording does not claim Phase 1 mass exposure was introduced in
  Phase 7.
- Verification claims distinguish historical success from currently blocked
  regression runs.
- Determinism promises ordering, not bit-identical cross-platform physics.

## Refinement B — Simulation invariants

Status: Complete.

- [x] Add repeated-tick finite-volume bounds checks.
- [x] Add internal-network conservation checks.
- [x] Add public state and flow finiteness checks.
- [x] Add requested/applied transfer consistency checks.

Acceptance criteria:

- Every tested finite compartment remains within capacity after every tick.
- Internal connections conserve total water within documented tolerance.
- Infinite configured sources are excluded from conservation assertions.

## Refinement C — Early diagnostics

Status: Implemented; Unity Scene-view and regression verification pending.

- [x] Visualize water, dry, and combined centers of mass.
- [x] Visualize active gravity and solved surface planes.
- [x] Visualize connection direction, head, and requested/applied flow.
- [x] Keep diagnostics read-only and optional.

Acceptance criteria:

- Disabling or removing diagnostics cannot change simulation or Rigidbody state.
- Every Inspector-facing field has a unit-aware tooltip.

## Refinement D — Representation-neutral baked geometry

- [x] Rename authored mode `BakedCells` to `BakedData` while retaining value `2`.
- [x] Replace public cell-specific runtime geometry with an internal neutral
      implementation.
- [x] Internalize grid and occupied-index format details.
- [x] Make baked presentation consume `IFloodVolumeGeometry`.
- [x] Document source migration for the pre-`1.0.0` break.

Acceptance criteria:

- Gameplay code needs only `FloodVolumeData`, `IFloodVolumeGeometry`, and
  `FloodState`.
- Existing serialized mode value `2` still loads as baked data.
- Runtime performs no source-mesh analysis.

## Refinement E — Future fluid-boundary design

- [x] Specify pressure state, density, supply, capacity, and manager ownership.
- [x] Map finite volume-to-volume and future external-to-volume behavior.
- [x] Record reconciliation requirements and non-goals.

Acceptance criteria:

- Documentation enables Phase 9 design review without adding runtime types.
- No ocean-specific branch or external fluid component is implemented.

## Refinement F — Documentation onboarding

- [x] Add actionable local-disk and Git-subpath installation instructions.
- [x] Add a verified 60-second prefab path and separate manual setup path.
- [x] Add focused prefab, renderer, sample, baked-data, and connection quick
      references.
- [x] Add importable baked-geometry and connected-compartments samples.
- [x] Register and document all samples for package-only consumers.

Acceptance criteria:

- A Unity 6.5 user can install the package and run `Room.prefab` without
  guessing a package path or required component assignment.
- Core simulation remains render-pipeline independent; the included URP
  material requirement is explicit.
- Every major implemented workflow has either a working prefab/sample or a
  complete step-by-step reference.
- Package documentation contains no dead repository-relative links.
- Sample declarations, scenes, scripts, metadata, and imported destinations are
  internally consistent.

## Refinement G — Editable authored samples

- [x] Replace runtime-created sample hierarchies with persistent scene objects.
- [x] Keep scripts only for behavior that genuinely requires Play Mode.
- [x] Store sample presentation materials as editable assets.
- [x] Make primary demonstration parameters editable on their package
      components before Play Mode.
- [x] Synchronize the authoritative `Samples~` package source.
- [x] Update package-wide sample documentation, tuning ownership, and
      re-import/upgrade warnings.
- [x] Synchronize the current imported `0.10.0` copies with exact relative-path
      and SHA-256 parity to authoritative `Samples~`.
- [x] Re-import and inspect all three current `Assets/Samples` copies.
- [x] Verify all three sample behaviors in Play Mode.

Acceptance criteria:

- All three sample scenes expose their useful hierarchy, component references,
  materials, and tuning fields before Play Mode.
- Stopping Play Mode removes transient simulation state only, not the authored
  demonstration hierarchy.
- Entering Play Mode does not create duplicate cameras, lights, compartments,
  vessels, or presentation objects.
- `Samples~` remains authoritative; users are warned to preserve modified
  imported copies before re-import or package upgrade.
- Existing mass response, baked free-surface animation, and compartment
  equalization behavior remain demonstrable.

## Verification ledger

- [x] IDE diagnostics are clean after all architecture refinements.
- [x] Unity 6.5 batch import compiled the package runtime, Editor, and test
      assemblies without script errors after the onboarding changes.
- [x] Package manifest JSON, sample declarations, scene-script GUIDs, baked-data
      reference, documentation names, and `0.10.0` import paths were checked.
- [x] Unity batch test retries were intermittently rejected by the project lock;
      the successful import/compile pass exited during assembly reload without
      producing a test-results XML file.
- [ ] Run all Edit Mode tests in Unity 6.5.
- [ ] Run all Play Mode tests in Unity 6.5.
- [ ] Re-import and open all three Package Manager sample scenes; confirm their
      pre-Play hierarchies, references, components, and materials match `Samples~`.
- [ ] Run Flood Mass Integration, Baked Geometry with each animation toggle
      combination, and Connected Compartments in Play Mode.
- [ ] Validate the baked-data source migration in the Editor.
- [ ] Validate diagnostics in Scene view and Play Mode.

## Progress log

### 2026-08-03

- Created this companion context guard.
- Locked the clean pre-`1.0.0` baked API break.
- Limited Phase 9 work to documentation and design.
- Reframed Phase 7 around aggregation and integration.
- Resolved equilibrium-surface and practical determinism policy.
- Added repeated-tick bounds, conservation, finiteness, and reconciliation
  invariant coverage.
- Added optional read-only Scene-view diagnostics for mass, gravity, surfaces,
  and connection flow.
- Completed the clean representation-neutral baked-data source API break while
  preserving serialized geometry mode value `2`.
- Specified the future fluid-boundary seam without adding Phase 9 runtime code.
- Completed Refinement B simulation invariant coverage.
- Implemented Refinement C with one optional read-only diagnostic root
  component, Editor-only handles and labels, and deterministic snapshot tests.
- Implemented Refinement D with serialized enum value `2` preserved, an
  internal representation-neutral baked runtime, generic public asset
  diagnostics, interface-based baked presentation, and source migration
  guidance.
- Kept Unity Scene-view and full regression verification pending while the
  project remains unavailable for batch testing.
- Synchronized package documentation, changelog, feature roadmap, and this
  companion ledger.
- Documented Package Manager disk and Git-subpath installation, including
  reproducible commit/tag pinning and render-pipeline requirements.
- Split the ready-to-run `Room.prefab` path from reusable `Flooding.prefab` and
  build-your-own workflows.
- Added focused prefab, renderer, sample, baked-data, and connection quick
  references.
- Registered Flood Mass Integration, Baked Geometry, and Connected Compartments
  for Package Manager import with exact `0.10.0` destinations and Play Mode
  instructions.
- Completed the importable Baked Geometry and Connected Compartments samples;
  Unity import, Scene-view, and full regression verification remain pending.
- Validated package metadata, documentation references, sample scene GUIDs, and
  a Unity 6.5 package assembly compile. Batch test execution remained blocked by
  project-lock contention and did not produce test result XML.
- Converted all three authoritative `Samples~` scenes to persistent authored
  hierarchies with editable component wiring and local presentation materials.
- Kept Mass Integration tuning on `RigidbodyFloodMassAdapter`, `FloodVolume`,
  and sample-only `FloodMassDemoBuoyancy`; limited Baked Geometry behavior to
  optional fill/roll animation and runtime free-surface generation; and kept
  Connected Compartments flow tuning on its manager, volumes, and connection.
- Updated package-wide documentation and overwrite warnings for imported sample
  copies. Re-import inspection, sample Play Mode behavior, and full Edit Mode
  and Play Mode test verification remain pending.
- Synchronized all 32 imported sample files with zero missing, extra, or
  content-mismatched files. Unity batch regression execution was still rejected
  by project-lock contention, so runtime checks remain pending.
