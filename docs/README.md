# Aquarium Docs

This repo is Aquarium: the Epiphany client and SDF body surface. Read these
docs when you are changing what the visible scene means, how bodies appear, or
what client state survives between runs.

- `epiphany-agent-sdf-visual-language.md`: the main visual contract. It defines
  Self, Face, Imagination, Eyes, Body, Hands, Soul, Life, the cursor, role
  anchors, normalized Epiphany inputs, SDF rules, motion rules, and LOD/cost
  constraints.
- `cult-runtime-surface.md`: typed CultCache documents for camera/runtime
  state, renderer settings, and Face voice routing.
- `fensalir-dependency.md`: dependency direction between Aquarium and the
  adjacent Fensalir runtime.

The top-level `README.md` describes the actual client loop: orbit camera,
Grid/height-field construction, SDF role bodies, Epiphany integration, debug
panel, and Face voice surface. Persistent repo memory lives in `../state/`.
