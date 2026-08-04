# Baked Geometry sample

Import **Baked Geometry** from **Window > Package Management > Package Manager
> Flooding > Samples**. Unity copies it to:

`Assets/Samples/Flooding/0.9.1/Baked Geometry`

Open `BakedGeometry.unity` from that imported folder, then enter Play Mode.

## What this sample teaches

Baked Data can flood a **closed elliptical bowl / hull-section interior** that
is not a rectangular prism and is not a horizontal polygon extruded along local
Y. Horizontal free-surface footprints are ellipses that follow the curved wall.

```text
Rectangular Prism   ❌
Extruded Polygon    ❌
Baked Geometry      ✅
```

The Editor bakes occupancy cells and a presentation-boundary copy of the closed
source mesh once. Runtime queries only that immutable `FloodVolumeData`
asset—no live source-mesh analysis in Play Mode or player builds.

**Voxels answer quantity** (capacity, fill, solved plane height).
**The baked boundary answers footprint shape** (water edges follow the hull
curve). The visual footprint can therefore look more accurate than the voxel
volume approximation.

## Expected behavior

The scene's persistent hierarchy is visible and editable before Play Mode:

- **Baked Geometry Sample** contains `FloodSimulationManager` and
  `BakedGeometrySampleBootstrap`;
- **Hull Section Compartment** contains `FloodVolume` in **Baked Data** mode
  (assigned to `HullSectionFloodVolumeData.asset`), `FloodBakedSurfaceRenderer`,
  and `FloodVolumeAuthoring` for inspectable rebakes;
- **Baked Water Surface** contains the built-in Mesh Filter and Mesh Renderer.
  Its Mesh Filter intentionally has no saved mesh because
  `FloodBakedSurfaceRenderer` generates the free-surface mesh at runtime;
- **Authoring Source Mesh** is the shipped closed hull-section mesh
  (`HullSectionSourceMesh.asset`) used for the bake and as translucent
  presentation;
- **Baked Cells Presentation** is an Editor-built mesh of retained occupancy
  cells (hidden by default; press **B** in Play Mode); and
- the root-level **Main Camera** and **Directional Light** provide the sample
  view and lighting.

By default, the sample cycles between 28% and 72% of baked capacity while
gently rolling the compartment. The generated water surface stays aligned to
world gravity inside the curved hull. A Game-view HUD reports capacity, current
volume, fill fraction, bake resolution, and retained cell count.

### Play Mode controls

| Key | Action |
| --- | --- |
| Space | Pause / resume fill and roll |
| B | Show / hide baked retained cells |
| R | Toggle roll animation (resets to level when disabled) |

Clear **Animate Fill** or **Animate Roll** on `BakedGeometrySampleBootstrap` to
disable either animation from the Inspector.

## Tweak the imported sample

Stop Play Mode before making persistent edits.

- Select **Baked Geometry Sample**. On `BakedGeometrySampleBootstrap`, adjust
  **Minimum Fill Fraction**, **Maximum Fill Fraction**, and **Fill Rate**
  (cubic meters per second). **Roll Degrees** is the maximum local Z rotation,
  and **Roll Period** is one complete cycle in seconds.
- Select **Hull Structure.mat**, **Compartment Water.mat**, or
  **Baked Cells.mat** in the imported sample folder to change URP Lit colors.
- Select **Main Camera** or **Directional Light** to edit composition and
  lighting.
- Select **Hull Section Compartment** to inspect `FloodVolumeAuthoring`
  (**Visualize Bake** draws retained samples in the Scene view while selected).

Presentation and material edits do not change baked occupancy. Rebake when the
source mesh or resolution should change the simulation.

`HullSectionFloodVolumeData.asset` is the shipped, pre-baked gameplay asset.
Play Mode reads only this immutable asset. Features smaller than the bake
resolution can disappear from the retained representation; arbitrary-plane
query cost grows with occupied cell count.

## Author or rebake

The sample ships its authoring source mesh so you can see the bake pipeline:

```text
Authoring Source Mesh
      ↓
Editor bake (FloodVolumeAuthoring)
      ↓
HullSectionFloodVolumeData
      ↓
runtime FloodVolume + FloodBakedSurfaceRenderer
```

To rebake in the imported, writable copy:

1. Stop Play Mode and select **Hull Section Compartment**.
2. Confirm `FloodVolumeAuthoring` has **Target Volume** set to this GameObject's
   `FloodVolume` and **Source Mesh Filter** set to
   **Authoring Source Mesh**.
3. Choose **Cell Resolution** in meters and a suitable **Maximum Grid Cells**
   safety limit.
4. To preserve the shipped example, duplicate
   `HullSectionFloodVolumeData.asset` and assign the duplicate as
   **Baked Data**. Then click **Bake Closed Mesh To Flood Volume Data**.
5. Confirm the `FloodVolume` remains in **Baked Data** mode and references the
   newly baked asset. Keep **Visualize Bake** enabled to inspect retained
   samples while the authoring GameObject is selected.
6. Rebuild the Play Mode cell presentation if needed by running
   **Flooding > Internal > Build Baked Geometry Sample** (package maintainers)
   or by manually replacing **Baked Cells Presentation** after inspecting the
   new bake in the Scene view.

`FloodVolumeAuthoring` and the source mesh are Editor authoring inputs. Runtime
flooding code never analyzes the source mesh after the `FloodVolumeData` asset
has been baked and assigned.

## Reimporting the sample

Package Manager sample import copies package contents into `Assets/Samples`.
Reimporting can overwrite files with the same names, including changes to the
scene, bootstrap, README, meshes, materials, and bake asset. Before
reimporting, move or duplicate any edited assets outside the imported
`Assets/Samples/Flooding/0.9.1/Baked Geometry` folder, or back them up in
version control. Reimporting does not write changes back into the package's
`Samples~` source folder.
