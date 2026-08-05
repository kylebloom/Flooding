# FloodRegion

Independently simulated equilibrium water body composed from one or more
explicit `FloodVolume` members.

## Role

```text
FloodVolume     = authored floodable geometry (+ query facade)
FloodRegion     = one shared CurrentVolume + one SurfacePlane
FloodConnection = hydraulic restriction BETWEEN regions
```

Membership is authoring truth. Geometry validates overlap or face-sharing; it
does not invent membership. Opening a door must use a `FloodConnection` between
two regions — it does not merge topology.

## When to use

| Scenario | Authoring |
| --- | --- |
| Unrestricted doorway / open corridor | One `FloodRegion` with overlapping or face-touching room (+ optional doorway) volumes |
| Watertight / controllable door | Two `FloodRegion`s + `FloodConnection` |
| Single room | Standalone `FloodVolume` (no region required) or one-member region |

## Minimum setup

1. Create an empty GameObject for the region (defines the region-local frame).
2. Add `FloodRegion`.
3. Create child `FloodVolume` members (rectangular for the current two-box union).
4. Assign members on the region. Do not rely on automatic overlap discovery.
5. Set **Initial Volume** on the **region** (member Initial Volume is ignored).
6. Match water density across region and members.
7. Add `FloodRegionSurfaceRenderer` + a child Mesh Filter visual for continuous
   presentation.
8. Disable any `FloodSurfaceRenderer` components on member volumes.

## Inspector fields

| Field | Meaning |
| --- | --- |
| Simulation Manager | Tick owner; parent manager if unset |
| Members | Explicit `FloodVolume` list |
| Water Density | kg/m³; must match members |
| Initial Volume | Authoritative m³ at Play Mode start |

## Query semantics

```csharp
member.ContainsPoint(p);  // inside that member's authored geometry only
member.QueryPoint(p);     // member containment + region water/depth/plane
region.ContainsPoint(p);  // inside the composite union
region.QueryPoint(p);     // union containment + region water state
```

## Connections / sources / sinks

Targets may still reference a member `FloodVolume`. Mutations resolve to the
owning region via `EffectiveFluidBoundary`.

A `FloodConnection` whose endpoints resolve to the **same** region is an
authoring **error**.

## Geometry notes (current)

- One-member regions reuse the member geometry (parity with standalone volumes
  when transforms match).
- Two rectangular members use `CompositeFloodGeometry` →
  `TwoBoxAnalyticUnionStrategy` (exact inclusion-exclusion prototype).
- General occupancy / `FloodRegionData` baking is planned; analytic volumes are
  not silently voxelized.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Region invalid / zero capacity | Members assigned? Rectangular? Axis-aligned with region? Overlap or face-touch? |
| Double transparent water | Disable member `FloodSurfaceRenderer`; use `FloodRegionSurfaceRenderer` |
| Connection error naming a region | Endpoints are in the same region — split into two regions for a door |
| Member Initial Volume ignored | Expected — set `FloodRegion.Initial Volume` |
