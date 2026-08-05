# FloodConnection

`FloodConnection` is pressure-driven exchange through a rectangular opening
between two independently simulated fluid boundaries.

Endpoints are authored as `FloodVolume` or `ExternalFluidBoundary`. When a
`FloodVolume` is a [`FloodRegion`](flood-region.md) member, hydraulic evaluation
and commits resolve to that region via `EffectiveFluidBoundary`.

Conceptual role:

```text
FloodConnection = hydraulic restriction BETWEEN FloodRegions
                  (or between a region/volume and an ExternalFluidBoundary)
```

## Use this when

- You need conserved transfer between separately solved water bodies.
- You need exchange with an infinite exterior waterline.
- Flow direction should follow head difference automatically.
- A doorway / hatch / valve / pipe is a **restriction** (including a door that
  can open and close).

## Do not use this when

- The opening is permanently unrestricted and should look like one continuous
  water body. Prefer one [`FloodRegion`](flood-region.md) with overlapping or
  face-touching member volumes instead.
- Both endpoints belong to the **same** `FloodRegion` — that is an authoring
  error.

## Beginner setup

1. Create GameObject `Door Connection` under `Flood System`.
2. Position Transform at opening bottom center.
3. Keep local axes meaningful:
   - Local X = opening width
   - Local Y = opening up/height
   - Local forward = positive A to B direction
4. Add **Flood Connection**.
5. Assign:
   - **Simulation Manager**: same manager as both sides.
   - **Side A** and **Side B**: two different boundaries (volumes may be region
     members; they must not resolve to the same region).
6. Set:
   - **Opening Width**: positive value
   - **Opening Height**: positive value
   - **Discharge Coefficient**: start at `0.62`
   - **Is Open**: enabled

## Door / hatch pattern (two regions)

```text
FloodRegion "RoomA"  -- FloodConnection --  FloodRegion "RoomB"
```

1. Put Room A volumes in region A.
2. Put Room B volumes in region B.
3. Point the connection at a volume on each side (or the volumes that sit at the
   doorway).
4. Drive `IsOpen` / `OpenFraction` at runtime for the door.
5. Do **not** put both rooms in one region merely because authored geometry
   meets at the doorway.

## Key Inspector fields

- **Side A / Side B**: endpoints with matching density.
- **Opening Width / Height**: geometric aperture.
- **Discharge Coefficient**: hydraulic restriction.
- **Is Open**: hard open/close gate.
- **OpenFraction** (runtime/API): partial aperture scaling.

## Verification checklist

1. Create head difference (different interior levels or exterior waterline).
2. Enter Play Mode.
3. Confirm non-zero requested/applied flow and direction from high head to low.
4. Toggle **Is Open** off; confirm immediate stop.
5. Confirm Inspector validation fails if both sides resolve to one region.

## Common mistakes

- Endpoint density mismatch.
- Side A and Side B assigned to the same endpoint object.
- Both sides are members of the same `FloodRegion`.
- Opening Transform placed at doorway center instead of bottom.
- Assuming width/height can express a partially open door.
  Use `OpenFraction` for partial openness.
- Using a connection for an always-open doorway when you wanted seamless
  first-person presentation — use `FloodRegion` composition instead.

## Runtime API notes

- `IsOpen`: hard gate.
- `OpenFraction`: 0 to 1 aperture scale.
- `RequestedFlowRate`: unconstrained demand.
- `CurrentFlowRate`: applied flow after reconciliation.
- `FlowDirectionWorld`, `PressureHeadDifference`,
  `SubmergedOpeningArea` are useful diagnostics.
- Validation: `TryValidateEndpoints` reports same-region errors clearly.
