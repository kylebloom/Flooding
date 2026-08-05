# FloodRegion

`FloodRegion` is an independently simulated equilibrium water body composed from
one or more explicit [`FloodVolume`](flood-volume.md) members.

It owns `CurrentVolume`, `InitialVolume`, capacity of the composite geometry,
and one shared free-surface plane. Member volumes keep spatial identity for
geometry and `ContainsPoint`, but water depth / plane / fill come from the
region.

## Role in the package model

```text
FloodVolume     = authored floodable geometry (+ query facade)
FloodRegion     = one shared CurrentVolume + one SurfacePlane
FloodConnection = hydraulic restriction BETWEEN regions
```

Membership is **authoring truth**. Geometry validates overlap or face-sharing
within tolerance; it does not invent membership. Opening a door must use a
[`FloodConnection`](flood-connection.md) between two regions — it does **not**
merge topology at runtime.

## Use this when

- Multiple rooms / a doorway should look and behave as **one** continuous water
  body (first-person continuity).
- You need correct union capacity (overlap counted once).
- You want one equilibrium free surface across composed geometry.

## Do not use this when

- Two spaces are separated by a watertight or controllable door/valve.
  Keep **two regions** and link them with a `FloodConnection`.
- You only have a single simple compartment — a standalone `FloodVolume` is
  enough.

## When to use which pattern

| Scenario | Authoring |
| --- | --- |
| Unrestricted doorway / open corridor | One `FloodRegion` with overlapping or face-touching room (+ optional doorway) volumes |
| Watertight / controllable door | Two `FloodRegion`s + `FloodConnection` |
| Single room | Standalone `FloodVolume` (no region required) or one-member region |

## Minimum setup

1. Create an empty GameObject for the region (defines the region-local frame).
2. Add **Flood Region**.
3. Create child `FloodVolume` members.
   - Current multi-member union supports **exactly two Rectangular Prism**
     members that are axis-aligned with the region.
   - One-member regions reuse that member’s geometry (any mode), with matching
     transforms for parity.
4. Assign members on the region **Members** list. Do not rely on automatic
   overlap discovery.
5. Set **Initial Volume** on the **region** (member Initial Volume is ignored).
6. Match **Water Density** across region and members.
7. Add presentation (see below).
8. Assign the same [`FloodSimulationManager`](flood-simulation-manager.md) (or
   parent the hierarchy under one).

## Presentation

Continuous first-person water requires a **region-level** surface, not stacked
member renderers. Full field tables and verification:
[FloodRegionSurfaceRenderer](../presentation/surface-renderers.md#floodregionsurfacerenderer).

1. Add **Flood Region Surface Renderer** on the region GameObject (or child).
2. Assign a child Transform with a **Mesh Filter** as the water visual.
3. Disable any `FloodCubeSurfaceRenderer` /
   `FloodPolygonSurfaceRenderer` /
   `FloodBakedSurfaceRenderer` on member volumes.

Member surface renderers that remain enabled log a warning: they will double-draw
and reintroduce seams.

## Key Inspector fields

| Field | Meaning |
| --- | --- |
| Simulation Manager | Tick owner; nearest parent manager if unset |
| Members | Explicit `FloodVolume` list (authoring truth) |
| Water Density | kg/m³; must match all members |
| Initial Volume | Authoritative m³ at Play Mode start |

## Query semantics

```csharp
member.ContainsPoint(p);  // inside that member's authored geometry only
member.QueryPoint(p);     // member containment + region water/depth/plane
region.ContainsPoint(p);  // inside the composite union
region.QueryPoint(p);     // union containment + region water state
```

## Connections / sources / sinks

Gameplay and Inspector targets may still reference a member `FloodVolume`
(for example a leak Transform inside Room A). Hydraulic mutations resolve to the
owning region through `EffectiveFluidBoundary`.

- [`FloodSource`](flood-source.md) / [`FloodSink`](flood-sink.md) **Target** may
  be a member volume; water is added/removed from the region.
- [`FloodConnection`](flood-connection.md) endpoints may reference member
  volumes; snapshots and commits resolve to each side’s effective region /
  commit participant.

A `FloodConnection` whose endpoints resolve to the **same** region is an
authoring **error**, for example:

```text
ERROR: FloodConnection "WatertightDoor" resolves both endpoints to FloodRegion "Basement".
FloodConnection may only connect independently simulated regions.
```

## Geometry notes (current)

- Capacity and volume-below-plane for two rectangular members use
  `CompositeFloodGeometry` → `TwoBoxAnalyticUnionStrategy` (exact
  inclusion-exclusion prototype strategy).
- Overlap is counted once; partial fill applies the same plane to all IE terms.
- Face-touching (shared face within tolerance) is valid continuity — authors
  need not intentionally penetrate rooms.
- Disconnected members fail validation.
- General region-local occupancy / `FloodRegionData` baking is planned; analytic
  volumes are not silently voxelized. See package
  `docs/FLOOD_REGION_OCCUPANCY_DESIGN.md` in the repository.

## Verification checklist

1. Enter Play Mode with region **Initial Volume** set (for example half capacity).
2. Confirm `FloodRegion.CurrentVolume` and every member’s `CurrentVolume` match.
3. Confirm one continuous surface from `FloodRegionSurfaceRenderer` across both
   rooms / the doorway.
4. Confirm `roomA.ContainsPoint` is false for a point only inside Room B, while
   `region.ContainsPoint` is true for both.
5. Confirm a source targeting Room A raises the shared region volume.
6. Confirm a connection between two members of the **same** region fails
   validation.

## Common mistakes

- Putting both sides of a closed door in one region (use two regions +
  connection).
- Leaving member surface renderers enabled.
- Setting member **Initial Volume** and expecting it to apply.
- Non-rectangular or non-axis-aligned members for the two-volume union
  prototype.
- Expecting opening a `FloodConnection` to merge regions at runtime.

## Runtime API notes

- State: `CurrentVolume`, `MaximumVolume`, `FillPercentage`, `CurrentState`,
  `SurfacePlane`, `InitialVolume`.
- Mutations: `AddWater`, `RemoveWater`, `ConfigureInitialVolume`.
- Queries: `ContainsPoint`, `IsPointSubmerged`, `QueryPoint`.
- Membership: `Members`, `BoundMembers`, `SetMembers`, `Rebuild`.
- Events: `StateChanged`, `VolumeChanged`, `WaterHeightChanged`.
