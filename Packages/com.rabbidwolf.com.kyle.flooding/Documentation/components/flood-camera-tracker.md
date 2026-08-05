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

## Key Inspector fields

- **Viewpoint**: optional transform override.
- **Volume Selection Mode**: explicit or auto discover.
- **Manager**: source of registered volumes for auto mode.
- **Enter/Exit thresholds**: underwater latching hysteresis.

## Verification checklist

1. Enter Play Mode in a rising-flood scene.
2. Confirm transitions occur once near the surface without rapid toggling.
3. Confirm `IsUnderwater` and `SubmersionDepthMeters` track camera motion.

## Common mistakes

- Tracker on camera but wrong volume selected in explicit mode.
- Auto mode without accessible manager/registered volumes.
- Overlapping volumes creating ambiguous expectations (sticky selection applies).

## Runtime API notes

- Read properties: `ActiveVolume`, `IsInsideFloodVolume`, `IsUnderwater`,
  `SurfaceSignedDistanceMeters`, `SubmersionDepthMeters`, `CurrentQuery`.
- Events: `EnteredFloodVolume`, `ExitedFloodVolume`, `EnteredWater`,
  `ExitedWater`, `ActiveVolumeChanged`.
