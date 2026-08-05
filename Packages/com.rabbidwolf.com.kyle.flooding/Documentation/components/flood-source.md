# FloodSource

`FloodSource` requests configured inflow into one target `FloodVolume`.
It is not pressure-equilibrium flow.

If the target is a [`FloodRegion`](flood-region.md) member, injection still
authors against that volume (spatial attachment / ingress can stay in that
room), but the accepted water is applied to the **owning region** through
`EffectiveFluidBoundary`.

## Use this when

- You want scripted or authored inflow (pipe leak, sprinkler, debug faucet).
- Inflow should ignore exterior pressure equilibrium.

## Do not use this when

- You need ocean/lake exchange based on head difference.
  Use `ExternalFluidBoundary` + `FloodConnection` instead.

## Beginner setup

1. Create GameObject `Leak` under `Flood System`.
2. Add **Flood Source**.
3. Assign:
   - **Simulation Manager**: same manager as target volume / region.
   - **Target**: desired `FloodVolume` (standalone or region member).
4. Set:
   - **Flow Rate**: `1` (m³/s) to start.
   - **Active**: enabled.

## Key Inspector fields

- **Target**: destination compartment volume (may be a region member).
- **Flow Rate**: configured requested inflow.
- **Active**: on/off gate.
- **Ingress Anchor** (optional): presentation-only spawn point; ignored by
  simulation.

## Verification checklist

1. Enter Play Mode.
2. Confirm target volume (or its owning region) rises.
3. Confirm `CurrentFlowRate` becomes zero near full capacity.
4. If the target is a region member, confirm sibling member volumes show the
   same shared `CurrentVolume`.

## Common mistakes

- Source and target on different managers.
- Target missing or wrong compartment assigned.
- Assuming source can reverse or equalize pressure.
- Expecting a source into Room A of a multi-member region to fill only Room A’s
  geometry capacity — region capacity and surface are shared.

## Runtime API notes

- `IsActive` controls runtime enable/disable.
- `FlowRate` is configured requested rate.
- `CurrentFlowRate` is applied rate after manager reconciliation.
- `RequestedFlowRate` is the configured rate that would be requested this frame
  when active.
