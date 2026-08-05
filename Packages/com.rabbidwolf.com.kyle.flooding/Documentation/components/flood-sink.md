# FloodSink

`FloodSink` requests configured water removal from one finite `FloodVolume`.
Removed volume leaves the simulation.

## Use this when

- You need pump-like drainage from a compartment.
- You want gameplay-controlled dewatering.

## Do not use this when

- You need pressure-driven transfer to another body.
  Use `FloodConnection` to another boundary instead.

## Beginner setup

1. Create GameObject `Bilge Pump` under `Flood System`.
2. Add **Flood Sink**.
3. Assign:
   - **Simulation Manager**: same manager as target volume.
   - **Target**: drained `FloodVolume`.
4. Set:
   - **Flow Rate**: `0.5` to `2` (m^3/s) based on gameplay scale.
   - **Active**: enabled.

## Key Inspector fields

- **Target**: source compartment for removal.
- **Flow Rate**: configured requested removal.
- **Active**: on/off gate.

## Verification checklist

1. Start with non-zero water volume.
2. Enter Play Mode and confirm volume decreases.
3. Confirm `CurrentFlowRate` drops toward zero when supply is exhausted.

## Common mistakes

- Expecting sink to move water into another compartment.
- Sink active but target is already empty.
- Sink on a different manager than the target volume.

## Runtime API notes

- `IsActive` and `FlowRate` are gameplay control points.
- `CurrentFlowRate` is applied (supply-constrained) removal.
