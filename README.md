# Flooding

Gameplay-focused, volume-based flooding simulation for Unity 6.5. This
repository contains the **Flooding** Unity package (`com.rabbidwolf.com.kyle.flooding`)
and a development project used to build, test, and author samples for it.

The simulation models bulk water inside compartments—leaks, doorways, hull
breaches, connected rooms, and optional Rigidbody mass integration—without
computational fluid dynamics. Water **volume** (cubic meters) is authoritative;
surface height, mass, and center of mass are derived.

**Current version:** `0.11.0` (pre-1.0 prototype)

## Requirements

- Unity Editor **6.5** (`6000.5.6f1`)
- Core simulation is render-pipeline independent
- Included sample water material targets **Universal Render Pipeline (URP)**; other
  pipelines need a compatible transparent material

## Use the package

Most users install the package into their own Unity project rather than opening
this repository directly.

### Install from Git URL

In **Window > Package Management > Package Manager**, choose
**+ > Install package from git URL** and enter:

```text
https://github.com/kylebloom/Flooding.git?path=/Packages/com.rabbidwolf.com.kyle.flooding
```

Pin a tested revision for reproducible builds:

```text
https://github.com/kylebloom/Flooding.git?path=/Packages/com.rabbidwolf.com.kyle.flooding#<commit-or-tag>
```

### Install from a local clone

1. Clone this repository.
2. In your Unity project, open **Window > Package Management > Package Manager**.
3. Choose **+ > Install package from disk**.
4. Select `Packages/com.rabbidwolf.com.kyle.flooding/package.json`.

Do **not** copy the package folder into `Assets`.

### Quick start

After installation, see the package README for a 60-second smoke test and
scenario walkthroughs:

- [Package README](Packages/com.rabbidwolf.com.kyle.flooding/README.md)
- [Unity Editor workflow](Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md)
- [Package technical overview](Packages/com.rabbidwolf.com.kyle.flooding/Documentation/index.md)

Fastest path: drag
`Packages/com.rabbidwolf.com.kyle.flooding/Runtime/Prefabs/Room.prefab` into a
scene and enter Play Mode.

## Work in this repository

Open the repository root as a Unity project in Editor 6.5. The `Assets/` folder
holds development scenes and imported samples; the package source lives under
`Packages/com.rabbidwolf.com.kyle.flooding/`.

### Repository layout

```text
Flooding/
├── Assets/                          # Development scenes and imported samples
├── Packages/
│   └── com.rabbidwolf.com.kyle.flooding/
│       ├── Runtime/                 # Simulation, geometry, components, presentation
│       ├── Editor/                  # Inspectors, baking, authoring tools
│       ├── Tests/                   # Edit Mode and Play Mode test assemblies
│       ├── Documentation/           # Editor workflow and package overview
│       ├── Samples~/                # Authoritative sample sources (import via Package Manager)
│       ├── Runtime/Prefabs/         # Room.prefab, Flooding.prefab
│       └── README.md                # Primary user-facing package documentation
└── docs/                            # Spec, architecture, and implementation plans
```

### Run tests

1. Open this project in Unity 6.5.
2. Open **Window > General > Test Runner**.
3. Run **Edit Mode** tests for deterministic simulation rules.
4. Run **Play Mode** tests for GameObject lifecycle, physics, and frame behavior.

Test assemblies:

- `Kyle.Flooding.Tests.Editor`
- `Kyle.Flooding.Tests.PlayMode`

### Import samples

In **Window > Package Management > Package Manager**, select **Flooding**, open
**Samples**, and import:

| Sample | Demonstrates |
| --- | --- |
| **Hull Breach** | Ocean waterline driving bidirectional flow through a hull opening |
| **Connected Compartments** | Pressure-driven equalization between two finite compartments |
| **Baked Geometry** | Curved hull-section bake, cell viz, gravity-aligned free surface |
| **Flood Mass Integration** | Cutaway barge: visible water, COM markers, roll/pitch from flood mass |

Imported samples copy to `Assets/Samples/Flooding/<package-version>/`. The `Samples~`
folders in the package are the authoritative sources.

## Features (summary)

- Volume-authoritative flooding in cubic meters with gravity-aligned surfaces
- Rectangular prism, extruded polygon, and Editor-baked complex compartments
- Fixed-step simulation with `FloodSimulationManager` orchestration
- Direct inflow (`FloodSource`), finite connections (`FloodConnection`), and
  infinite exterior exchange (`ExternalFluidBoundary`)
- Immutable `FloodState` snapshots for gameplay and presentation
- Optional Rigidbody mass and center-of-mass integration
- Scaled-cube, polygon-mesh, and baked free-surface renderers
- Optional flow visuals, audio, and Scene-view diagnostics

See the [package README](Packages/com.rabbidwolf.com.kyle.flooding/README.md) for
the full feature list, known limitations, and scenario links.

## Documentation

| Document | Purpose |
| --- | --- |
| [Package README](Packages/com.rabbidwolf.com.kyle.flooding/README.md) | Install, quick start, samples, limitations |
| [Editor workflow](Packages/com.rabbidwolf.com.kyle.flooding/Documentation/editor-workflow.md) | Step-by-step setup, scenarios, scripting, troubleshooting |
| [Package overview](Packages/com.rabbidwolf.com.kyle.flooding/Documentation/index.md) | Runtime model, units, component map |
| [Repository docs](docs/README.md) | Spec, architecture, and implementation plans |

## Status

The package is a pre-1.0 prototype. Public APIs may evolve before `1.0.0`.
Implementation and Unity regression verification status are tracked in
[docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md).

## Author

Kyle Bloom

## License

This project is licensed under the [MIT License](LICENSE).
