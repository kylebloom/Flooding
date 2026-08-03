# Flood Mass Integration sample

Import **Flood Mass Integration** from **Window > Package Management > Package
Manager > Flooding > Samples**. Unity copies it to:

`Assets/Samples/Flooding/0.9.1/Flood Mass Integration`

Open `FloodMassRollPitch.unity` from that imported folder, then enter Play Mode.

## Authored hierarchy

The scene is fully serialized and editable before Play Mode:

- **Main Camera**: the scene camera and audio listener.
- **Directional Light**: lights the demonstration.
- **Flood Mass Demo Vessel**: owns the `BoxCollider`, `Rigidbody`,
  `FloodSimulationManager`, `FloodMassAggregator`,
  `RigidbodyFloodMassAdapter`, and sample-only `FloodMassDemoBuoyancy`
  components.
  - **Vessel Visual**: a cube mesh rendered with the editable `Vessel`
    material asset. The parent `BoxCollider`, not this visual child, defines
    vessel collision.
  - **Port Compartment**: a `FloodVolume` with a 1.5 m by 4 m by 1 m
    rectangular interior and an initial volume of 1 m³.
  - **Starboard Compartment**: a matching `FloodVolume` with an initial
    volume of 0 m³.

The adapter's **Flood Mass** reference is assigned to the vessel's
`FloodMassAggregator`. The aggregator discovers both child `FloodVolume`
components, then the adapter applies the combined dry-plus-flood mass and local
center of mass to the vessel `Rigidbody`.

`FloodMassDemoBuoyancy` supplies four simple world-up spring supports so the
shifted center of mass produces a visible roll response. It is sample-only
behavior, not a production buoyancy or vessel-stability implementation.
The demonstration intentionally does not render visible water; it shows the
physical response to the reported aggregate flood mass and center of mass.

## Tuning the sample

1. Stop Play Mode.
2. Select **Flood Mass Demo Vessel**.
3. On `RigidbodyFloodMassAdapter`, change **Dry Mass** in kilograms or **Dry
   Center Of Mass Local** in vessel-local meters.
4. On `FloodMassDemoBuoyancy`, tune **Support Height** in world-space meters,
   spring stiffness in N/m, damping in N·s/m, and the local support-point
   positions. These values control only the sample's visible support response.
5. Select **Port Compartment** or **Starboard Compartment**.
6. On `FloodVolume`, change **Initial Volume** in cubic meters. For the default
   1.5 m × 4 m × 1 m geometry, valid values are 0–6 m³.
7. To resize a compartment, keep **Geometry Mode** set to **Rectangular Prism**
   and edit **Width**, **Length**, and **Maximum Height**, all in meters.
8. Enter Play Mode. The port-heavy default rolls the vessel toward port.

The runtime adapter owns its configured dry mass and dry local center of mass.
Do not have another component write the same Rigidbody properties while the
adapter is enabled.

## Reimporting after package changes

The Package Manager imports a copy; it does not keep the scene synchronized
with `Packages/com.rabbidwolf.com.kyle.flooding/Samples~/Mass Integration`.
Back up any edits made under `Assets/Samples` before refreshing the sample.
Then delete only the imported
`Assets/Samples/Flooding/0.9.1/Flood Mass Integration` folder and click
**Import** for **Flood Mass Integration** again in the package's **Samples**
list. Do not replace other imported sample folders.
