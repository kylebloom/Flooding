# Local Ingress sample

This Unity 6.5 sample compares **instant bulk free-surface presentation** with
**local ingress presentation**: a curved jet enters from a breach, impacts the
floor with splash, and spreads as irregular shallow water before converging to
the authoritative room-wide surface.

Local ingress does **not** change flood simulation semantics. Total water volume,
transfers, and gameplay queries remain owned by `FloodVolume`.

## Import and open

Import **Local Ingress** from **Window > Package Management > Package Manager >
Flooding > Samples**. Unity copies it to:

`Assets/Samples/Flooding/0.11.0/Local Ingress`

Open `LocalIngress.unity` from that imported folder.

If the scene/materials need rebuilding after a package update:

**Flooding > Internal > Build Local Ingress Sample**

## What you should see (without reading the HUD)

### Major breach (default / key **3**)

1. Water emerges from the hull opening as a tapered jet.
2. The jet curves under gravity toward the floor.
3. Impact produces a visible splash.
4. Shallow water spreads directionally away from the impact with irregular,
   moving edges (not a perfect circle).
5. Over several seconds the local layer fades as the bulk free surface takes
   over.

### Medium breach (**2**)

Clearly visible stream, impact, and directional shallow spread — less violent
than major.

### Tiny leak (**1**)

Narrow trickle, minimal splash, small local puddle.

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
| **O** | Toggle primary breach open/closed |
| **P** | Toggle secondary doorway open/closed |
| **R** | Reset compartment water volume |

## Visual stack

- Procedural ballistic jet mesh (`FloodIngressStreamPresenter`)
- Optional URP shaders: `Kyle/Flooding/Ingress Jet`, `Kyle/Flooding/Ingress Patch`
- Pooled impact `ParticleSystem`
- Multi-lobe irregular floor spread from one logical patch per provider

## Limitations (v1)

- Not CFD; local patches are a visual proxy only.
- Local patches do not affect `QueryPoint` / gameplay depth.
- Residual overlap with the bulk surface can appear during mid-handoff.
- Rich jet/patch shaders require URP; Built-in/HDRP keep ballistic mesh + Lit
  transparent fallback.
