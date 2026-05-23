# Fensalir Dependency

Aquarium is a client of Fensalir. It is not the native runtime, and it should
not become a second renderer because the real runtime is next door.

Aquarium owns:

- Epiphany agent/body semantics.
- Epiphany SDF shader code and material/anatomy choices.
- Client render-plan configuration.
- CultCache documents for Aquarium state.
- CultNet interpretation and voice surfaces.
- World-space layout rules for Epiphany roles, anchors, and interaction roots.

`E:\Projects\Fensalir` owns:

- Native windowing and process host.
- D3D12 renderer, shader compilation, and render graph execution.
- Input translation, audio output, debug chrome, and live reload.
- `Aquarium.Engine.Contracts`.
- Reusable fractal, field, reservoir, synth, and demo machinery.

The dependency direction is one-way. If Aquarium needs new renderer authority,
add that authority in Fensalir and consume it here through contracts. If a
behavior names Epiphany, role bodies, SDF anatomy, voice routing, or visible
scene semantics, keep it in Aquarium.
