# Local ingress presentation

Local ingress presentation is a **transient visual approximation** of how newly
entering water might spread before visually converging with the authoritative
bulk free surface. It does **not** change flood simulation semantics.

```text
FloodConnection / FloodSource
            |
            +----------------------+
            |                      |
            v                      v
Authoritative volume        Ingress presentation
simulation                  anchor + rate
            |                      |
            v                      v
Bulk free surface          stream / shallow spread
            \                      /
             \                    /
              ---- visual blend ---
```

## Core rule

| Owner | Responsibility |
| --- | --- |
| `FloodVolume` / solver | Total water volume, transfers, flood state, gameplay queries |
| Local ingress presentation | Stream, shallow local pool, opacity handoff to bulk surface |

Do not treat local patches as a second water-volume simulation. Presentation may
track a flow **impulse / visual weight** to drive radius; authoritative cubic
meters remain exclusively on `FloodVolume`.

## Setup in the Unity Editor

### 1. Create a profile

1. **Assets > Create > Flooding > Flood Ingress Presentation Profile**
2. Tune spread speed, max radius, settling / convergence durations, minimum flow,
   max patches, and flow→stream/spread/splash curves.

### 2. Configure presentation anchors

On each `FloodConnection` or `FloodSource` that should show localized entry:

1. Select the connection/source GameObject.
2. Under **Presentation**, optionally assign **Ingress Anchor**.
3. Fallbacks:
   - `FloodConnection`: `OpeningCenterWorld` (opening mid-height), not the
     opening bottom-center transform position.
   - `FloodSource`: the component Transform position; forward is ingress
     direction.

Simulation ignores `Ingress Anchor`.

### 3. Add the presenter

1. Create or select a GameObject under the flooded compartment (for example
   `Local Ingress Presenter`).
2. **Add Component > Flooding > Flood Local Ingress Presenter**.
3. Assign:
   - **Volume** — destination `FloodVolume`
   - **Profile** — the ingress profile asset
   - **Floor Plane** — Transform whose position is a point on the floor and
     **up** is the floor normal
   - **Patch Material** — transparent water-compatible material
   - **Connections** / **Sources** — explicit providers (preferred)
4. Optionally add **Flood Ingress Stream Presenter** children and assign them in
   **Stream Presenters** (index-aligned: connections first, then sources).

Auto-discover (when enabled) runs only on enable / `RefreshProviders()`, never
every frame.

### 4. Keep the bulk surface

Leave `FloodCubeSurfaceRenderer` / other `FloodSurfaceRenderer` components in
place. Early ingress is locally dominant; after convergence the bulk surface
dominates. v1 does **not** add `VisualFillWeight` to the bulk renderer.

## Convergence model

Each provider owns at most one patch:

1. **Growing** — inflow above minimum flow; radius expands from flow impulse and
   profile spread curves.
2. **Settling** — provider stopped or disappeared; patch remains visible for
   `Settling Duration`.
3. **Converging** — handoff 0→1 over `Convergence Duration`; local opacity fades
   as `(1 - handoff) * strength`.
4. **Inactive** — slot freed after full handoff.

Stopping never pops a patch instantly.

## Multiple ingress points

- One persistent patch per provider (`EntityId`).
- No geometric merge across providers.
- When the bounded array is full: reuse Inactive, else weakest Converging, else
  ignore a weaker new sample. Provider ownership is preserved.

## Floor projection

The floor Transform is a presentation plane:

- position = point on floor
- up = floor normal

Patches align to that normal (not assumed world +Y). Optional one-shot raycast
along `-floorNormal` runs only when a patch slot is first activated/reused.
Configurable **Floor Offset** reduces Z-fighting.

## Performance characteristics (v1 design)

| Concern | Approach |
| --- | --- |
| CFD / particles-as-fluid | Not used |
| Patch count | Bounded (`Maximum Simultaneous Patches`, default 8) |
| Allocations | Fixed sample/patch/disc arrays; shared unit-disc mesh; MPB color updates |
| Scene search | Explicit lists; one-time discover only |
| Instantiate/Destroy | Disc slots pooled under the presenter; no per-frame spawn |
| Draw calls | One disc mesh renderer per active patch + optional stream mesh |

Expected cost for a typical compartment: a handful of transparent quads/discs and
simple LateUpdate sampling/math.

## Limitations (v1)

- Not CFD; no physically simulated sloshing or pressure-driven local surface shape.
- Intended for visually plausible early ingress in mostly horizontal floors.
- Local patches do **not** affect `QueryPoint` / gameplay depth.
- Residual visual overlap with the bulk surface can appear during mid-handoff.
- Complex ramps/stairs may need custom presentation.
- Splash is deliberately simple (stream + optional particle emission scaling).

## Sample

Import **Local Ingress** from Package Manager Samples, or rebuild with
**Flooding > Internal > Build Local Ingress Sample**. See
`Samples~/Local Ingress/README.md`.
