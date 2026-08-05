# FloodSimulationManager

`FloodSimulationManager` is the fixed-step orchestrator for Flooding.
Volumes, regions, sources, sinks, connections, and external boundaries must
share one manager per simulation group.

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
   manager explicitly to each component (including any `FloodRegion`).
4. Keep initial defaults:
   - **Ticks Per Second**: `10`
   - **Maximum Ticks Per Frame**: default
   - **Simulate Automatically**: enabled
   - **Gravity Mode**: **Physics Gravity**

## What it orchestrates

Each tick (simplified):

1. Snapshot finite water bodies and external boundaries.
2. Gather source / sink / connection requests.
3. Scale by supply and remaining capacity.
4. Commit signed volume deltas.
5. Publish states (`FloodVolume` and `FloodRegion` events).

### Regions and commit participants

When a [`FloodVolume`](flood-volume.md) belongs to a
[`FloodRegion`](flood-region.md):

- Water is owned by the region.
- Sources, sinks, and connections that target a member volume resolve through
  `EffectiveFluidBoundary` to the region’s commit participant.
- The manager does **not** double-count multi-member region water.

Standalone volumes (no owning region) behave as before.

`RegisteredVolumes` remains a live view of registered volumes for presentation
discovery (including region members). Region components also register with the
manager.

## Key Inspector fields

- **Ticks Per Second**: simulation frequency.
- **Maximum Ticks Per Frame**: backlog guard for long frames.
- **Simulate Automatically**: advances from game time.
- **Gravity Mode**: `Physics Gravity` or `Custom`.
- **Custom Gravity**: only used in `Custom` mode.

## Verification checklist

1. Enter Play Mode.
2. Confirm active `FloodSource` / `FloodSink` / `FloodConnection` actually
   changes volume or flow diagnostics.
3. With a `FloodRegion`, confirm a source targeting any member raises the shared
   region volume once (not once per member).
4. Disable `Simulate Automatically` and call `Advance` from code to verify
   manual control if needed.

## Common mistakes

- Different components using different managers unintentionally.
- Disabled manager GameObject.
- Setting custom gravity but leaving mode on Physics.
- Expecting overlapping registered volumes without a `FloodRegion` to share
  water — they remain independent unless composed explicitly.

## Runtime API notes

- `Advance(elapsedSeconds)` accumulates and runs fixed ticks.
- `SimulateTick(tickDuration)` runs exactly one tick.
- `LastTickMetrics` exposes per-tick applied flow and conservation diagnostics.
- `RegisteredVolumes` lists registered `FloodVolume`s in registration order.
