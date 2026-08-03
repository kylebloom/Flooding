# Baked Geometry sample

Import **Baked Geometry** from **Window > Package Management > Package Manager
> Flooding > Samples**. Unity copies it to:

`Assets/Samples/Flooding/0.9.1/Baked Geometry`

Open `BakedGeometry.unity` from that imported folder, then enter Play Mode.

## Expected behavior

The scene's persistent hierarchy is visible and editable before Play Mode:

- **Baked Geometry Sample** contains `FloodSimulationManager` and
  `BakedGeometrySampleBootstrap`;
- **Sloped Baked Compartment** contains `FloodVolume` in **Baked Data** mode,
  assigned to `SlopedCompartmentFloodVolumeData.asset`, plus
  `FloodBakedSurfaceRenderer`;
- **Baked Water Surface** contains the built-in Mesh Filter and Mesh Renderer.
  Its Mesh Filter intentionally has no saved mesh because
  `FloodBakedSurfaceRenderer` generates the water surface mesh at runtime;
- **Retained Shape Presentation** contains four editable floor-step GameObjects
  and three editable wall GameObjects. Each has built-in Transform, Mesh Filter,
  and Mesh Renderer components and uses the local
  `BakedGeometryStructure.mat` material; and
- the root-level **Main Camera** and **Directional Light** provide the sample
  view and lighting.

By default, the sample cycles between 28% and 72% of its 21 m³ baked capacity
while gently rolling the compartment. The generated water surface stays aligned
to world gravity and follows the stepped/sloped retained shape. The dark floor
steps and walls are sample-only presentation objects; they mirror the retained
cell layout and are not analyzed by flooding runtime code.

## Tweak the imported sample

Stop Play Mode before making persistent edits.

- Select **Baked Geometry Sample**. On `BakedGeometrySampleBootstrap`, clear
  **Animate Fill** or **Animate Roll** to disable either animation independently.
  **Minimum Fill Fraction**, **Maximum Fill Fraction**, and **Fill Rate** control
  the fill cycle; fill rate is in cubic meters per second. **Roll Degrees** is
  the maximum local Z rotation, and **Roll Period** is one complete cycle in
  seconds.
- Expand **Sloped Baked Compartment > Retained Shape Presentation** to move or
  resize an individual floor step or wall with its Transform.
- Select `BakedGeometryStructure.mat` in the imported sample folder to change
  the structure's color, smoothness, or other URP Lit material properties.
- Select **Main Camera** or **Directional Light** to edit the saved composition
  and lighting directly.

These presentation edits do not change the baked occupancy in
`SlopedCompartmentFloodVolumeData.asset`. To make the simulation match altered
structure geometry, author and bake a replacement data asset as described below.

`SlopedCompartmentFloodVolumeData.asset` is the shipped, pre-baked gameplay
asset. It contains 168 retained 0.5 m cells in an 8 × 5 × 6 grid. The retained
cell columns rise from left to right to represent an uneven, stepped slope.
Play Mode reads only this immutable asset. No source Mesh Filter is required,
and no runtime source-mesh vertex or triangle analysis occurs.

## Author or rebake a replacement

The sample intentionally ships without its authoring source mesh. To author a
replacement in the imported, writable copy:

1. Stop Play Mode and select **Sloped Baked Compartment**.
2. Add the package `FloodVolumeAuthoring` component to that GameObject.
3. Create a separate child GameObject such as **Authoring Source Mesh**.
4. Add built-in Mesh Filter and Mesh Renderer components to that child, then
   assign a readable, closed, manifold, non-degenerate mesh to its Mesh Filter.
5. On `FloodVolumeAuthoring`, assign **Target Volume** to the
   **Sloped Baked Compartment** `FloodVolume` and **Source Mesh Filter** to the
   child Mesh Filter.
6. Choose **Cell Resolution** in meters and a suitable **Maximum Grid Cells**
   safety limit.
7. To preserve the shipped example, duplicate
   `SlopedCompartmentFloodVolumeData.asset` and assign the duplicate as
   **Baked Data**. Then click **Bake Closed Mesh To Flood Volume Data**.
8. Confirm the `FloodVolume` remains in **Baked Data** mode and references the
   newly baked asset. Keep **Visualize Bake** enabled to inspect retained
   samples while the authoring GameObject is selected.

The source child and `FloodVolumeAuthoring` are Editor authoring inputs only.
They are not needed in a player build after the `FloodVolumeData` asset has
been baked and assigned.

## Reimporting the sample

Package Manager sample import copies package contents into `Assets/Samples`.
Reimporting can overwrite files with the same names, including changes to the
scene, bootstrap, README, and local structure material. Before reimporting,
move or duplicate any edited assets outside the imported
`Assets/Samples/Flooding/0.9.1/Baked Geometry` folder, or back them up in version
control. Reimporting does not write changes back into the package's `Samples~`
source folder.
