# Flood underwater in URP

This guide covers the underwater presentation stack for first-person views:

- `FloodUnderwaterProfile` (look settings asset)
- `FloodUnderwaterCameraEffect` (camera consumer)
- `Flood Underwater Renderer Feature` (URP pass)

Core simulation works without URP; this page is only for optional camera
underwater visuals.

## Minimum setup checklist

1. Project uses URP.
2. URP Asset has **Depth Texture** enabled.
3. Active URP Renderer includes **Flood Underwater Renderer Feature**.
4. Renderer feature uses package `FloodUnderwater` material.
5. Camera has `FloodCameraTracker` and `FloodUnderwaterCameraEffect`.
6. Camera effect has a valid `FloodUnderwaterProfile`.

## Step-by-step: Flood Underwater Renderer Feature

1. Open **Edit > Project Settings > Graphics**.
2. Click the assigned URP Asset in **Scriptable Render Pipeline Settings**.
3. In that URP Asset, enable **Depth Texture**.
4. In the same URP Asset, click its default renderer reference.
5. In the renderer asset Inspector, go to **Renderer Features**.
6. Click **Add Renderer Feature**.
7. Choose **Flood Underwater Renderer Feature**.
8. Set fields:
   - **Material**:
     `Packages/com.rabbidwolf.com.kyle.flooding/Materials/FloodUnderwater`
   - **Render Pass Event**: `Before Rendering Post Processing`
   - **Waterline Softness Meters**: `0.03`

## Recommended starting values

- `Waterline Softness Meters`: `0.03`
- Profile `Distortion Strength`: `0.008`
- Profile `Distortion Speed`: `0.35`
- Profile `Transition Duration`: `0.15` to `0.30`

## Camera wiring

1. Select `Main Camera`.
2. Add `FloodCameraTracker`.
3. Add `FloodUnderwaterCameraEffect`.
4. Assign `Profile` to your underwater profile asset.
5. Leave `Tracker` empty when tracker is on the same camera.

## Verification

1. Enter Play Mode in rising-flood scene.
2. Confirm split view near waterline crossing.
3. Confirm full-view tint/fog when camera is submerged.
4. Tilt room (sample key `T`) and confirm waterline follows solved
   `SurfacePlane`, not world Y.

## Common mistakes

- Depth Texture disabled.
- Renderer feature missing on the active renderer.
- Renderer feature material unassigned.
- Camera effect/profile not assigned.
- Expecting clipping to compartment bounds in screen space
  (current pass uses infinite solved surface plane).

## Tuning quick notes

- Line too hard: increase `Waterline Softness Meters`.
- Line too blurry: decrease `Waterline Softness Meters`.
- Too wavy: lower profile `Distortion Strength`.
- Enter/exit flicker: widen tracker hysteresis band.

For full tuning table, see
[underwater look cheat sheet](../editor-workflow.md#tune-underwater-look-symptom--where-to-click).

Related: [Materials](../presentation/materials.md),
[Underwater audio](../presentation/audio.md#floodunderwateraudio),
[Presentation hub](../presentation/README.md).
