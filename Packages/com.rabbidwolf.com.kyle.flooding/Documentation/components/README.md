# Flooding component guides

This folder provides component-by-component documentation for scene setup,
use cases, runtime behavior, and troubleshooting.

Use these guides when you already know the scenario you want, but need deep
details for one specific component.

## Core model

```text
FloodVolume              = authored floodable geometry (+ query facade)
FloodRegion              = independently simulated / equilibrium water body
FloodConnection          = hydraulic restriction between FloodRegions
FloodSimulationManager   = orchestration / conservation
```

- A standalone `FloodVolume` (not listed on any region) still owns its own water
  state — the simple one-room case does not require a region.
- When volumes are members of a `FloodRegion`, the **region** owns
  `CurrentVolume`, `InitialVolume`, and the shared free-surface plane.
- Unrestricted openings (doorways, open corridors) should usually be one region
  with overlapping or face-touching member volumes.
- Watertight / controllable doors remain two regions linked by a
  `FloodConnection`. Opening the door changes flow; it does not merge regions.

## Core simulation components

- [FloodSimulationManager](flood-simulation-manager.md)
- [FloodVolume](flood-volume.md)
- [FloodRegion](flood-region.md)
- [FloodSource](flood-source.md)
- [FloodSink](flood-sink.md)
- [FloodConnection](flood-connection.md)
- [ExternalFluidBoundary (External Fluid Body)](external-fluid-boundary.md)

## Camera and underwater presentation

- [FloodCameraTracker](flood-camera-tracker.md)
- [FloodUnderwaterProfile + FloodUnderwaterCameraEffect + URP Renderer Feature](flood-underwater-urp.md)

## Optional presentation (visuals, audio, UI, diagnostics)

Deep guides for surface renderers, flow VFX, audio, telemetry, materials, and
diagnostics live in the [Presentation guides hub](../presentation/README.md):

- [Surface renderers](../presentation/surface-renderers.md) (cube / polygon /
  baked / region)
- [FloodConnectionVisual](../presentation/flood-connection-visual.md)
- [Audio](../presentation/audio.md)
- [Telemetry](../presentation/telemetry.md)
- [FloodDiagnostics](../presentation/flood-diagnostics.md)
- [Materials](../presentation/materials.md)
- [Local ingress](../local-ingress.md)

## How to use these guides

1. Start with the component you are adding to your scene.
2. Follow the minimum setup checklist in that guide.
3. Run the verification checklist in Play Mode.
4. Use the troubleshooting section before changing architecture.

For end-to-end workflows, see [Unity Editor workflow](../editor-workflow.md).
For choosing a visual/audio stack, see
[Presentation guides](../presentation/README.md).
