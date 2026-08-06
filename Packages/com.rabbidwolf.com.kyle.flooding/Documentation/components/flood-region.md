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
| Three+ rooms or mixed geometry modes | One `FloodRegion` + **Bake Region** → `FloodRegionData` |
| Watertight / controllable door | Two `FloodRegion`s + `FloodConnection` |
| Single room | Standalone `FloodVolume` (no region required) or one-member region |

## Minimum setup

1. Create an empty GameObject for the region (defines the region-local frame).
2. Add **Flood Region**.
3. Create child `FloodVolume` members and assign them on the region **Members**
   list. Do not rely on automatic overlap discovery.
4. Choose a geometry path:
   - **One member:** uses that member’s geometry (any mode). Region and member
     transforms must match for parity.
   - **Two Rectangular Prism members** axis-aligned with the region: optional
     exact `TwoBoxAnalyticUnionStrategy` (no bake required).
   - **N members, extruded, baked, or mixed modes:** set **Cell Resolution** /
     **Maximum Grid Cells**, click **Bake Region**, and save a
     `FloodRegionData` asset.
5. Set **Initial Volume** on the **region** (member Initial Volume is ignored).
6. Match **Water Density** across region and members.
7. Add presentation (see below).
8. Assign the same [`FloodSimulationManager`](flood-simulation-manager.md) (or
   parent the hierarchy under one).

## Bake Region (FloodRegionData)

Editor bake remaps every member into one region-local occupancy grid so
overlapping cells exist once. Runtime never runs mesh CSG and never silently
voxelizes analytic volumes — bake is an explicit Inspector action.

| Member mode | Bake step |
| --- | --- |
| All modes | Sample region cell centers through member `ContainsLocalPoint` |
| Baked Data (extra) | Also remap each member occupied cell center into a region cell |

After a successful bake:

- Capacity = occupied cell count × cell volume (exact for the cell union;
  approximate vs source solids).
- Continuity requires one face-connected occupied component (overlap or
  face-touch within the region grid).
- Stale diagnostics fire when members, transforms, geometry, or Cell Resolution
  change.

Clear the baked asset reference to fall back to the two-box analytic path when
eligible. Prefer keeping two-box for simple doorway prototypes when you need
exact inclusion-exclusion precision.

Full bake workflow: [Editor workflow — Bake a FloodRegion](../editor-workflow.md#bake-a-floodregion-occupancy-union).

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

Supported presentation sources:

- Two-box presentation footprint when using the analytic strategy
- Extruded composite footprints
- Occupancy bake (voxel free-surface contours; optional presentation boundary
  when present on the asset)

## Key Inspector fields

| Field | Meaning |
| --- | --- |
| Simulation Manager | Tick owner; nearest parent manager if unset |
| Members | Explicit `FloodVolume` list (authoring truth) |
| Baked Region Data | Optional `FloodRegionData` asset from **Bake Region** |
| Cell Resolution | Requested max region cell edge (m) for bake |
| Maximum Grid Cells | Bake safety limit on inspected cells |
| Visualize Bake | Draw occupied region cells when selected |
| Water Density | kg/m³; must match all members |
| Initial Volume | Authoritative m³ at Play Mode start |

## Query semantics

```csharp
member.ContainsPoint(p);  // inside that member's authored geometry only
member.QueryPoint(p);     // member containment + region water/depth/plane
region.ContainsPoint(p);  // inside the composite union
region.QueryPoint(p);     // union containment + region water state
```

For multi-room gameplay (movement slowdown, depth HUD, etc.), query the
**region**, not each member. A point in Room B is outside Room A's member
geometry even when both share one region water surface.

`QueryPoint` / `ContainsPoint` / `IsPointSubmerged` lazily initialize composite
geometry when the region is active but has not built yet (for example edit-mode
tooling before Play Mode `Awake`). If geometry validation fails (disconnected
members, bake required, etc.), queries report outside rather than inventing
membership. Check `ValidationMessage` / the Console when region queries always
return outside.

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

## Geometry notes

`CompositeFloodGeometry` selects a strategy:

1. Usable assigned `FloodRegionData` → `RegionOccupancyUnionStrategy`
2. Else exactly two rectangular axis-aligned members →
   `TwoBoxAnalyticUnionStrategy` (exact inclusion-exclusion)
3. Else authoring error (bake required)

- Overlap is counted once (IE terms or deduplicated region cells).
- Face-touching (shared face within tolerance / face-adjacent cells) is valid
  continuity — authors need not intentionally penetrate rooms.
- Disconnected members fail validation / bake.
- Analytic standalone volumes remain exact; region bake is opt-in per region.

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
7. For N-member or mixed-mode regions: confirm **Bake Region** succeeded and
   the Inspector shows a current (non-stale) bake.

## Common mistakes

- Putting both sides of a closed door in one region (use two regions +
  connection).
- Leaving member surface renderers enabled.
- Setting member **Initial Volume** and expecting it to apply.
- Three+ members or mixed modes without clicking **Bake Region**.
- Expecting opening a `FloodConnection` to merge regions at runtime.

## Runtime API notes

- State: `CurrentVolume`, `MaximumVolume`, `FillPercentage`, `CurrentState`,
  `SurfacePlane`, `InitialVolume`.
- Mutations: `AddWater`, `RemoveWater`, `ConfigureInitialVolume`.
- Queries: `ContainsPoint`, `IsPointSubmerged`, `QueryPoint`.
- Membership: `Members`, `BoundMembers`, `SetMembers`, `Rebuild`.
- Bake: `BakedRegionData`, `ConfigureBakeSettings`, `AssignBakedRegionData`.
- Events: `StateChanged`, `VolumeChanged`, `WaterHeightChanged`.
