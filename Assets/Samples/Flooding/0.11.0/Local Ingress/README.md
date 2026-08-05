# Local Ingress sample

This Unity 6.5 **URP showcase** compares **instant bulk free-surface
presentation** with **local ingress presentation**: a turbulent jet enters from
a breach, impacts the floor with droplets/spray/foam, and spreads as irregular
shallow water before converging to the authoritative room-wide surface.

Local ingress does **not** change flood simulation semantics. Total water volume,
transfers, and gameplay queries remain owned by `FloodVolume`.

## Import and open

Import **Local Ingress** from **Window > Package Management > Package Manager >
Flooding > Samples**. Unity copies it to:

`Assets/Samples/Flooding/0.12.0/Local Ingress`

Open `LocalIngress.unity` from that imported folder.

If the scene/materials need rebuilding after a package update:

**Flooding > Internal > Build Local Ingress Sample**

## What you should see (without reading the HUD)

### Major breach (default / key **3**)

1. Water blasts from the hull opening as a continuous turbulent jet.
2. Surface detail and soft edges move along the jet (not a flat transparent beam).
3. Droplets separate at impact; soft spray/mist adds scale.
4. Obvious whitewater/foam at the impact zone.
5. Shallow water spreads directionally with irregular, moving edges and a foam
   rim — not a static blue disc.
6. Over several seconds the local layer fades as the bulk free surface takes
   over.

No cube-shaped splash particles. No obviously rectangular water beam.

### Medium breach (**2**)

Visible jet, splash, and moderate foam — less violent than major; mist is
present but lighter.

### Tiny leak (**1**)

Ceiling trickle with few droplets, essentially no major foam, small shallow
patch.

### Stop flow (**O**)

Jet fades quickly; local water remains briefly, then converges smoothly.

Press **I** to toggle local ingress ON/OFF and compare against instant
equilibrium visuals for the same solver state.

## Controls

| Key | Action |
| --- | --- |
| **I** | Toggle local ingress presentation ON/OFF |
| **1** | Tiny leak preset |
| **2** | Medium breach preset |
| **3** | Major breach preset (+ secondary doorway) |
| **O** | Toggle primary breach open/closed (`IsOpen`) |
| **4** | Primary breach aperture 25% (`OpenFraction`) |
| **5** | Primary breach aperture 50% |
| **6** | Primary breach aperture 100% |
| **P** | Toggle secondary doorway open/closed |
| **R** | Reset compartment water volume |

Aperture keys change solver flow only; local ingress presentation continues to
follow the applied flow rate, not `OpenFraction` directly.

## Visual stack

- Procedural ballistic jet mesh (`FloodIngressStreamPresenter`)
- URP shaders: `Kyle/Flooding/Ingress Jet`, `Kyle/Flooding/Ingress Patch`
- Layered impact particles under `FloodIngressImpact`:
  - `Droplets` (stretched soft billboards)
  - `SprayMist` (soft additive mist)
  - `FoamBurst` (near-floor whitewater)
- Soft radial particle texture (`Ingress Soft Particle.png`)
- Multi-lobe irregular floor spread with edge foam from one logical patch per
  provider

Approximate Major Breach cost: single-digit / low-teens draw calls for the
primary ingress (jet + ≤3 patch lobes + 3 particle systems), with bounded
particle budgets (Droplets ≤96, SprayMist ≤64, FoamBurst ≤48).

## Limitations (v1)

- Not CFD; local patches are a visual proxy only.
- Local patches do not affect `QueryPoint` / gameplay depth.
- Residual overlap with the bulk surface can appear during mid-handoff.
- Rich jet/patch shaders require URP; Built-in/HDRP keep ballistic mesh + Lit /
  Particles transparent fallbacks.
