## Flooding Package – Proposed Changes and Design Intent

The flooding package should evolve from a simple “scale a cube upward” prototype into a modular, volume-based flooding system that can support irregular compartments, overflow, vessel movement, shifting water weight, and dynamic flow between spaces.

The intent is not to build full computational fluid dynamics. Instead, the package should simulate the parts that matter to gameplay:

- how much water exists,
- where its surface settles under gravity,
- where water can flow,
- how much mass it adds,
- and how that mass affects the object being flooded.

## 1. Make water volume the authoritative state

### Current behavior

`FloodSimulation` and `FloodVolume` store `CurrentVolume` as authoritative
state. New scenes author initial volume in cubic meters, immutable state
snapshots and events publish volume-derived values, and legacy initial-height
data is migrated explicitly. `CurrentHeight` remains an equivalent level-fill
value for compatibility and diagnostics.

```text
Water volume changes
    ↓
Rectangular height is derived
    ↓
Water cube scales upward
```

This is a sound baseline for a stationary rectangular room, but the derived
height and current presentation become insufficient once the room tilts or has
an irregular shape.

### Proposed change

Formalize the current amount of water as the stable public state:

```csharp
public float CurrentVolume { get; }
```

Water height and the visible surface are then derived values:

```text
Container geometry
+
Current water volume
+
Gravity direction
    ↓
Water surface position
```

### Intent

Volume remains valid regardless of:

- room shape,
- room rotation,
- floor slope,
- vessel tilt,
- or visual implementation.

This also makes inflow and outflow physically meaningful because all flow rates can use cubic meters per second.

---

## 2. Replace the local-Y water assumption with a gravity-aligned surface

### Current behavior

The current rectangular and polygon renderers clip their water meshes against a
gravity-aligned plane. Global or manager-specific gravity is supported, with a
stable fallback near zero gravity.

### Proposed change

Represent the water surface as a plane whose normal opposes gravity:

```csharp
public Plane SurfacePlane { get; }
```

In normal Unity gravity, the surface remains horizontal in world space even if the compartment rotates.

```text
Level room:

┌─────────────────┐
│                 │
│~~~~~~~~~~~~~~~~~│
└─────────────────┘


Tilted room:

       /──────────────/
      /        ~~~~~~/
     /~~~~~~~~~~~~~~/
    /______________/
```

### Intent

Water should settle according to gravity, not according to the orientation of the room.

The solved plane represents instantaneous equilibrium for gameplay. Transient
slosh, surge, delayed settling, and oscillation after acceleration or rotation
remain out of scope.

This allows flooding behavior to remain believable when:

- a ship lists,
- a vehicle rolls,
- a building section collapses,
- or a movable container rotates.

---

## 3. Separate simulation geometry from rendered water

### Current behavior

The scaled cube represents both:

- the calculated water volume,
- and the visible water.

### Proposed change

Create separate responsibilities:

```text
FloodVolume
    Simulation state and container definition

FloodSurfaceRenderer
    Generates or positions the visible water surface

FloodWaterBodyRenderer
    Optional underwater sides or volume visuals

FloodEffects
    Foam, spray, particles and splashes
```

The simulation should expose data such as:

```csharp
public float CurrentVolume { get; }
public Plane SurfacePlane { get; }
public Vector3 WaterCenterOfMassWorld { get; }
```

The renderer consumes that state without changing it.

### Intent

This allows the visual implementation to be replaced without modifying the simulation.

The same flooding model could use:

- the current transparent cube,
- a generated polygon mesh,
- a custom URP shader,
- an Asset Store water system,
- or a high-quality HDRP surface.

---

## 4. Introduce a reusable container geometry model

### Current behavior

The floodable space is defined using:

```csharp
width
length
maximumHeight
```

That limits every room to a rectangular prism.

### Proposed change

Introduce a container representation that can progress through multiple levels of complexity.

### Phase 1: Polygon footprint with vertical walls

Define the room floor using a 2D polygon:

```csharp
[SerializeField]
private List<Vector2> footprint;
```

This supports:

- rectangles,
- L-shaped rooms,
- corridors,
- triangles,
- trapezoids,
- irregular ship compartments.

For vertical walls:

```text
capacity at height = footprint area × height
```

### Phase 2: Baked three-dimensional flood geometry

For sloped floors, curved hulls, or uneven interiors, create editor-time baked data:

```text
FloodVolumeAuthoring
        ↓ Bake
FloodVolumeData.asset
```

The baked data may contain:

```csharp
Bounds LocalBounds;
float CellSize;
FloodCell[] Cells;
float TotalCapacity;
```

### Intent

Complex geometry analysis should happen in the Unity Editor rather than repeatedly during gameplay.

Runtime code should operate on compact, preprocessed data.

---

## 5. Solve water-plane position from volume

Once the compartment can tilt, water height is no longer simply:

```text
volume / floor area
```

### Proposed change

