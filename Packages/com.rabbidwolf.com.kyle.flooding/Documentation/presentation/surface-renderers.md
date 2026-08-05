# Surface renderers

Surface renderers turn immutable `FloodState` into a visible water mesh. They
subscribe to volume or region state, optionally interpolate, and never write
simulation values.

## Renderer chooser

| Geometry / ownership | Component | Child visual |
| --- | --- | --- |
| Standalone **Rectangular Prism** | `FloodCubeSurfaceRenderer` | Transform with Mesh Filter (preferred) or cube scale fallback |
| Standalone **Extruded Polygon** | `FloodPolygonSurfaceRenderer` | Mesh Filter + Mesh Renderer |
| Standalone **Baked Data** | `FloodBakedSurfaceRenderer` | Mesh Filter + Mesh Renderer |
| **FloodRegion** (shared plane) | `FloodRegionSurfaceRenderer` | Transform with Mesh Filter |
| Legacy scenes (`FloodWaterVisual`) | Replace with `FloodCubeSurfaceRenderer` | Same child |

`FloodRegionSurfaceRenderer` does **not** inherit `FloodSurfaceRenderer`. Use it
on the region; disable any member-volume surface renderers.

## Shared base (`FloodSurfaceRenderer`)

Used by cube, polygon, and baked renderers.

### Use this when

- One standalone `FloodVolume` owns its water state.
- You want smoothed visual updates between published states.

### Key Inspector fields

| Field | Unit / notes | Default |
| --- | --- | --- |
| **Flood Volume** | Source volume; auto `GetComponent` on same GameObject | — |
| **Interpolation Duration** | Seconds; `0` snaps immediately | `0.1` |

### Runtime API

- `SourceVolume`, `InterpolationDuration`, `DisplayedState`
- `SnapToCurrentState()` — apply current volume state immediately

### Region members

If the assigned volume is a `FloodRegion` member, the base logs a warning on
enable. Disable the member renderer and use
`FloodRegionSurfaceRenderer` instead.

---

## FloodCubeSurfaceRenderer

Presents rectangular-prism water as a gravity-aligned submerged mesh from the
solved surface plane. Without a Mesh Filter on the child, falls back to scaling
the child along local Y (local gravity only — prefer Mesh Filter).

### Use this when

- Geometry mode is **Rectangular Prism**.
- Fastest bulk water for rooms and prefabs (`Room.prefab`).

### Beginner setup

1. Select the compartment GameObject that has **Flood Volume**.
2. Create child GameObject `Water Visual`.
3. Add built-in **Mesh Filter** and **Mesh Renderer** to `Water Visual`.
4. Assign a transparent material (package
   [`Floodwater.mat`](materials.md) on URP, or your own).
5. On the compartment, **Add Component > Flood Cube Surface Renderer**
   (or attach on the same GameObject as the volume).
6. Assign **Water Visual** to the child Transform.
7. Keep **Minimum Visible Height** at `0.01` m initially.

### Key Inspector fields

| Field | Unit / notes | Default |
| --- | --- | --- |
| *(base fields)* | See above | |
| **Water Visual** | Child Transform; Mesh Filter required for gravity-aligned mesh | — |
| **Minimum Visible Height** | Hide below this height (m) | `0.01` |

### Verification checklist

1. Enter Play Mode with water rising.
2. Confirm the mesh grows with fill and follows the solved plane when the
   compartment (or gravity) is tilted.
3. Confirm the visual hides when height is near empty.

### Common mistakes

- Using this renderer with polygon or baked geometry (visual stays inactive).
- Leaving it enabled on a region member (double draw / seams).
- Pink or invisible mesh: wrong or missing transparent material for your RP.

---

## FloodPolygonSurfaceRenderer

Generates a submerged mesh from an extruded footprint and the solved plane.

### Use this when

- Geometry mode is **Extruded Polygon**.

### Beginner setup

1. Author a valid polygon footprint on **Flood Volume**.
2. Create child `Water Visual` with **Mesh Filter** + **Mesh Renderer**.
3. Assign a transparent material.
4. **Add Component > Flood Polygon Surface Renderer** on the compartment.
5. Assign **Water Mesh Filter** (or leave empty to resolve from children).
6. Keep **Minimum Visible Height** at `0.01` m.

### Key Inspector fields

