# FloodVolume

`FloodVolume` authors floodable geometry for one scene compartment and exposes
gameplay queries.

- **Standalone** (not a region member): also owns authoritative water volume
  state. Height, mass, center of mass, and surface plane are derived.
- **Region member**: geometry + query facade only. Water state
  (`CurrentVolume`, `InitialVolume`, shared `SurfacePlane`) comes from the
  owning [`FloodRegion`](flood-region.md).

See also: [FloodRegion](flood-region.md) for multi-room continuous flooding
(two-box analytic or **Bake Region** / `FloodRegionData` for N members and mixed
modes).

## Use this when

- You need floodable geometry with finite capacity.
- Gameplay needs point queries (inside / submerged / depth).
- You need one of three geometry modes: rectangular, polygon, or baked data.
- You are composing rooms into a `FloodRegion` (members still use this
  component for geometry).

## Geometry modes

- **Rectangular Prism**: fastest setup, axis-aligned in local compartment space.
  Required for the current two-member region union prototype.
- **Extruded Polygon**: custom floor footprint with vertical walls.
- **Baked Data**: complex interior from pre-baked closed mesh samples.

## Beginner setup (rectangular, standalone)

1. Create GameObject `Compartment` under `Flood System`.
2. Add **Flood Volume**.
3. Set:
   - **Geometry Mode**: `Rectangular Prism`
   - **Width**: `5`
   - **Length**: `5`
   - **Maximum Height**: `3`
   - **Water Density**: `1000`
   - **Initial Volume**: `0`
4. Add a surface renderer (`FloodCubeSurfaceRenderer`) and water visual child.
   See [surface renderers](../presentation/surface-renderers.md).

## Region member setup

1. Create the volume under (or assigned to) a `FloodRegion` GameObject.
2. Configure geometry and matching **Water Density**.
3. Assign the volume on the region **Members** list.
4. Set water start amount on **FloodRegion → Initial Volume** (this volume’s
   **Initial Volume** is ignored while bound).
5. Prefer [`FloodRegionSurfaceRenderer`](flood-region.md#presentation) on the
   region. Disable any `FloodSurfaceRenderer` on the member to avoid double
   drawing.

## Key Inspector fields

- **Simulation Manager**: shared manager (region members report the owning
  region’s manager when bound).
- **Geometry Mode**: Rectangular / Polygon / Baked Data.
- **Water Density**: kg/m³.
- **Initial Volume**: starting cubic meters for **standalone** volumes only.
  Ignored while this volume is a `FloodRegion` member.
- Geometry-specific fields appear by selected mode.

## Query semantics

| API | Meaning |
| --- | --- |
| `ContainsPoint(p)` | Inside **this volume’s authored geometry** |
| `QueryPoint(p)` | If outside this volume → outside; else water / depth / plane from the owning region when bound, otherwise this volume |
| `IsPointSubmerged(p)` | Inside this volume and below the active surface plane |
| `OwningRegion` | Non-null when bound to a region |
| `IsRegionMember` | Convenience flag for membership |

Region-wide containment uses `FloodRegion.ContainsPoint` /
`FloodRegion.QueryPoint` (composite union).

## Verification checklist

1. Enter Play Mode with one active source targeting this volume.
2. Confirm `CurrentVolume` rises and does not exceed capacity (standalone
   capacity, or the owning region’s capacity when a member).
3. Confirm the assigned surface renderer is visible above its minimum threshold.
4. If this volume is a region member, confirm `OwningRegion` is set and member
   `Initial Volume` does not control Play Mode start water.

## Common mistakes

- Using a cube renderer with non-rectangular geometry.
- Invalid polygon footprint (self-intersections, duplicate points).
- Stale or missing baked data assignment.
- Leaving member `FloodSurfaceRenderer`s enabled under a `FloodRegion`
  (causes seams / double transparency).
- Expecting overlapping standalone volumes to merge water — they do not.
  Use an explicit `FloodRegion`.

## Runtime API notes

- Read-only state: `CurrentState`, `CurrentVolume`, `FillPercentage`,
  `SurfacePlane` (region-owned when a member).
- Mutations: `AddWater`, `RemoveWater`, `AddWaterOverTime`,
  `RemoveWaterOverTime` (forward to the owning region when bound).
- Queries: `ContainsPoint`, `IsPointSubmerged`, `QueryPoint`.
- Membership: `OwningRegion`, `IsRegionMember`.
