# Hull Breach sample

This Unity 6.5 sample demonstrates pressure-driven bidirectional flow between an
infinite external fluid boundary and one finite rectangular `FloodVolume`.

## Import and open

Import **Hull Breach** from **Window > Package Management > Package Manager >
Flooding > Samples**. Unity copies it to:

`Assets/Samples/Flooding/0.9.1/Hull Breach`

Open `HullBreach.unity` from that imported folder and enter Play Mode. The scene
hierarchy, component wiring, camera, light, water meshes, and presentation
materials are authored and editable before Play Mode.

> Re-importing this sample or upgrading the package can replace the copy under
> `Assets/Samples`. Move or rename an imported copy before re-importing if you
> want to preserve local scene, script, or material changes.

## Authored scene hierarchy

```text
Hull Breach Demo
  FloodSimulationManager
  FloodDiagnostics
  HullBreachBootstrap (sample-only ocean visual + readout)
  External Ocean
    ExternalFluidBoundary
    Ocean Surface Visual
  Breached Compartment
    FloodVolume
    FloodCubeSurfaceRenderer
    Floor / walls
    Water Visual
  Hull Breach Connection
    FloodConnection
    FloodConnectionVisual
    Opening Visual
Main Camera
Directional Light
```

The ocean Transform sits at world Y = 1 m and defines the external waterline.
The compartment is a 4×3×2.5 m rectangular prism. The breach opening sits on the
front wall near the floor and connects **External Ocean** (side A) to
**Breached Compartment** (side B). `FloodConnectionVisual` enables the green
opening mesh while flow is active.

Compartment water is presented by `FloodCubeSurfaceRenderer`, which builds a
submerged mesh from the solved gravity-aligned `SurfacePlane`. Rotating the
compartment keeps the free surface level with gravity. `HullBreachBootstrap`
does not own compartment water rendering.

## Edit and tune before Play Mode

- Select **External Ocean** and move its Transform on world Y to change exterior
  head. Keep `transform.up` opposing gravity for a normal free surface.
- Select **Breached Compartment** and edit **Initial Volume**, density, or
  rectangular dimensions on `FloodVolume`.
- Select **Hull Breach Connection** and edit opening width/height, discharge
  coefficient, or **Is Open**.
- Rotate the compartment to change interior geometry relative to gravity and
  the fixed ocean plane; the water surface should remain gravity-aligned.

## Expected Play Mode behavior

With the default empty compartment and ocean waterline above the breach:

1. Water inflows from ocean to compartment.
2. Interior level rises toward the ocean waterline.
3. Requested and applied flow approach zero near equilibrium.
4. Raising the compartment above the ocean waterline (or lowering the ocean)
   reverses flow to outflow.
5. Closing the connection stops transfer immediately.
6. Rotating the compartment on X or Z keeps the compartment water surface
   gravity-aligned while walls and opening rotate with the hull.

`FloodSource` is intentionally absent. Configured sources inject volume without
pressure equilibrium; this sample uses an `ExternalFluidBoundary` plus
`FloodConnection` so flow depends on breach depth and heads.

The Game-view readout reports ocean waterline elevation along gravity, compartment
volume and equivalent level-fill height, applied flow, and the connection's
signed pressure-head difference (side A minus side B).
