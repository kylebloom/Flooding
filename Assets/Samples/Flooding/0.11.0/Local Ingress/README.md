# Local Ingress sample

This Unity 6.5 sample compares **instant bulk free-surface presentation** with
**local ingress presentation**: water visibly enters at a breach, spreads as a
shallow local pool, then converges toward the authoritative room-wide surface.

Local ingress does **not** change flood simulation semantics. Total water volume,
transfers, and gameplay queries remain owned by `FloodVolume`.

## Import and open

Import **Local Ingress** from **Window > Package Management > Package Manager >
Flooding > Samples**. Unity copies it to:

`Assets/Samples/Flooding/0.11.0/Local Ingress`

Open `LocalIngress.unity` from that imported folder.

If the scene is missing after a source checkout, rebuild in the Editor:

**Flooding > Internal > Build Local Ingress Sample**

> Re-importing this sample or upgrading the package can replace the copy under
> `Assets/Samples`. Move or rename an imported copy before re-importing if you
> want to preserve local changes.

## What to look for

1. Enter Play Mode with **Local Ingress ON** (default).
2. Water pours in at the primary hull breach with a stream and expanding shallow
   pool near the opening.
3. After several seconds the local patch fades while the bulk
   `FloodCubeSurfaceRenderer` dominates.
4. Press **I** to toggle local ingress presentation OFF and compare against
   instant equilibrium visuals for the same solver state.

## Controls

| Key | Action |
| --- | --- |
| **I** | Toggle local ingress presentation ON/OFF |
| **1** | Tiny leak preset (`FloodSource`, primary breach closed) |
| **2** | Medium breach preset (smaller opening) |
| **3** | Major breach preset (large opening + secondary doorway) |
| **O** | Toggle primary breach open/closed |
| **P** | Toggle secondary doorway open/closed |
| **R** | Reset compartment water volume |

## HUD metrics

- Authoritative Volume / Fill %
- Current Inflow Rate
- Local Ingress ON/OFF
- Active Local Patches
- Oldest Patch Age
- Current Handoff %

## Authored hierarchy (summary)

```text
Local Ingress Demo
  FloodSimulationManager / FloodDiagnostics / LocalIngressBootstrap
  Large Compartment
    Floor / Walls / Ceiling
    Room Volume
      FloodVolume
      FloodCubeSurfaceRenderer
      Local Ingress Presenter
  External Ocean + Primary Breach (+ stream)
  Adjacent Flooded Room + Secondary Doorway
  Ceiling Leak Source
  Main Camera / Directional Light
```

## Limitations (sample / v1)

- Not CFD; local patches are a visual proxy only.
- Local patches do not affect `QueryPoint` / gameplay depth.
- Residual overlap with the bulk surface can appear during mid-handoff.
- Splash is intentionally simple (stream + optional particles).
