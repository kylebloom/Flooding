# FloodConnection

`FloodConnection` is pressure-driven exchange through a rectangular opening
between two `IFluidBoundary` endpoints (`FloodVolume` or
`ExternalFluidBoundary`).

## Use this when

- You need conserved transfer between compartments.
- You need exchange with an infinite exterior waterline.
- Flow direction should follow head difference automatically.

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
   - **Side A** and **Side B**: two different boundaries.
6. Set:
   - **Opening Width**: positive value
   - **Opening Height**: positive value
   - **Discharge Coefficient**: start at `0.62`
   - **Is Open**: enabled

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

## Common mistakes

- Endpoint density mismatch.
- Side A and Side B assigned to same endpoint.
- Opening Transform placed at doorway center instead of bottom.
- Assuming width/height can express a partially open door.
  Use `OpenFraction` for partial openness.

## Runtime API notes

- `IsOpen`: hard gate.
- `OpenFraction`: 0 to 1 aperture scale.
- `RequestedFlowRate`: unconstrained demand.
- `CurrentFlowRate`: applied flow after reconciliation.
- `FlowDirectionWorld`, `PressureHeadDifference`,
  `SubmergedOpeningArea` are useful diagnostics.
