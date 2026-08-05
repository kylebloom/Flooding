# FloodSink

`FloodSink` requests configured water removal from one finite `FloodVolume`.
Removed volume leaves the simulation.

If the target is a [`FloodRegion`](flood-region.md) member, removal authors
against that volume for Inspector/setup convenience, but supply and commits
resolve to the **owning region** through `EffectiveFluidBoundary`.

## Use this when

- You need pump-like drainage from a compartment or region.
- You want gameplay-controlled dewatering.

## Do not use this when

- You need pressure-driven transfer to another body.
  Use `FloodConnection` to another boundary instead.

## Beginner setup

1. Create GameObject `Bilge Pump` under `Flood System`.
2. Add **Flood Sink**.
3. Assign:
   - **Simulation Manager**: same manager as target volume / region.
   - **Target**: drained `FloodVolume` (standalone or region member).
4. Set:
   - **Flow Rate**: `0.5` to `2` (m³/s) based on gameplay scale.
   - **Active**: enabled.

## Key Inspector fields

- **Target**: source compartment volume for removal (may be a region member).
- **Flow Rate**: configured requested removal.
- **Active**: on/off gate.

## Verification checklist

1. Start with non-zero water volume (region **Initial Volume** if the target is
   a member).
2. Enter Play Mode and confirm volume decreases on the target / owning region.
3. Confirm `CurrentFlowRate` drops toward zero when supply is exhausted.
4. If the target is a region member, confirm all members report the same lowered
   `CurrentVolume`.

## Common mistakes

- Expecting sink to move water into another compartment.
- Sink active but target / region is already empty.
- Sink on a different manager than the target volume.
- Expecting a sink on Room A to empty only that room inside a shared region —
  the region has one water body.

## Runtime API notes

- `IsActive` and `FlowRate` are gameplay control points.
- `CurrentFlowRate` is applied (supply-constrained) removal.
- `RequestedFlowRate` is the configured rate that would be requested this frame
  when active.
