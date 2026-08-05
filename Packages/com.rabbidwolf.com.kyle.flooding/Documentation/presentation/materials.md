# Presentation materials and shaders

Package materials live under
`Packages/com.rabbidwolf.com.kyle.flooding/Materials/`. They are presentation
assets only — swapping materials never changes simulation.

## Package materials

| Asset | Shader / type | Role |
| --- | --- | --- |
| `Floodwater.mat` | URP Lit transparent | Default bulk water for surface renderers |
| `FloodUnderwater.mat` | `Kyle/Flooding/Underwater` | Fullscreen URP underwater / waterline pass |
| `Transparent_Mat.mat` | URP Lit transparent | Generic transparent fallback |

Paths:

```text
Packages/com.rabbidwolf.com.kyle.flooding/Materials/Floodwater.mat
Packages/com.rabbidwolf.com.kyle.flooding/Materials/FloodUnderwater.mat
Packages/com.rabbidwolf.com.kyle.flooding/Materials/Transparent_Mat.mat
```

## Render pipeline notes

| Need | URP project | Built-in / HDRP / custom |
| --- | --- | --- |
| Bulk water mesh | Assign `Floodwater.mat` (or your URP transparent) | Assign your own transparent material |
| Underwater fullscreen FX | `FloodUnderwater.mat` on the renderer feature | Effect unavailable (`Kyle.Flooding.URP` excluded) |
| Core sim / tracker / audio / telemetry | Works | Works |

If water looks pink/magenta, the Mesh Renderer’s shader is missing or
incompatible with the active pipeline — assign a valid transparent material.

## Where to assign them

### Bulk surface water

1. Select the child water visual GameObject (`Water Visual` / `Water Surface`).
2. On its **Mesh Renderer**, set **Material** to `Floodwater` (URP) or your own
   transparent material.
3. Keep surface type transparent and a reasonable alpha so submerged geometry
   remains readable.

Used by: [surface renderers](surface-renderers.md).

### Underwater camera pass

1. Open the active URP Renderer asset.
2. Select **Flood Underwater Renderer Feature**.
3. Set **Material** to `FloodUnderwater`.

Full setup: [Underwater URP](../components/flood-underwater-urp.md).

### Local ingress (sample / URP)

Polished ingress jet and patch looks use shaders in `Kyle.Flooding.URP`:

| Shader | Typical use |
| --- | --- |
| `Kyle/Flooding/Ingress Jet` | Ballistic stream mesh |
| `Kyle/Flooding/Ingress Patch` | Floor-spread lobes |

Sample materials and bootstrapping live under
`Samples~/Local Ingress/` (imported via Package Manager Samples). Without URP,
ingress still runs with Lit / Particles fallbacks.

Guide: [Local ingress](../local-ingress.md).

## FloodUnderwaterProfile (look asset, not a material)

Underwater tint, fog, distortion, and blend timing are stored in a
**Flood Underwater Profile** ScriptableObject
(**Assets > Create > Flooding > Flood Underwater Profile**), not on
`FloodUnderwater.mat`.

| Concern | Where to tune |
| --- | --- |
| Tint / fog / distortion / transition | Profile asset |
| Waterline softness (meters) | Renderer feature **Waterline Softness Meters** |
| Pass material reference | Renderer feature **Material** → `FloodUnderwater` |

## Practical tips

- Prefer one shared `Floodwater` material (or a project variant) across rooms for
  consistent look.
- Do not expect package materials to work on Built-in/HDRP without replacement.
- Local ingress profile foam/tint fields drive URP ingress shaders when those
  materials are assigned on the presenter.
- Presentation materials never affect capacity, fill, or queries.

## Related

- [Surface renderers](surface-renderers.md)
- [Underwater URP](../components/flood-underwater-urp.md)
- [Local ingress](../local-ingress.md)
- [Tune underwater look](../editor-workflow.md#tune-underwater-look-symptom--where-to-click)
