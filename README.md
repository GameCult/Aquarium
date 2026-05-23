# Epiphany Aquarium

Epiphany Aquarium is the Epiphany-facing visual client that runs on the
Fensalir native runtime. It owns the living scene semantics: agent bodies,
camera behavior, CultCache state, CultNet interpretation, Face voice routing,
and Epiphany-specific shaders.

Fensalir owns the host and renderer. This repo tells that runtime what Epiphany
looks like and how it behaves.

## What Runs

The runtime in `src/Aquarium.Epiphany` implements `IAquariumRuntime`. Fensalir
loads it, calls its update/frame methods, and renders the declared scene:

```text
Fensalir host
  -> Aquarium.Epiphany runtime
  -> Epiphany render plan + scene state
  -> agent/body SDF proxy shaders
  -> CultCache-backed client state
  -> optional Face realtime voice audio
```

The visible scene currently has:

- a Grid/height-field surface driven by Epiphany brush state;
- Self plus seven role-agent SDF bodies: Face, Imagination, Eyes, Body, Hands,
  Soul, and Life;
- a cursor body;
- Epiphany-authored body shaders under `src/Aquarium.Epiphany/Shaders`;
- a debug panel for runtime controls, state flushing, Face voice endpoints, and
  local realtime transcript counters.

## Projects

- `src/Aquarium.Epiphany`: runtime, render plan, scene builder, persistent
  state, voice routing, camera rig, and shaders.
- `src/Aquarium.Epiphany.AgentPreview`: isolated preview tool for rendering one
  Epiphany agent body through the real Fensalir renderer.
- `scripts`: thin wrappers around Fensalir dev scripts, plus the agent preview
  renderer.
- `docs`: client visual grammar and engine/client boundary notes.
- `state`: repo-local memory, map, evidence, and scratch state.

## Dependency

This repo currently references sibling Fensalir source projects at
`E:\Projects\Fensalir`. That is intentional until Fensalir has a packaged API.

Do not copy engine contracts or renderer helpers into this repo. If Epiphany
needs new renderer authority, add it in Fensalir and consume it through the
contract.

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
  for Epiphany organs.
- `docs/engine-client-boundary.md`: what this repo owns versus Fensalir.
- `docs/cult-runtime-surface.md`: client CultCache document surface.
- `state/README.md`: persistent state machinery.
