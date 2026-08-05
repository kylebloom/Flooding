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

## Visual stack (presentation only)

| Element | Implementation |
| --- | --- |
| Jet | Procedural tapered tube deformed on a ballistic curve (`FloodIngressJetMesh`) |
| Gravity | `FloodSimulationManager.ActiveGravity` (fallback `Physics.gravity`) |
| Jet motion | URP `Kyle/Flooding/Ingress Jet` (dual-layer flow noise, soft edges, Fresnel/specular, alpha breakup), or Lit/color fallback |
| Impact layers | Pooled hierarchy `FloodIngressImpact` → `Droplets` / `SprayMist` / `FoamBurst` |
| Droplets | Soft-alpha stretched billboards; ballistic, gravity-influenced |
| Spray mist | Soft billboards; medium+ flow only (`Spray Mist Threshold`) |
| Foam burst | Near-floor whitewater particles; scales with foam/splash strength |
| Floor spread | One logical patch → up to 3 deterministic visual lobes, directional stretch |
| Patch look | URP `Kyle/Flooding/Ingress Patch` irregular mask, moving normals, edge foam band, ripples — or Lit fallback |

Assembly split:

| Assembly | Owns |
| --- | --- |
| `Kyle.Flooding.Runtime` | Sampler, presentation state, presenters, jet/disc meshes, profile, generic particle hooks |
| `Kyle.Flooding.URP` | Polished ingress jet/patch shaders and other URP visual helpers |

The included Local Ingress sample intentionally uses the URP backend for showcase
quality. Built-in/HDRP keep the ballistic mesh and soft particle materials with
Lit/Particles fallbacks where shaders are unavailable.

Expected cost per active major ingress: single-digit to low-teens draw calls
(1 jet + ≤3 patch lobes + up to 3 particle systems), with bounded particle counts
(typically well under ~200 concurrent for a tuned major breach). Cheap vertex
updates on a reused jet mesh; no per-frame Instantiate, Destroy, or runtime
texture generation.

## Setup in the Unity Editor

### 1. Create a profile

1. **Assets > Create > Flooding > Flood Ingress Presentation Profile**
2. Tune lifecycle size, **Jet**, **Directional Spread**, **Splash**, and **Foam**
   groups, plus flow→stream/spread/splash curves.
3. Foam fields (`Foam Color`, `Foam Strength`, `Foam Edge Width`,
   `Foam Noise Scale`, `Foam Scroll Speed`) drive the URP patch edge band.
4. `Spray Mist Threshold` / `Foam Burst Threshold` gate the secondary impact
   particle layers.

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
4. Optionally add **Flood Ingress Stream Presenter** children (jet + impact
   layers) and assign them in **Stream Presenters** (index-aligned: connections
   first, then sources). Prefer materials using `Kyle/Flooding/Ingress Jet` and
   `Kyle/Flooding/Ingress Patch` under URP.

### 4. Configure impact particle layers

On each `Flood Ingress Stream Presenter`:

1. Create a child `FloodIngressImpact` (optional organizational root).
2. Add three `ParticleSystem` children:
   - **Droplets** — stretched billboard, soft circular alpha texture, gravity
   - **SprayMist** — soft billboard, low opacity, wider cone
   - **FoamBurst** — white/light cyan billboard near the floor, expands/fades
3. Assign them to **Droplet Particles**, **Spray Mist Particles**, and
   **Foam Burst Particles**.
4. Use transparent URP Particles/Unlit (or equivalent) with a soft radial alpha
   texture. Do **not** use opaque textureless quads or Cube mesh particles for
   water spray.

`Splash Particles` remains a compatibility alias for **Droplet Particles**.

Auto-discover (when enabled) runs only on enable / `RefreshProviders()`, never
every frame.

### 5. Keep the bulk surface

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
| Particle counts | Bounded per layer (`maxParticles` authored on each system) |
| Allocations | Fixed sample/patch/disc arrays; shared unit-disc mesh; MPB color updates |
| Scene search | Explicit lists; one-time discover only |
| Instantiate/Destroy | Disc slots pooled under the presenter; no per-frame spawn |
| Draw calls | Jet + patch lobes + up to 3 impact particle systems per active stream |

Expected cost for a typical compartment: a handful of transparent draws and
simple LateUpdate sampling/math. Aim for single-digit / low-teens draw calls per
active major ingress rather than optimizing below that at the expense of look.

## Limitations (v1)

- Not CFD; no physically simulated sloshing or pressure-driven local surface shape.
- Intended for visually plausible early ingress in mostly horizontal floors.
- Local patches do **not** affect `QueryPoint` / gameplay depth.
- Residual visual overlap with the bulk surface can appear during mid-handoff.
- Complex ramps/stairs may need custom presentation.
- Polished jet/patch appearance is URP-first; other pipelines use simpler fallbacks.

## Sample

Import **Local Ingress** from Package Manager Samples, or rebuild with
**Flooding > Internal > Build Local Ingress Sample**. See
`Samples~/Local Ingress/README.md`.

## Related presentation docs

- [Presentation guides hub](presentation/README.md)
- [Surface renderers](presentation/surface-renderers.md) (keep bulk water)
- [Connection visual](presentation/flood-connection-visual.md)
- [Materials / ingress shaders](presentation/materials.md)
- [Scenario 10](editor-workflow.md#scenario-10--local-ingress-presentation-vs-instant-bulk-surface)
