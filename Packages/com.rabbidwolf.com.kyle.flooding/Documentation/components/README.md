# Flooding component guides

This folder provides component-by-component documentation for scene setup,
use cases, runtime behavior, and troubleshooting.

Use these guides when you already know the scenario you want, but need deep
details for one specific component.

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

## How to use these guides

1. Start with the component you are adding to your scene.
2. Follow the minimum setup checklist in that guide.
3. Run the verification checklist in Play Mode.
4. Use the troubleshooting section before changing architecture.

For end-to-end workflows, see [Unity Editor workflow](../editor-workflow.md).