Given a candidate gravity-aligned plane, calculate how much of the container lies beneath it.

Then solve:

```text
SubmergedVolume(planePosition) = CurrentVolume
```

A binary search can find the correct plane position:

```text
Choose midpoint plane
    ↓
Calculate submerged volume
    ↓
Too much water below plane?
Move plane downward
    ↓
Too little?
Move plane upward
    ↓
Repeat
```

For baked cells, the calculation can approximate how much of each cell is below the plane.

### Intent

This supports real-time water redistribution inside tilted or irregular compartments without simulating individual particles.

The calculation can run at a fixed simulation rate such as 10–20 Hz and visually interpolate between results.

---

## 6. Calculate the water’s center of mass

### Current behavior

`FloodVolume` already reports configurable-density water mass and the
gravity-aligned submerged centroid in world space. It does not yet aggregate
those contributions or apply them to a parent physics body.

### Proposed change

Expose the mass and center of mass of the flooded portion:

```csharp
public float WaterMass =>
    CurrentVolume * WaterDensity;

public Vector3 WaterCenterOfMassWorld { get; }
```

For water density:

```text
approximately 1000 kg/m³
```

Each submerged geometry sample contributes to a weighted average:

```text
Water COM =
sum(cell position × submerged cell volume)
/
total submerged volume
```

### Intent

Water should affect the object differently depending on where it accumulates.

For example:

```text
30 m³ centered on port side
≈
30,000 kg of added mass on port side
```

That creates a rolling moment rather than simply increasing total weight.

---

## 7. Add a vessel or parent-body integration layer

### Proposed component

```csharp
FloodMassContributor
```

or:

```csharp
FloodedBodyController
```

This system gathers mass contributions from every `FloodVolume` attached to the same vessel:

```text
Compartment A water mass
+
Compartment B water mass
+
Compartment C water mass
    ↓
Combined flood mass and center of mass
```

It then supplies that information to the vessel physics system.

A clean boundary could look like:

```csharp
public interface IMassContributor
{
    float Mass { get; }

    Vector3 CenterOfMassWorld { get; }
}
```

### Intent

`FloodVolume` should not directly rotate, sink, or move the ship.

Its responsibility is to report water state.

A separate physics system decides how the added mass affects:

- total mass,
- rigidbody center of mass,
- roll,
- pitch,
- draft,
- buoyancy,
- and stability.

This keeps the flooding package reusable outside ship simulations.

---

## 8. Replace simple overflow with generalized fluid connections

### Implemented baseline

`FloodConnection` now transfers finite water volume bidirectionally between two
managed rectangular or polygon-prism compartments, including rotated
compartments with gravity-aligned pressure heads. Its Transform marks the
opening bottom, and authored width and height define a vertical rectangular
opening.

The manager evaluates all connections from one snapshot, scales competing
outflow by source availability, scales incoming flow by destination capacity,
and commits one delta per volume.

### Remaining expansion

The general connection component is:

```csharp
public sealed class FloodConnection : MonoBehaviour
{
    public FloodVolume VolumeA;
    public FloodVolume VolumeB;

    public float OpeningWidth;
    public float OpeningHeight;
    public bool IsOpen;
}
```

Connections represent:

- doorways,
- hull breaches,
- windows,
- vents,
- pipes,
- stairwells,
- drains,
- wall edges,
- overflow points.

The connection determines whether the opening is submerged relative to each volume’s current water plane.

### Intent

“Spilling over a wall” should not be a special case.

It should be the same general behavior as water flowing through any opening:

```text
Water reaches opening elevation
    ↓
Pressure or level difference exists
    ↓
Water transfers through connection
```

---

## 9. Support bidirectional pressure-driven flow

### Implemented baseline

Connections calculate flow direction and rate from the water pressure head on
both sides. `FloodSource` remains available as a fixed infinite external
boundary.

### Remaining expansion

The current approximation is:

A simplified approximation can use:

```text
Q = Cd × A × √(2gh)
```

Where:

```text
Q  = flow rate in m³/s
Cd = discharge coefficient
A  = submerged opening area
g  = gravity
h  = pressure-head difference
```

The sign of the pressure difference determines direction.

### Intent

This creates natural behavior for:

- water entering through a hull breach,
- two rooms equalizing,
- water reversing direction after the vessel tilts,
- overflow increasing as water rises,
- and flow stopping once pressure equalizes.

---

## 10. Model the exterior ocean as a fluid boundary

### Current behavior

A hull breach would likely be represented as a `FloodSource`.

### Proposed change

Create an external fluid representation:

```csharp
public sealed class ExternalFluidBody : MonoBehaviour
{
    public Plane SurfacePlane;
    public float Density;
}
```

A hull opening connects:

```text
ExternalFluidBody
        ↕
FloodConnection
        ↕
FloodVolume
```

### Intent

The breach should not magically generate water.

Its flow should depend on:

- the external waterline,
- breach depth,
- interior pressure,
- vessel orientation,
- and opening area.

