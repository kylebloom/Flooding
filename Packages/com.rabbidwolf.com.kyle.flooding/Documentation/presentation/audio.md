# Flood audio presentation

Four optional audio consumers drive `AudioSource` or `AudioMixer` parameters from
public flood diagnostics. None mutate simulation. The package does **not** ship
audio clips — assign your own looping water / ambience assets.

| Component | Driven by | Typical attachment |
| --- | --- | --- |
| `FloodConnectionAudio` | Measured connection flow | Door / breach GameObject |
| `FloodSourceAudio` | Configured source rate when active | Leak / pipe GameObject |
| `FloodVolumeAudio` | Compartment fill percentage | Room / volume GameObject |
| `FloodUnderwaterAudio` | `FloodCameraTracker` underwater latch | Camera or audio director |

## Shared setup tips

1. Prefer **3D** Spatial Blend (`≈ 1`) on connection/source/volume sources so
   sound localizes at the opening or room.
2. Use looping clips; components start/stop based on intensity.
3. Disabling any of these components must not change water volume or ticks.

---

## FloodConnectionAudio

Menu: **Add Component > Flooding > Flood Connection Audio** (requires
`AudioSource`).

### Use this when

- A door, hatch, or hull opening should sound louder as applied flow rises.

### Beginner setup

1. Select the connection GameObject (or a child at the opening).
2. **Add Component > Flood Connection Audio**.
3. Assign a looping clip on the **Audio Source** or in **Flow Clip**.
4. Set Spatial Blend near `1`.
5. Match thresholds to your scene’s typical rates (defaults suit medium doors):
   - **Low Flow Threshold**: `0.25` m³/s
   - **High Flow Threshold**: `2` m³/s

### Key Inspector fields

| Field | Unit / notes | Default |
| --- | --- | --- |
| **Connection** | Applied flow + submerged area | Auto same GO |
| **Audio Source** | Prefer 3D spatial | Auto |
| **Flow Clip** | Used when AudioSource clip is empty | — |
| **Low / High Flow Threshold** | m³/s intensity band | `0.25` / `2` |
| **Volume At Full Flow** | AudioSource volume 0–1 | `0.8` |
| **Pitch At Low / Full Flow** | Pitch range | `0.85` / `1.25` |

Pitch and volume also get a slight boost from submerged opening area.

### Runtime API

- `Connection`, `CurrentIntensity`
- `Refresh()`

---

## FloodSourceAudio

Menu: **Add Component > Flooding > Flood Source Audio** (requires `AudioSource`).

### Use this when

- A configured leak/pipe should sound when `FloodSource.IsActive`, scaled by
  **configured** `FlowRate` (not measured connection flow).

### Beginner setup

1. Select the source GameObject.
2. **Add Component > Flood Source Audio**.
3. Assign a looping clip.
4. Set **Full Flow Rate** (m³/s) to the configured rate you treat as “loudest”
   (default `1`).

### Key Inspector fields

| Field | Unit / notes | Default |
| --- | --- | --- |
| **Source** | `FloodSource` | Auto |
| **Audio Source** / **Flow Clip** | | Auto / optional |
| **Full Flow Rate** | m³/s = full intensity | `1` |
| **Volume At Full Flow** | 0–1 | `0.7` |
| **Pitch At Low / Full Flow** | | `0.9` / `1.2` |

Silent when the source is inactive or rate is idle.

---

## FloodVolumeAudio

Menu: **Add Component > Flooding > Flood Volume Audio** (requires `AudioSource`).

### Use this when

- A compartment should gain low ambience as it fills (slosh / flooded-room bed).

### Beginner setup

1. Select the compartment GameObject with **Flood Volume**.
2. **Add Component > Flood Volume Audio**.
3. Assign an ambience clip.
4. Tune fill gates:
   - **Silent Below Fill**: `0.02` (0–1)
   - **Full At Fill**: `0.85`

### Key Inspector fields

| Field | Unit / notes | Default |
| --- | --- | --- |
| **Volume** | `FloodVolume` fill driver | Auto |
| **Audio Source** / **Ambience Clip** | | Auto / optional |
| **Silent Below Fill** | Fill 0–1 | `0.02` |
| **Full At Fill** | Fill 0–1 | `0.85` |
| **Volume At Full Fill** | AudioSource volume 0–1 | `0.55` |
| **Pitch At Low / Full Fill** | Pitch drops as fill rises | `0.95` / `0.75` |

For region members, fill still comes from the volume’s reported state (region-
owned water). Prefer attaching ambience once per audible space.

---

## FloodUnderwaterAudio

Menu: **Add Component > Flooding > Flood Underwater Audio**.

Muffles mix groups through exposed **AudioMixer** parameters while
`FloodCameraTracker.IsUnderwater` is latched. Render-pipeline independent.

### Use this when

- First-person (or any tracked viewpoint) should dull/muffle audio underwater.

### Beginner setup

1. Create or open an **Audio Mixer** asset.
2. Add a **Lowpass** effect on the group that should muffle.
3. Expose parameters (right-click parameter → **Expose**):
   - Low-pass cutoff (Hz), default name `FloodLowPassCutoff`
   - Optional volume (dB), default name `FloodUnderwaterVolume`
4. Select the camera (or audio director) GameObject.
5. **Add Component > Flood Underwater Audio**.
6. Assign **Audio Mixer** and confirm parameter names match.
7. Ensure a [`FloodCameraTracker`](../components/flood-camera-tracker.md) exists
   (same GameObject or Main Camera).

### Key Inspector fields

| Field | Unit / notes | Default |
| --- | --- | --- |
| **Tracker** | Underwater latch source | Auto resolve |
| **Audio Mixer** | Owns exposed params | — |
| **Low Pass Parameter** | Exposed Hz name | `FloodLowPassCutoff` |
| **Volume Parameter** | Exposed dB name; empty = skip | `FloodUnderwaterVolume` |
| **Normal / Underwater Low Pass Cutoff** | Hz | `22000` / `700` |
| **Normal / Underwater Volume** | dB | `0` / `-4` |
| **Transition Duration** | Seconds (MoveTowards) | `0.25` |
| **Update Automatically** | LateUpdate Refresh | `true` |

### Runtime API

```csharp
audio.Refresh(Time.deltaTime);
float cutoff = audio.CurrentLowPassCutoffHz;
float blend = audio.CurrentUnderwaterBlend; // 0–1
```

### Common mistakes

- Forgetting to **Expose** mixer parameters (SetFloat fails; component warns once).
- Parameter name typos vs Inspector strings.
- No tracker / wrong camera — blend never leaves dry values.
- Rapid enter/exit flicker: widen tracker hysteresis, not only mixer transition.

---

## Verification checklist

1. Play a rising-flood or equalization scene with your clips assigned.
2. Confirm loudness tracks flow or fill as expected.
3. For underwater audio, cross the waterline and confirm muffling without
   simulation changes.
4. Disable each audio component and confirm water volume is unchanged.

## Related

- [Connection visual](flood-connection-visual.md)
- [Camera tracker](../components/flood-camera-tracker.md)
- [Scenario 7](../editor-workflow.md#scenario-7--flow-visuals-and-audio)
- [Underwater audio in Editor workflow](../editor-workflow.md#underwater-audio-audiomixer)
