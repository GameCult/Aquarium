# Epiphany Aquarium

Epiphany Aquarium is the visual body for Epiphany. It is a native client that
runs on the Fensalir runtime and turns agent state into an embodied scene:
camera, Grid, Self, role bodies, cursor, voice controls, durable client state,
and Epiphany-specific shaders.

Fensalir owns the host, renderer, hot reload, audio output, and reusable field
machinery. This repo owns what the Aquarium is and what its inhabitants mean.

## What The Client Is

The Aquarium is not a dashboard with a decorative viewport. The viewport is the
client. It is a live spatial interface where Epiphany appears as bodies in a
field:

- `Self` is the central body and light source.
- Seven role agents orbit and inhabit the scene: `Face`, `Imagination`, `Eyes`,
  `Body`, `Hands`, `Soul`, and `Life`.
- The cursor is a scene body with current and previous world-space state.
- The Grid is a height-field surface shaped by Self, role wells, and cursor
  pressure.
- The camera is an orbit rig around the Grid center, persisted through
  CultCache.
- Overlay UI exists for runtime control, but the scene is the primary machine.

The project is currently a client runtime, an isolated agent preview tool, docs,
scripts, and persistent repo memory. The rest of the old reusable engine has
been moved into the adjacent Fensalir repo.

## Runtime Loop

`src/Aquarium.Epiphany` implements `IAquariumRuntime`. Fensalir loads it and
calls into it each frame:

```text
Fensalir host
  -> Aquarium.Epiphany runtime
  -> CultCache state load/save
  -> orbit camera + input projection
  -> Epiphany scene builder
  -> Epiphany render plan
  -> SDF body shaders + height-field passes
  -> Face voice routing and optional PCM audio
```

`AquariumRuntime` owns the live client state. It loads typed state documents,
keeps camera/runtime settings warm, saves on a short cadence, builds the
Epiphany debug panel, accepts runtime UI commands, and composes each frame.

The panel currently exposes time pause/scrub, explicit state flush, graphics
settings, Face voice endpoint rows, prompts, routing state, transcript counters,
and transport status. It is operational surface, not the identity of the app.
The app is the embodied scene it controls.

## Visible Machine

`EpiphanySceneBuilder` turns time, camera input, persistent state, and voice
surface state into an `AquariumSceneState`.

The current scene contains:

- a 128x128 R16 height-field target for the Grid;
- a Self gravity bowl and wave contribution;
- orbiting brush wells for each role agent;
- SDF proxy objects for Self, the seven roles, and the cursor;
- Self-owned scene light;
- cursor projection from screen space onto the Grid plane;
- normalized state scalars passed into shaders for each body.

`EpiphanyRenderPlan` declares the actual pass structure: height-field,
HDR scene, SDF proxies, bloom/presentation, DirectWrite overlay, and debug
views for the height field and scene. Body appearance lives in
`src/Aquarium.Epiphany/Shaders`, including dedicated shaders for Self, Face,
Imagination, Eyes, Body, Hands, Soul, Life, and the cursor.

The visual grammar is documented in
`docs/epiphany-agent-sdf-visual-language.md`. Change that before teaching the
bodies a new language by accident. Yes, this is how the furniture starts
talking back.

## Face Voice Surface

Face voice routing is client-owned because it is Epiphany meaning, not engine
meaning. The runtime stores endpoint rows, voices, prompts, enablement, active
thread, audible radius, and auto-route settings through CultCache-backed state.

When enabled, the Face voice surface can start active or enabled endpoints,
stop active speech, clear local transcript counters, and route speech by the
scene/proximity model. Fensalir supplies the audio output lane; Aquarium decides
which Face should speak and why.

## Projects

- `src/Aquarium.Epiphany`: runtime, render plan, scene builder, persistent
  state, voice routing, camera rig, UI commands, and shaders.
- `src/Aquarium.Epiphany.AgentPreview`: isolated preview tool for rendering one
  Epiphany agent body through the real Fensalir renderer.
- `scripts`: wrappers around Fensalir dev scripts, plus the agent preview
  renderer.
- `docs`: client visual grammar and engine/client boundary notes.
- `state`: repo-local memory, map, evidence, and scratch state.

## Dependency

This repo currently references sibling Fensalir source projects at
`E:\Projects\Fensalir`. That is intentional until Fensalir has a packaged API.

Do not copy engine contracts or renderer helpers into this repo. If Epiphany
needs new renderer authority, add it in Fensalir and consume it through the
contract. If a behavior names Epiphany roles, client voice policy, agent
meaning, or scene semantics, it belongs here.

## Build

Requirements:

- Windows
- .NET SDK matching `global.json`
- sibling repos at `E:\Projects\Fensalir` and `E:\Projects\CultLib`

```powershell
dotnet build EpiphanyAquarium.sln
```

## Run

Launch through Fensalir:

```powershell
.\scripts\dev-reload.ps1
```

Run headless:

```powershell
.\scripts\dev-reload.ps1 -Headless -RetainSlots 4
```

Watch and reload on source changes:

```powershell
.\scripts\dev-watch.ps1
```

Render isolated agent preview frames:

```powershell
.\scripts\render-agent-preview.ps1 -Agent Soul
```

## State

Epiphany Aquarium persists typed client state through CultCache:

- camera target, yaw, pitch, distance, and runtime time;
- renderer presentation settings owned by shared contracts;
- Face voice endpoint rows, prompts, routing state, and transport settings.

Speech transcript counters are local runtime surface, not durable Epiphany
memory.

## Docs

- `docs/epiphany-agent-sdf-visual-language.md`: renderer-facing visual grammar
  for Epiphany bodies.
- `docs/engine-client-boundary.md`: what this repo owns versus Fensalir.
- `docs/cult-runtime-surface.md`: client CultCache document surface.
- `state/README.md`: persistent state machinery.
