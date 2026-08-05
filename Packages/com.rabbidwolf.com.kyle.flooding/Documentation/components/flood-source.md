# FloodSource

`FloodSource` requests configured inflow into one target `FloodVolume`.
It is not pressure-equilibrium flow.

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
   - **Simulation Manager**: same manager as target volume.
   - **Target**: desired `FloodVolume`.
4. Set:
   - **Flow Rate**: `1` (m^3/s) to start.
   - **Active**: enabled.

## Key Inspector fields

- **Target**: destination compartment.
- **Flow Rate**: configured requested inflow.
- **Active**: on/off gate.

## Verification checklist

1. Enter Play Mode.
2. Confirm target volume rises.
3. Confirm `CurrentFlowRate` becomes zero near full capacity.

## Common mistakes

- Source and target on different managers.
- Target missing or wrong compartment assigned.
- Assuming source can reverse or equalize pressure.

## Runtime API notes

- `IsActive` controls runtime enable/disable.
- `FlowRate` is configured requested rate.
- `CurrentFlowRate` is applied rate after manager reconciliation.
