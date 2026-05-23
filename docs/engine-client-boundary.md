# Fensalir Dependency Boundary

Epiphany Aquarium is a client of Fensalir.

This repo owns:

- Epiphany agent/body semantics.
- Epiphany shader code.
- Client render-plan configuration.
- CultCache documents for Epiphany Aquarium state.
- CultNet interpretation and voice surfaces.

`E:\Projects\Fensalir` owns:

- Native windowing and process host.
- D3D12 renderer, shader compilation, and render graph execution.
- Input translation, audio output, debug chrome, and live reload.
- `Aquarium.Engine.Contracts`.
- Reusable fractal, field, reservoir, and demo machinery.

The dependency direction is one-way. If Epiphany needs new engine authority,
add that authority in Fensalir and consume it here through contracts. Do not
copy contracts, host scripts, or renderer helpers into this repo.
