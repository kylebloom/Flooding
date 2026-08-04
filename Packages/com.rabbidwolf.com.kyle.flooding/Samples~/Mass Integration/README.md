# Flood Mass Integration sample

Import **Flood Mass Integration** from **Window > Package Management > Package
Manager > Flooding > Samples**. Unity copies it to:

`Assets/Samples/Flooding/0.10.0/Flood Mass Integration`

Open `FloodMassRollPitch.unity` from that imported folder, then enter Play Mode.

## What this demonstrates

```text
Visible compartment water
        ↓
FloodVolume mass + centroid
        ↓
FloodMassAggregator
        ↓
RigidbodyFloodMassAdapter moves Rigidbody COM
        ↓
Vessel rolls / pitches on sample-only spring supports
```

The sample is a cutaway four-compartment barge. Water is rendered with
`FloodCubeSurfaceRenderer` (presentation only). Mass shift comes from the
package adapter, not from the renderer.

## Authored hierarchy

- **Main Camera** / **Directional Light** / **Ground Plane**
- **Flood Mass Demo Vessel**
  - `BoxCollider`, `Rigidbody`
  - `FloodSimulationManager`
  - `FloodMassAggregator`
  - `RigidbodyFloodMassAdapter`
  - `SampleVesselSupport` (**sample only** — artificial restoring forces, **not**
    buoyancy or vessel stability)
  - `FloodMassDemoBootstrap` (presets, auto-demo, HUD, COM markers)
  - **Hull Cutaway** — translucent low walls so interiors stay visible
  - **Dry / Flood / Combined Com Marker** spheres and **COM Shift Line**
  - **Port Bow / Starboard Bow / Port Stern / Starboard Stern** compartments
    each with `FloodVolume`, `FloodCubeSurfaceRenderer`, and **Water Visual**

All compartments start dry (`Initial Volume = 0`). An auto-demo floods
starboard, then bow, then starboard-bow so Play Mode shows the feature without
any input.

## Controls

| Key | Action |
| --- | --- |
| `1` | Empty all compartments |
| `2` | Flood port |
| `3` | Flood starboard |
| `4` | Flood bow |
| `5` | Flood stern |
| `6` | Flood starboard bow |
| `R` | Reset pose, empty water, restart auto-demo |
| `A` / `D` | Transfer water port ↔ starboard |
| `W` / `S` | Transfer water fore ↔ aft |

Any control key except `R` cancels the auto-demo. `R` restarts it.

## Tuning

1. Stop Play Mode and select **Flood Mass Demo Vessel**.
2. On `RigidbodyFloodMassAdapter`, edit **Dry Mass** (kg) or **Dry Center Of
   Mass Local** (meters).
3. On `SampleVesselSupport`, tune support height, stiffness, damping, and
   support-point extents. These values affect only the sample scaffolding.
4. On `FloodMassDemoBootstrap`, edit preset volume (m³ per compartment) and
   transfer rate (m³/s).
5. On each compartment `FloodVolume`, edit rectangular dimensions. Default
   capacity is 1.8 × 2.8 × 1.0 = 5.04 m³.

The runtime adapter owns dry mass and dry local center of mass. Do not have
another component write the same Rigidbody properties while the adapter is
enabled.

## Reimporting after package changes

The Package Manager imports a copy; it does not keep the scene synchronized
with `Packages/com.rabbidwolf.com.kyle.flooding/Samples~/Mass Integration`.
Back up any edits made under `Assets/Samples` before refreshing the sample.
Then delete only the imported
`Assets/Samples/Flooding/0.10.0/Flood Mass Integration` folder and click
**Import** for **Flood Mass Integration** again in the package's **Samples**
list.

Editor authors can rebuild the authored package scene with
**Flooding > Internal > Build Flood Mass Integration Sample** after the sample
scripts are imported or present under `Assets/Samples`.
