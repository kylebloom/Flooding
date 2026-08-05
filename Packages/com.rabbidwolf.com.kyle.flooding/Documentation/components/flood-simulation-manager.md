# FloodSimulationManager

`FloodSimulationManager` is the fixed-step orchestrator for Flooding.
Volumes, sources, sinks, connections, and external boundaries must share one
manager per simulation group.

## Use this when

- You want deterministic fixed-step flooding updates.
- Multiple flooding components must reconcile flow in one tick.
- You need gravity policy control for a flooding hierarchy.

## Do not use this when

- You want ad hoc per-component Update loops.
- You want separate gravity policies in one network (use separate managers).

## Beginner setup

1. Create an empty GameObject named `Flood System`.
2. Add **Flood Simulation Manager**.
3. Place all related flood GameObjects under `Flood System`, or assign this
   manager explicitly to each component.
4. Keep initial defaults:
   - **Ticks Per Second**: `10`
   - **Maximum Ticks Per Frame**: default
   - **Simulate Automatically**: enabled
   - **Gravity Mode**: **Physics Gravity**

## Key Inspector fields

- **Ticks Per Second**: simulation frequency.
- **Maximum Ticks Per Frame**: backlog guard for long frames.
- **Simulate Automatically**: advances from game time.
- **Gravity Mode**: `Physics Gravity` or `Custom`.
- **Custom Gravity**: only used in `Custom` mode.

## Verification checklist

1. Enter Play Mode.
2. Confirm active `FloodSource`/`FloodSink`/`FloodConnection` actually changes
   volume or flow diagnostics.
3. Disable `Simulate Automatically` and call `Advance` from code to verify
   manual control if needed.

## Common mistakes

- Different components using different managers unintentionally.
- Disabled manager GameObject.
- Setting custom gravity but leaving mode on Physics.

## Runtime API notes

- `Advance(elapsedSeconds)` accumulates and runs fixed ticks.
- `SimulateTick(tickDuration)` runs exactly one tick.
- `LastTickMetrics` exposes per-tick applied flow and conservation diagnostics.
