# ExternalFluidBoundary (External Fluid Body)

`ExternalFluidBoundary` represents an infinite exterior fluid reference such as
ocean, lake, or reservoir waterline.

## Use this when

- A finite compartment exchanges water with an exterior body.
- You need inflow/outflow based on head and opening depth.

## Do not use this when

- You only need configured scripted inflow.
  Use `FloodSource` for that case.

## Beginner setup

1. Create GameObject `External Ocean` under `Flood System`.
2. Add **External Fluid Body** (`ExternalFluidBoundary`).
3. Set Transform:
   - Position on waterline point.
   - Up vector as water-surface normal (usually world up).
4. Set **Density** to match connected volumes (typically `1000`).
5. Enable **Boundary Enabled**.
6. Connect this boundary to a volume using `FloodConnection`.

## Key Inspector fields

- **Density**: must match connected compartment density.
- **Boundary Enabled**: enables/disables exchange participation.

## Verification checklist

1. Place breach opening below exterior waterline with empty interior.
2. Enter Play Mode and confirm inflow.
3. Raise interior level above waterline and confirm outflow.

## Common mistakes

- Using FloodSource instead of exterior boundary for pressure equilibrium.
- Density mismatch with connected volume.
- Incorrect transform orientation for waterline normal.

## Runtime API notes

- Exterior side is treated as infinite for supply/capacity reconciliation.
- Cannot connect exterior-to-exterior in this version.
