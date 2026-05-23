# Epiphany Aquarium

Epiphany Aquarium is the Epiphany-owned client for the Fensalir native runtime.
This repo owns client semantics: agent bodies, camera intent, CultCache state,
CultNet interpretation, voice surfaces, and Epiphany-specific shaders.

Fensalir owns the window, renderer, D3D12 backend, audio device path, hot reload,
debug chrome, contracts, fractal machinery, and engine demos. It lives next to
this repo at `E:\Projects\Fensalir`.

## Repository Shape

- `src/Aquarium.Epiphany`: the runtime loaded by the Fensalir host.
- `src/Aquarium.Epiphany.AgentPreview`: isolated Epiphany agent preview tool.
- `docs`: Epiphany/client boundary notes.
- `state`: repo memory, map, evidence, and scratch state for the client.
- `scripts`: thin wrappers that delegate host/watch/reload work to Fensalir.

The client references Fensalir source projects directly while the package
boundary is still young. Do not copy engine contracts back into this repo.

## Build

```powershell
dotnet build EpiphanyAquarium.sln
```

## Run

```powershell
.\scripts\dev-reload.ps1
```

The wrapper calls `E:\Projects\Fensalir\scripts\dev-reload.ps1` with
`src\Aquarium.Epiphany\Aquarium.Epiphany.csproj` as the client project.

For a headless smoke:

```powershell
.\scripts\dev-reload.ps1 -Headless -RetainSlots 4
```

For the watcher:

```powershell
.\scripts\dev-watch.ps1
```

## Boundary

Epiphany may depend on Fensalir contracts and renderer services. Fensalir must
not depend on Epiphany policy, role names, CultNet surfaces, or client layout.
If a change needs new engine authority, make it in `E:\Projects\Fensalir` first
and consume the resulting contract here.
