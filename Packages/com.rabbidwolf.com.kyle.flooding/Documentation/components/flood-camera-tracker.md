# FloodCameraTracker

`FloodCameraTracker` reports camera/viewpoint relationship to flood volumes
(active volume, inside/outside, underwater state, signed distance, depth).
It does not change simulation.

## Use this when

- You need camera-aware underwater transitions.
- You need gameplay/UI state from viewpoint submersion.

## Beginner setup

1. Select player camera GameObject.
2. Add **Flood Camera Tracker**.
3. Choose selection mode:
   - **Explicit**: assign one `FloodVolume`.
   - **Auto Discover Registered**: assign manager or let it resolve.
4. Keep default hysteresis values initially:
   - Enter Water Threshold Meters: `-0.02`
   - Exit Water Threshold Meters: `+0.02`

## Regions and overlapping volumes

- Auto-discover still selects among registered `FloodVolume`s (including region
  members). Selection uses each volume’s `QueryPoint` (member geometry
  containment + that volume’s active surface plane).
- For a composed [`FloodRegion`](flood-region.md), member volumes share one
  surface plane, so crossing an unrestricted doorway inside the region should
  keep a consistent waterline as long as presentation uses
  `FloodRegionSurfaceRenderer`.
- Independent overlapping volumes that are **not** in a region remain ambiguous
  and are **not** physically merged. Sticky selection still applies.

## Key Inspector fields

- **Viewpoint**: optional transform override.
- **Volume Selection Mode**: explicit or auto discover.
- **Manager**: source of registered volumes for auto mode.
- **Enter/Exit thresholds**: underwater latching hysteresis.

## Verification checklist

1. Enter Play Mode in a rising-flood scene.
2. Confirm transitions occur once near the surface without rapid toggling.
3. Confirm `IsUnderwater` and `SubmersionDepthMeters` track camera motion.
4. In a two-room `FloodRegion`, walk through the doorway and confirm underwater
   state does not pop due to mismatched member planes.

## Common mistakes

- Tracker on camera but wrong volume selected in explicit mode.
- Auto mode without accessible manager/registered volumes.
- Overlapping **standalone** volumes creating ambiguous expectations (sticky
  selection applies; use a `FloodRegion` if they should be one water body).
- Region members still using per-volume surface renderers, so the tracker plane
  and visible mesh disagree.

## Runtime API notes

- Read properties: `ActiveVolume`, `IsInsideFloodVolume`, `IsUnderwater`,
  `SurfaceSignedDistanceMeters`, `SubmersionDepthMeters`, `CurrentQuery`.
- Events: `EnteredFloodVolume`, `ExitedFloodVolume`, `EnteredWater`,
  `ExitedWater`, `ActiveVolumeChanged`.

## Related presentation consumers

- [Underwater URP](flood-underwater-urp.md)
- [Underwater audio](../presentation/audio.md#floodunderwateraudio)
- [Camera telemetry](../presentation/telemetry.md#floodcameratelemetry)
- [Presentation hub](../presentation/README.md)
