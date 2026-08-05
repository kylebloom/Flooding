# FloodRegion occupancy / bake design (Phase 16S)

**Roadmap status:** delivered in Phase 16S / package `0.14.1` — see
[`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md).

Design reference for the shipped bake. Do not implement runtime mesh CSG. Do not
silently voxelize analytic volumes in gameplay scenes; bake is an explicit
Editor action per `FloodRegion`.

## Goal

Generalize region union beyond the two-box analytic prototype:

```text
Member geometry
      ↓
Region-local occupancy (FloodRegionData)
      ↓
deduplicated cell union
      ↓
capacity / volume-below-plane / centroid / containment / surface
```

## Why

- Baked volume cells today have **per-asset local identity** and cannot be
  unioned by index across `FloodVolume`s.
- Inclusion-exclusion is exact for two AABBs but combinatorial for N members
  and awkward for mixed geometry modes.
- A region-local grid remaps all members into one frame so overlapping cells
  exist once.
- Authors currently cannot put three overlapping rooms in one region; Phase 16S
  removes that ceiling.

## Proposed asset: `FloodRegionData`

Immutable ScriptableObject (Editor-baked), analogous to `FloodVolumeData`:

| Field | Role |
| --- | --- |
| `localBounds` | Region-frame AABB |
| `cellSize` / `gridSize` | Region grid |
| `occupiedCellIndices` | Sorted unique flattened indices (union) |
| `capacity` | `cellVolume × count` |
| `centroid` | Mean occupied cell centers |
| optional presentation boundary | Merged or source-combined mesh in region space |

## Bake inputs

- Region transform (bake-space frame)
- Member list + each member’s geometry mode
- For rectangular / extruded: sample `ContainsLocalPoint` after transforming
  sample points into member local space (exact containment, approximate
  occupancy representation)
- For baked members: transform each occupied cell center into region space and
  mark the containing region cell
- Requested cell resolution + max cell count safety limit

Authors opt in via an explicit **Bake Region** Editor action — never as a
hidden conversion of every analytic volume.

## Runtime strategy

```text
IRegionUnionStrategy
  ├─ TwoBoxAnalyticUnionStrategy     (keep for eligible two-box cases)
  └─ RegionOccupancyUnionStrategy    (consumes FloodRegionData)
```

`CompositeFloodGeometry` selects the strategy; `FloodRegion` stays unaware of
IE vs occupancy internals.

## Precision policy

- Analytic standalone volumes remain exact.
- Region occupancy bake is exact for the **cell union**, approximate vs source
  solids (same contract as `FloodVolumeData`).
- Prefer keeping two-box analytic strategy available when eligible so simple
  doorway prototypes do not lose precision.

## Out of scope

- Runtime arbitrary mesh boolean / CSG
- Dynamic region merge when a door opens
- Auto membership from overlap
