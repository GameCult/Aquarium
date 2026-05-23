# Aquarium

Aquarium is Epiphany's native visual body. It is the client that turns Epiphany
state into a spatial scene: Self, Face, Imagination, Eyes, Body, Hands, Soul,
Life, the cursor, the Grid, Face voice routing, client state, and the
Epiphany-specific SDF body language.

This repo is currently checked out at `E:\Projects\Aquarium-Engine`; upstream is
expected to become `GameCult/Aquarium`. The old name is logistics. The machine
is Aquarium.

Fensalir supplies the native host, D3D12 renderer, hot reload, audio output, and
shared contracts. Aquarium supplies the meaning, layout, state documents, voice
surface, render plan, and SDF shaders that make the scene Epiphany instead of a
generic renderer demo.

## What The Client Is

Aquarium is not an engine repo and not a dashboard with a viewport bolted on.
The viewport is the client. Epiphany appears as bodies in a field:

- `Self` is the central body, coordinator anchor, and scene light.
- `Face`, `Imagination`, `Eyes`, `Body`, `Hands`, `Soul`, and `Life` are
  orbiting role bodies with distinct anatomy and materials.
- The cursor is a scene body with current and previous world-space state.
- The Grid is a height-field surface shaped by Self, role wells, and cursor
  pressure.
- The camera is an orbit rig around the Grid center, persisted through
  CultCache.
- Overlay UI exists for runtime control, but the embodied scene is the app.

The repo contains the Aquarium runtime, an isolated agent preview tool, docs,
scripts, and persistent project memory. Reusable host/renderer machinery lives
next door in Fensalir.

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
  -> Grid height-field brushes
  -> Epiphany SDF body shaders
  -> Face voice routing and optional PCM audio
```

`AquariumRuntime` owns live client state. It loads typed CultCache documents,
keeps camera/runtime settings warm, saves on a short cadence, builds the
Epiphany debug panel, accepts runtime UI commands, and composes each frame.

The panel exposes time pause/scrub, explicit state flush, graphics settings,
Face voice endpoint rows, prompts, routing state, transcript counters, and
transport status. It is operational surface. The app is still the spatial body
it controls.

## SDF Body Work

Aquarium's current visual language is analytic SDF role bodies over the Grid.
The body shaders live under `src/Aquarium.Epiphany/Shaders`; the design contract
lives in `docs/epiphany-agent-sdf-visual-language.md`.

The live rules are:

- each organ gets a distinct SDF family, not a color swap;
- each shader exposes a cheap bound/hit distance and a refined surface material;
- material regions stay inside the role shader: core, shell, tool edge, lens,
  ribbon, memory seed, bubble, risk seam, and similar readable anatomy;
- state should move low-dimensional parameters first: radius, lobe count,
  twist, aperture, rib spacing, pulse phase, shell openness, wake length;
- interaction roots must stay stable even while bodies breathe, fold, shimmer,
  or speak;
- role placement is Aquarium-owned world-space layout, derived from Epiphany
  semantic state rather than supplied as world coordinates by Epiphany.

The eight bodies are not a single heroic scene march. Aquarium renders bounded
per-agent SDF proxy objects so role anatomy remains readable and cost remains
inspectable. The agent preview tool exists to render one body through the real
Fensalir renderer without dragging the whole client loop into every visual
iteration.

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

`EpiphanyRenderPlan` declares the pass structure: height-field, HDR scene, SDF
proxies, bloom/presentation, DirectWrite overlay, and debug views for the height
field and scene.

## Epiphany Integration

Aquarium owns the projection from Epiphany's semantic surfaces into visible
spatial state. The SDF visual-language document names the intended inputs:
agent memory, heartbeat, role surfaces, role results, coordinator state, Face
surface, jobs, pressure, and reorientation documents.

Those documents are normalized into bounded visual fields such as activity,
readiness, load, blocked/completed state, heartbeat phase, wake intensity,
memory resonance, pressure, review state, risk, confidence, evidence gaps,
speaking state, job count, home orbit slot, expressive offset, gravity-well
pulse, lift, and trait activations.

Epiphany supplies meaning. Aquarium owns the spatial projection and SDF
language. Fensalir renders the pixels.

## Face Voice Surface

Face voice routing is Aquarium state because it is Epiphany meaning, not
renderer machinery. The runtime stores endpoint rows, voices, prompts,
enablement, active thread, audible radius, and auto-route settings through
CultCache-backed state.

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
- `docs`: SDF visual grammar, CultCache state surface, and Fensalir dependency
  notes.
- `state`: repo-local memory, map, evidence, and scratch state.

## Dependency

This repo currently references sibling Fensalir source projects at
`E:\Projects\Fensalir`. That is intentional until Fensalir has a packaged API.

Do not copy renderer contracts, host scripts, or D3D12 helpers into Aquarium.
If Aquarium needs new renderer authority, add it in Fensalir and consume it
through contracts. If a behavior names Epiphany roles, client voice policy,
agent meaning, SDF anatomy, or scene semantics, it belongs here.

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

Aquarium persists typed client state through CultCache:

- camera target, yaw, pitch, distance, and runtime time;
- renderer presentation settings owned by shared contracts;
- Face voice endpoint rows, prompts, routing state, and transport settings.

Speech transcript counters are local runtime surface, not durable Epiphany
memory.

## Docs

- `docs/epiphany-agent-sdf-visual-language.md`: SDF visual grammar for
  Epiphany bodies.
- `docs/cult-runtime-surface.md`: client CultCache document surface.
- `docs/fensalir-dependency.md`: what Aquarium consumes from Fensalir.
- `state/README.md`: persistent state machinery.
