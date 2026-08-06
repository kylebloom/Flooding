# Region Stress sample

Unity 6.5 Package Manager sample for **Phase 17 / 0.14.3**. It proves that
multiple `FloodRegion`s, mixed member geometry, a multi-deck continuous region,
controllable inter-region connections, exterior flooding, pumping, first-person
immersion, and region presentation **compose** in one representative scenario.

This sample answers correctness, composition, and authoring-friction questions.
It is not a performance budget suite (Phase 19) and does not add Editor tooling
(Phase 18).

## Import and open

1. **Window > Package Management > Package Manager > Flooding > Samples**
2. Import **Region Stress**
3. Open `RegionStress.unity` from the imported folder
4. Enter Play Mode

> Re-importing can replace the copy under `Assets/Samples`. Rename a local copy
> before re-import if you want to keep edits.

### Rebuild from the Editor menu

After the bootstrap script compiles (imported sample or package `Samples~`):

**Flooding > Internal > Build Region Stress Sample**

The builder regenerates the greybox scene, materials, irregular baked niche,
and region occupancy bakes, then mirrors assets into the package sample folder.

## Topology

```text
Exterior ocean
      │ breach (default OpenFraction 60%)
      ▼
┌─ Region A (Compartment A) ─────────┐
│ RoomA prism + overlapping alcove   │── door (default 25%)
└────────────────────────────────────┘
                    │
                    ▼
┌─ Region Corridor/Stair (ONE region) ┐
│ Upper corridor                     │
│ Upper landing                      │
│ Descending stair shaft             │
│ Lower landing                      │
└──────────────┬─────────────────────┘
               │ hatch (default 100%)
               ▼
┌─ Region B (Compartment B) ─────────┐
│ RoomB prism                        │
│ Irregular baked hull niche (slope) │
│ FloodSink pump (starts OFF)        │
└────────────────────────────────────┘
```

**Architectural point under test:** upper corridor, stair, and lower landing
share one `FloodRegion` and therefore one equilibrium surface plane. The hatch
into Compartment B is a separate hydraulic boundary (`FloodConnection`), not a
region merge.

## Controls

| Input | Action |
| --- | --- |
| WASD / mouse | Move / look |
| Esc / click | Unlock / relock cursor |
| `1` / `2` / `3` | Breach aperture 25% / 60% / 100% |
| `4` / `5` / `6` | Door aperture 0% / 25% / 100% |
| `7` / `8` / `9` | Hatch aperture 0% / 50% / 100% |
| `B` / `D` / `H` | Toggle breach / door / hatch `IsOpen` |
| `P` | Toggle pump `IsActive` |
| `C` | Closed-system mode (close breach, disable pump; capture baseline volume) |
| `T` | Toggle vessel pitch/roll tilt |
| `R` | Drain all regions |

## Recommended Play Mode scenarios

### 1. Partial apertures under unequal head

Leave defaults (breach 60%, door 25%, hatch 100%, pump off). Let Region A fill
from the ocean, then walk the corridor/stair while levels diverge. Change
apertures with `1`–`9` while the network is far from equilibrium. Watch competing
flows, region fills, and presentation update together.

### 2. Closed-system conservation

After meaningful water has entered:

1. Press `C` (closes breach, disables pump, records total finite volume).
2. Open/close the door and hatch (`D`/`H` or aperture keys).
3. Confirm **Total finite** stays near the closed baseline and
   **ConservationError** stays near zero while water redistributes.

### 3. Multi-deck continuous region

Stand in the upper corridor and lower landing of the stair region. Confirm one
shared free-surface plane through the oddly shaped vertical union (not separate
compartment heights).

### 4. Irregular baked niche (question 9)

In Compartment B, inspect the sloped/curved baked niche waterline. Decide whether
voxel presentation boundaries are objectionable enough to justify source-derived
smooth boundaries later.

### 5. First-person immersion

Walk dry → waterline → submerged. Confirm camera underwater state and (with URP
depth + underwater feature) waterline transition agree with the rendered surface.
Press `T` and check surface stability under tilt.

## Evaluation checklist

Record yes/no (or notes) after a Play Mode session:

- [ ] Authoring 5–10 members across 3 regions remained understandable
- [ ] Region bake diagnostics were usable when geometry changed (Inspector stale)
- [ ] Connections behaved intuitively across region boundaries
- [ ] Surface remained visually stable under pitch/roll (`T`)
- [ ] Underwater transition agreed with the rendered surface
- [ ] Artifacts at doorways / stairs / concave / overlaps (note where)
- [ ] Rebaking was painful or acceptable
- [ ] Presentation mesh rebuild cost felt fine / spiky / unusable (smoke only)
- [ ] Voxel boundary on the irregular niche justifies source-derived CSG now?
- [ ] Partial apertures under unequal head behaved sanely
- [ ] Closed-system conservation held after `C`

Failures and sharp edges should become Phase 18 / 19 follow-ups, not silent debt.

## Authored hierarchy (summary)

```text
Region Stress Demo
  FloodSimulationManager
  FloodDiagnostics
  RegionStressBootstrap
  Vessel
    External Ocean
    Region A …
    Region Corridor/Stair …
    Region B … (+ pump)
    Connections (Breach, Door, Hatch)
    Player (CharacterController + camera stack)
  Main Camera is on Player
  Directional Light
```

Each multi-member region uses `FloodRegionSurfaceRenderer` (not stacked member
renderers) and a baked `FloodRegionData` asset with format-2 presentation
boundary. Occupancy surfaces snap on simulation ticks (no per-frame mesh
interpolation) so Play Mode stays interactive; cell resolution is intentionally
coarser (~0.55 m) than a final ship bake.

## Out of scope

- Source-derived smooth presentation boundaries
- Hole-loop triangulation
- Closed submerged occupancy meshes
- Package Editor authoring/debug UX (Phase 18)
- Formal timing hooks or performance budgets (Phase 19)
