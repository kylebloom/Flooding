# Connected Compartments sample

This Unity 6.5 sample demonstrates bidirectional flow between two rectangular
`FloodVolume` compartments managed by one `FloodSimulationManager`.

## Import and open

Import **Connected Compartments** from **Window > Package Management > Package
Manager > Flooding > Samples**. Unity copies it to:

`Assets/Samples/Flooding/0.9.1/Connected Compartments`

Open `ConnectedCompartments.unity` from that imported folder and enter Play
Mode. The scene hierarchy, component wiring, camera, light, water meshes, and
four presentation materials are all authored and editable before Play Mode.

> Re-importing this sample or upgrading the package can replace the copy under
> `Assets/Samples`. Move or rename an imported copy before re-importing if you
> want to preserve local scene, script, or material changes.

## Authored scene hierarchy

```text
Connected Compartments Demo
  FloodSimulationManager
  FloodDiagnostics
  ConnectedCompartmentsBootstrap (sample-only presentation/readout)
  Compartment A (High Water)
    FloodVolume
    Floor
    Low Front Wall
    Back Wall
    Outer Wall
    Water Visual
  Compartment B (Low Water)
    FloodVolume
    Floor
    Low Front Wall
    Back Wall
    Outer Wall
    Water Visual
  Flood Connection
    FloodConnection
    FloodConnectionVisual
    Connection Opening
  Live Flow Direction
    Arrow Shaft
    Arrow Head Upper
    Arrow Head Lower
Main Camera
Directional Light
```

Both compartment GameObjects are children of **Connected Compartments Demo**,
and each `FloodVolume` explicitly references the same manager. Each compartment is a
3 m wide, 4 m long, 2 m high rectangular prism with a 24 m³ capacity.
**Flood Connection** links A to B at floor level; positive signed flow is from
A toward B. Its `FloodConnectionVisual` orients and scales **Live Flow
Direction** from applied flow.

The child shell cubes and water cubes are persistent scene GameObjects. Their
Mesh Renderers use the editable **Compartment Walls**, **Compartment A Water**,
**Compartment B Water**, and **Connection and Flow** material assets stored next
to the scene.

## Edit and tune before Play Mode

- Select **Compartment A (High Water)** or **Compartment B (Low Water)** and
  edit its **Flood Volume** component. **Width**, **Length**, and **Maximum
  Height** are meters; **Initial Volume** is cubic meters. Defaults are 3×4×2 m,
  with 6 m³ in A and 1 m³ in B. Initial volume is clamped to capacity.
- Select **Flood Connection** and edit its **Flood Connection** component.
  **Opening Width** and **Opening Height** are meters, **Discharge Coefficient**
  is dimensionless from 0 to 1, and **Is Open** toggles transfer. Its volume and
  manager references are already wired. The sibling **Flood Connection Visual**
  drives the live flow arrow; tune its flow thresholds without changing
  simulation.
- Select **Connected Compartments Demo** and edit **Flood Simulation Manager**
  to change **Ticks Per Second** or scheduling. The sample-only
  **Connected Compartments Bootstrap** component contains only Inspector-wired
  water visual references, water inset, and the readout's equalized height
  tolerance.
- Edit any of the four local `.mat` assets to change the authored colors,
  transparency, smoothness, or other URP/Lit presentation settings.

The presentation script does not create or replace scene objects. If you resize
a `FloodVolume`, adjust its Floor, wall, and initial Water Visual transforms to
match the authored dimensions in Edit Mode; Play Mode updates only each water
cube's fill height and footprint.

## Expected Play Mode behavior

Compartment A starts visibly deeper than Compartment B. Water flows through
the green opening from A to B while total water remains 7 m³. Transparent
walls, lowered front walls, contrasting water colors, and the elevated
orthographic camera keep both water levels visible. The bright green
world-space arrow points in the current applied-flow direction and grows
slightly with flow magnitude. The water cubes track each
`FloodVolume.CurrentHeight`. The Game view readout reports both volumes and
heights, requested and capacity-constrained applied flow in m³/s, and whether
the compartments are still equalizing.

Because both compartments have identical floor area, they settle at
approximately 3.5 m³ and 0.292 m water height each. Requested and applied flow
approach zero as the pressure heads equalize. Small differences below 0.01 m
are shown as **Equalized**.

For package diagnostics, select **Connected Compartments Demo** in the
Hierarchy and keep the Scene view visible during Play Mode. Its
`FloodDiagnostics` component discovers the child volumes and connection and
draws surface planes plus a connection arrow labeled with pressure head and
requested/applied flow. This diagnostic overlay is Editor-only visualization;
the Game view readout is sample-only presentation.