As the ship lists and the breach moves deeper, inflow may increase automatically.

---

## 11. Introduce a fixed-step simulation manager

### Implemented baseline

`FloodSimulationManager` now runs registered volumes and sources at a
configurable fixed rate. It captures volume snapshots, aggregates source
requests, reconciles destination capacity, commits changes, and publishes state
after commit.

`FloodSource` no longer changes its target from its own `Update()`, and
`FloodVolume` no longer polls from `LateUpdate`.

### Remaining expansion

The existing manager must expand as connections and geometry are introduced:

```csharp
FloodSimulationManager
```

The manager executes all flooding logic in a predictable order:

```text
1. Read vessel transforms and gravity
2. Resolve each volume’s water surface
3. Evaluate openings and pressure differences
4. Calculate connection flows
5. Apply volume transfers simultaneously
6. Recalculate water centers of mass
7. Publish state-change events
```

The manager can run at a configured rate:

```text
10–20 simulation ticks per second
```

Rendering can still run every frame and interpolate visual state.

### Intent

This avoids update-order bugs and prevents one connection from using partially updated state.

It also improves:

- determinism,
- performance,
- debugging,
- replayability,
- and future save/load support.

Applying transfers simultaneously is especially important. Each connection should first calculate its intended transfer from the same snapshot, then all transfers should be committed together.

---

## 12. Make audio flow-driven

### Proposed components

```text
FloodSourceAudio
FloodConnectionAudio
FloodVolumeAudio
```

Audio should react to measured simulation values:

```csharp
public float CurrentFlowRate { get; }
public float SubmergedDepth { get; }
public float FillPercentage { get; }
```

Example behavior:

```text
Very low flow
→ dripping or trickling

Moderate flow
→ continuous stream

High flow
→ heavy rushing water

Overflow
→ waterfall and splashing

Mostly submerged room
→ muffled environmental ambience
```

### Intent

Sounds should originate from the physical event causing them.

A breach sound comes from the breach. A waterfall sound comes from the overflow edge. General room ambience belongs to the compartment.

This makes the soundscape spatially meaningful and avoids generic water loops.

---

## 13. Add presentation components for overflow

### Proposed component

```csharp
FloodConnectionVisual
```

It consumes connection state and selects an appropriate visual response:

```text
Low flow
→ droplets or particles

Medium flow
→ narrow flowing mesh

High flow
→ broad waterfall mesh, spray and foam
```

Example API:

```csharp
public void ApplyFlowState(
    float flowRate,
    float submergedOpeningArea,
    Vector3 flowDirection);
```

### Intent

The simulation only needs to determine the amount and direction of flow.

The presentation layer decides how to make that flow look convincing.

This avoids expensive particle-based fluid simulation while still allowing strong visual feedback.

---

## 14. Add change events and stable public interfaces

The package should expose events such as:

```csharp
public event Action<FloodState> StateChanged;
public event Action<float> VolumeChanged;
public event Action<Plane> SurfacePlaneChanged;
```

A state snapshot might contain:

```csharp
public readonly struct FloodState
{
    public float Volume { get; init; }
    public float Capacity { get; init; }
    public Plane SurfacePlane { get; init; }
    public float WaterMass { get; init; }
    public Vector3 CenterOfMassWorld { get; init; }
}
```

### Intent

Other systems should react to flooding without depending on its internal implementation.

Consumers may include:

- character wetness,
- swimming,
- buoyancy,
- electrical hazards,
- door pressure,
- audio,
- UI,
- objectives,
- AI navigation,
- vessel stability.

---

# Resulting feedback loop

Once these changes exist, the system can support the real-time behavior you described:

```text
A breach introduces water into one compartment
        ↓
The compartment gains water volume
        ↓
The new water mass shifts the vessel’s center of mass
        ↓
The vessel rolls or pitches
        ↓
Every compartment rotates relative to gravity
        ↓
Water surface planes and centers of mass are recalculated
        ↓
Some doors, edges or breaches become more deeply submerged
        ↓
Connection flow rates change
        ↓
Water redistributes
        ↓
The vessel’s mass distribution changes again
```

The intent is for this behavior to emerge from the interaction of small reusable systems rather than from scripted sinking stages.

## Recommended implementation order

The work should continue incrementally. Steps 1–7 are implemented:

1. [Complete] Keep the current rectangular volume working.
2. [Complete] Make `CurrentVolume` the authoritative state.
3. [Complete] Separate the surface renderer from simulation.
4. [Complete] Add connections between two stationary rectangular rooms.
5. [Complete] Add polygon footprints with vertical walls.
6. [Complete] Support gravity-aligned surfaces for rotated containers.
7. [Complete] Calculate water center of mass.
8. Integrate water mass with a test Rigidbody.
9. Add baked geometry for complex compartments.
10. Add an exterior ocean boundary and buoyancy integration.

This preserves the working prototype while replacing one assumption at a time.
