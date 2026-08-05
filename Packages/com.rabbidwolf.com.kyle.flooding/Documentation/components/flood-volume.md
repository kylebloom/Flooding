# FloodVolume

`FloodVolume` is a finite compartment with authoritative water volume state.
Height, mass, center of mass, and surface plane are derived.

## Use this when

- You need a floodable compartment with finite capacity.
- Gameplay needs point queries (inside/submerged/depth).
- You need one of three geometry modes: rectangular, polygon, or baked data.

## Geometry modes

- **Rectangular Prism**: fastest setup, axis-aligned in local compartment space.
- **Extruded Polygon**: custom floor footprint with vertical walls.
- **Baked Data**: complex interior from pre-baked closed mesh samples.

## Beginner setup (rectangular)

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

## Key Inspector fields

- **Simulation Manager**: shared manager.
- **Geometry Mode**: Rectangular/Polygon/Baked Data.
- **Water Density**: kg/m^3.
- **Initial Volume**: starting cubic meters.
- Geometry-specific fields appear by selected mode.

## Verification checklist

1. Enter Play Mode with one active source.
2. Confirm `CurrentVolume` rises and does not exceed capacity.
3. Confirm visual renderer is assigned and visible above minimum threshold.

## Common mistakes

- Using a cube renderer with non-rectangular geometry.
- Invalid polygon footprint (self-intersections, duplicate points).
- Stale or missing baked data assignment.

## Runtime API notes

- Read-only state: `CurrentState`, `CurrentVolume`, `FillPercentage`.
- Mutations: `AddWater`, `RemoveWater`, `AddWaterOverTime`,
  `RemoveWaterOverTime`.
- Queries: `ContainsPoint`, `IsPointSubmerged`, `QueryPoint`.
