# Epiphany Aquarium Docs

This repo is the Epiphany client, not the Fensalir engine. Read these docs when
you are changing what the Aquarium means, how its bodies appear, or what client
state survives between runs.

- `epiphany-agent-sdf-visual-language.md`: visual grammar for Self, Face,
  Imagination, Eyes, Body, Hands, Soul, Life, and the cursor. Use this before
  changing Epiphany body shaders.
- `engine-client-boundary.md`: dependency direction between Epiphany Aquarium
  and Fensalir.
- `cult-runtime-surface.md`: typed CultCache documents for camera/runtime
  state, renderer settings, and Face voice routing.

The top-level `README.md` describes the actual client loop: orbit camera,
Grid/height-field construction, SDF role bodies, debug panel, and Face voice
surface. Persistent repo memory lives in `../state/`.
