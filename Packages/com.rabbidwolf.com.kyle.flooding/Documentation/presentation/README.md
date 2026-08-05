# Flooding presentation guides

This folder documents the **optional presentation layer**: water visuals, flow
VFX/SFX, local ingress, first-person underwater look, telemetry for UI, and
Scene-view diagnostics.

Presentation components **never mutate** flood simulation. Water volume,
transfers, and gameplay queries stay on `FloodVolume` / `FloodRegion` /
`FloodSimulationManager`.

Use these guides when you need to choose a visual/audio stack or configure one
option in depth. For simulation components, see
[Component guides](../components/README.md). For end-to-end scenarios, see
[Unity Editor workflow](../editor-workflow.md).

## Core rule

```text
Simulation (authoritative)          Presentation (optional consumers)
─────────────────────────           ────────────────────────────────
FloodVolume / FloodRegion           Surface renderers
FloodSource / FloodSink             Connection visual + audio
FloodConnection                     Local ingress (stream / patch)
FloodState snapshots                Camera tracker → underwater FX / audio
                                    Telemetry adapters → your UI
                                    FloodDiagnostics → Scene view only
```

## Choose a presentation option

| I want… | Use | Guide |
| --- | --- | --- |
| Visible water in a rectangular room | `FloodCubeSurfaceRenderer` + child visual | [Surface renderers](surface-renderers.md) |
| Visible water for a custom floor plan | `FloodPolygonSurfaceRenderer` | [Surface renderers](surface-renderers.md) |
| Free surface for baked / complex interiors | `FloodBakedSurfaceRenderer` | [Surface renderers](surface-renderers.md) |
| One continuous surface across a `FloodRegion` | `FloodRegionSurfaceRenderer` | [Surface renderers](surface-renderers.md) |
| Door/breach flow arrow, particles, or mesh | `FloodConnectionVisual` | [Connection visual](flood-connection-visual.md) |
| Localized jet + floor-spread at a leak/breach | `FloodLocalIngressPresenter` | [Local ingress](../local-ingress.md) |
| Fill / flow / underwater sound | Audio components | [Audio](audio.md) |
| First-person waterline / underwater tint | Tracker + profile + URP feature | [Camera tracker](../components/flood-camera-tracker.md), [Underwater URP](../components/flood-underwater-urp.md) |
| Bind fill / underwater state to UI | Telemetry adapters | [Telemetry](telemetry.md) |
| Scene-view COM / surface / flow overlays | `FloodDiagnostics` | [Diagnostics](flood-diagnostics.md) |
| Package materials and shader roles | Materials folder | [Materials](materials.md) |

## Recommended stacks

### Bulk water only (most scenes)

1. Match geometry mode → surface renderer ([chooser](surface-renderers.md#renderer-chooser)).
2. Assign a transparent material ([materials](materials.md)).
3. Leave ingress, camera, and audio off until you need them.

### Door or breach with feedback

1. Bulk surface renderer on the destination compartment (or region).
2. Optional [`FloodConnectionVisual`](flood-connection-visual.md) and
   [`FloodConnectionAudio`](audio.md#floodconnectionaudio) on the opening.
3. Optional [local ingress](../local-ingress.md) when instant bulk fill looks wrong
   for a localized entry.

### First-person rising flood

1. Bulk surface (prefer region renderer when walking through open doorways).
2. [`FloodCameraTracker`](../components/flood-camera-tracker.md) on the camera.
3. Optional URP underwater stack
   ([guide](../components/flood-underwater-urp.md)).
4. Optional [`FloodUnderwaterAudio`](audio.md#floodunderwateraudio) and
   [`FloodCameraTelemetry`](telemetry.md#floodcameratelemetry).

## Relationship map

```text
FloodVolume / FloodRegion.StateChanged
  → Flood*SurfaceRenderer / FloodRegionSurfaceRenderer

FloodConnection.CurrentFlowRate
  → FloodConnectionVisual / FloodConnectionAudio
  → FloodIngressSampler → FloodLocalIngressPresenter → FloodIngressStreamPresenter

FloodSource (configured rate when active)
  → FloodSourceAudio (+ ingress if listed as a provider)

FloodVolume fill
  → FloodVolumeAudio / FloodVolumeTelemetry

FloodCameraTracker
  → FloodUnderwaterCameraEffect → URP Flood Underwater Renderer Feature
  → FloodUnderwaterAudio / FloodCameraTelemetry

FloodUnderwaterProfile / FloodIngressPresentationProfile
  → shared look settings (ScriptableObject assets)

FloodDiagnostics
  → Scene-view gizmos only (selected GameObject)
```

## How to use these guides

1. Pick the row in the chooser table that matches your goal.
2. Follow the minimum setup checklist in that guide.
3. Verify in Play Mode with the checklist at the bottom of the guide.
4. Use troubleshooting before changing simulation architecture.

## Related docs

- [Component guides hub](../components/README.md)
- [Local ingress](../local-ingress.md)
- [Scenario 7 — flow visuals and audio](../editor-workflow.md#scenario-7--flow-visuals-and-audio)
- [Scenario 8 — diagnostics](../editor-workflow.md#scenario-8--scene-view-diagnostics-while-tuning)
- [Scenario 9 — first-person underwater](../editor-workflow.md#scenario-9--first-person-camera-through-a-rising-flood)
- [Scenario 10 — local ingress](../editor-workflow.md#scenario-10--local-ingress-presentation-vs-instant-bulk-surface)
