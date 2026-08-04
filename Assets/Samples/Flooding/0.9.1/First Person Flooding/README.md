# First Person Flooding sample

This Unity 6.5 sample demonstrates first-person presentation of a rising flood:
visible water, camera waterline crossing, underwater tint/fog (URP), optional
audio muffling, and simple telemetry — without changing the equilibrium flood
simulation.

## Import and open

Import **First Person Flooding** from **Window > Package Management > Package
Manager > Flooding > Samples**. Unity copies it to:

`Assets/Samples/Flooding/0.9.1/First Person Flooding`

Open `FirstPersonFlooding.unity` from that imported folder.

> Re-importing this sample or upgrading the package can replace the copy under
> `Assets/Samples`. Move or rename an imported copy before re-importing if you
> want to preserve local changes.

## One-time URP project setup (waterline effect)

The sample wires `FloodCameraTracker`, `FloodUnderwaterCameraEffect`, and a
profile asset. The fullscreen waterline pass is a **project renderer feature**:

1. Open your **URP Asset** and enable **Depth Texture**.
2. Open the **URP Renderer** asset → **Add Renderer Feature** →
   **Flood Underwater Renderer Feature**.
3. Assign material
   `Packages/com.rabbidwolf.com.kyle.flooding/Materials/FloodUnderwater`
   (shader `Kyle/Flooding/Underwater`).

Without this, Play Mode still shows rising water, tracker telemetry, and
first-person movement; the fullscreen underwater pass will not run.

Optional audio: create an Audio Mixer, expose a low-pass cutoff parameter named
`FloodLowPassCutoff` (and optional `FloodUnderwaterVolume`), then assign that
mixer on the camera's `FloodUnderwaterAudio` component.

## Authored scene hierarchy

```text
First Person Flooding Demo
  FloodSimulationManager
  FloodDiagnostics
  FirstPersonFloodingBootstrap
  Flooded Room
    Floor / Ceiling / Walls
    Room Volume
      FloodVolume
      FloodCubeSurfaceRenderer
      FloodVolumeTelemetry
      Water Visual
  Rising Water Source
    FloodSource
  Player
    CharacterController
    Main Camera
      Camera, AudioListener
      FloodCameraTracker
      FloodUnderwaterCameraEffect
      FloodUnderwaterAudio
      FloodCameraTelemetry
Directional Light
```

Also authored: `FirstPersonUnderwaterProfile.asset`, URP Lit wall/floor/water
materials.

## Tune the underwater look

Hover Inspector **field labels** to read tooltips.

Most look settings live on the profile asset, not the renderer feature:

1. Select **Main Camera** → **Flood Underwater Camera Effect**.
2. Click **Profile** (`FirstPersonUnderwaterProfile`), or open
   `FirstPersonUnderwaterProfile.asset` in this sample folder.
3. Common tweaks:

| Want | Field |
| --- | --- |
| Less wavy | **Distortion Strength** → try `0.002` or `0` |
| Slower shimmer | **Distortion Speed** |
| Softer / clearer water | **Fog Density**, **Maximum Fog Strength**, tint colors |
| Depth ramp | **Full Effect Depth** (meters) |

Waterline edge softness is on the project **URP Renderer** → **Flood
Underwater Renderer Feature** → **Waterline Softness Meters**.

Full symptom → field map:
[Tune underwater look](../../Documentation/editor-workflow.md#tune-underwater-look-symptom--where-to-click)
in the package documentation.

## Controls

| Input | Action |
| --- | --- |
| WASD | Move |
| Mouse | Look (Esc unlocks cursor; click relocks) |
| T | Toggle modest room tilt (waterline follows `SurfacePlane`) |
| R | Drain the room |

## Expected Play Mode behavior

1. Water rises from the `FloodSource` and is visible via
   `FloodCubeSurfaceRenderer`.
2. Camera starts above water inside the room.
3. As the surface reaches eye height, the waterline moves through the view
   (with the URP feature enabled).
4. After submersion, the full view receives underwater tint/fog; intensity
   increases somewhat with depth.
5. Audio blend rises underwater when an AudioMixer is assigned.
6. Press **T** to tilt the room: the free surface stays gravity-aligned and the
   waterline still uses the authoritative plane (not world Y).

Camera effects are presentation consumers only. They do not add CFD, waves, or
slosh, and they do not alter equilibrium flooding.

## Rebuild

**Flooding > Internal > Build First Person Flooding Sample** regenerates the
scene, materials, and underwater profile under `Samples~/First Person Flooding`.