| Field | Unit / notes | Default |
| --- | --- | --- |
| *(base fields)* | | |
| **Water Mesh Filter** | Target whose mesh is generated | Auto children |
| **Minimum Visible Height** | Disable Mesh Renderer below height (m) | `0.01` |

### Common mistakes

- Invalid / self-intersecting polygon (simulation may already reject it).
- Cube renderer still attached instead of polygon renderer.
- Expecting holes in the footprint (unsupported).

---

## FloodBakedSurfaceRenderer

Presents the **free surface** for baked geometry: contours from occupancy /
presentation-boundary intersection with the gravity plane. It does not render a
full volumetric fill mesh and never analyzes a live source Mesh Filter at
runtime.

### Use this when

- Geometry mode is **Baked Data** with a current `FloodVolumeData` bake.

### Beginner setup

1. Complete the bake workflow on `FloodVolumeAuthoring` (see
   [Editor workflow Scenario 5](../editor-workflow.md#scenario-5--sloped-or-uneven-interior-baked-data)).
2. Create child `Water Surface` with **Mesh Filter** + **Mesh Renderer**.
3. Assign a transparent material.
4. **Add Component > Flood Baked Surface Renderer**.
5. Assign **Water Mesh Filter**.
6. Prefer baking a **presentation boundary** for clean contours.

### Key Inspector fields

| Field | Unit / notes | Default |
| --- | --- | --- |
| *(base fields)* | | |
| **Water Mesh Filter** | Child Mesh Filter for the free-surface mesh | Auto children |
| **Minimum Visible Volume** | Hide Mesh Renderer below this volume (m³) | `0.001` |

### Behavior notes

- Hidden when nearly empty or nearly full (within solver absolute volume of
  capacity).
- Contours need at least three vertices to draw.
- Volume comes from bake occupancy cells; contours prefer the presentation
  boundary when present.

### Common mistakes

- Stale bake asset after changing the source mesh.
- Using cube/polygon renderers on baked volumes.
- Expecting a solid water body mesh instead of a free-surface sheet.

---

## FloodRegionSurfaceRenderer

One continuous submerged mesh for a `FloodRegion` shared `FloodState` and
composite geometry. Required for seamless first-person water across unrestricted
doorways inside a region.

### Use this when

- Multiple `FloodVolume` members share one region water body.
- You need a single waterline for camera tracking and visuals.

### Beginner setup

1. Select the **Flood Region** GameObject.
2. Create child `Water Visual` with **Mesh Filter** + **Mesh Renderer**.
3. Assign a transparent material.
4. **Add Component > Flood Region Surface Renderer**.
5. Assign **Flood Region** (auto on same GameObject) and **Water Visual**.
6. On every member volume, **disable** any `FloodSurfaceRenderer`
   (`FloodCube` / `FloodPolygon` / `FloodBaked`).

### Key Inspector fields

| Field | Unit / notes | Default |
| --- | --- | --- |
| **Flood Region** | Region driving the mesh | Auto same GO |
| **Water Visual** | Child Transform with Mesh Filter | — |
| **Interpolation Duration** | Seconds; `0` = snap | `0.1` |
| **Minimum Visible Height** | Hide below height (m) | `0.01` |

### Runtime API

- `SourceRegion`, `DisplayedState`
- `SnapToCurrentState()`

### Geometry support

- Extruded composite footprints with at least three points.
- Two-box inclusion/exclusion when `PresentationGeometry` is available.

Unsupported composite geometry hides the visual.

### Common mistakes

- Leaving member surface renderers enabled (warning + double transparency).
- Putting the region renderer on a member instead of the region.
- Using per-volume planes for camera FX while the region owns the shared plane.

See also: [FloodRegion presentation](../components/flood-region.md#presentation).

---

## FloodWaterVisual (obsolete)

`FloodWaterVisual` is an empty subclass of `FloodCubeSurfaceRenderer` kept for
pre-0.3.0 scenes. It is hidden from the Add Component menu.

**Upgrade:** replace the component with `FloodCubeSurfaceRenderer` and keep the
same child visual assignment. Step-by-step:
[Upgrade from FloodWaterVisual](../editor-workflow.md#upgrade-from-floodwatervisual).

---

## Materials

- URP: `Packages/com.rabbidwolf.com.kyle.flooding/Materials/Floodwater.mat`
- Built-in / HDRP / custom: assign your own transparent material

Details: [Materials](materials.md).
