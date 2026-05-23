# Scratch

## Current Slice

Repo split is active.

- `E:\Projects\Fensalir` owns the native runtime, renderer, contracts, fractal
  machinery, host scripts, and engine demos.
- `E:\Projects\Aquarium-Engine` is the transitional checkout for Aquarium,
  Epiphany's visual client and SDF body surface.
- Client project references point to the adjacent Fensalir source tree for now.

## Verification

- `dotnet build EpiphanyAquarium.sln`
- `.\scripts\dev-reload.ps1 -Headless -RetainSlots 4`
