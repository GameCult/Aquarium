# Epiphany Aquarium Instructions

## Purpose

This repo is the Epiphany Aquarium client. It owns Epiphany-facing runtime
semantics, agent visuals, CultCache documents, CultNet interpretation, voice
surfaces, and client-authored render plans.

The native host and renderer live in the adjacent Fensalir repo:
`E:\Projects\Fensalir`.

## Operating Doctrine

- Epiphany owns meaning; Fensalir owns the machine that displays and reloads it.
- Keep dependency direction one-way: this repo can reference Fensalir, but
  Fensalir must not learn Epiphany nouns.
- Client render code should publish explicit contracts and shaders. Do not sneak
  renderer policy, D3D12 plumbing, or host lifecycle code back into this repo.
- CultCache documents are typed client state, not loose JSON sidecars.
- CultNet surfaces are semantic input. Spatial placement, body projection, and
  renderer budget belong to the client/runtime contract, not to chat folklore.
- If an engine capability is missing, add it in Fensalir and consume it here.
  Do not build a small private engine because the real one is across the hall.

## Persistent State

- `state/map.yaml` is the canonical client map.
- `state/memory.json` is durable Epiphany Aquarium doctrine and taste.
- `state/evidence.jsonl` stores lessons that should change future behavior.
- `state/scratch.md` is disposable working context for the active slice.

Update state when ownership, contracts, or client doctrine changes.

## Verification

```powershell
dotnet build EpiphanyAquarium.sln
.\scripts\dev-reload.ps1 -Headless -RetainSlots 4
```

## Style

Keep docs about the live system. Historical scars belong in evidence only when
they change future decisions.
