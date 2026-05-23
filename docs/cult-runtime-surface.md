# Cult Runtime Surface

Epiphany Aquarium persists client state through CultCache. Fensalir only passes
the cache path into the runtime; this repo owns the document schemas and their
meaning.

## Cache Path

The local wrappers call Fensalir's dev scripts, which pass a cache path under
the Fensalir artifacts directory:

```powershell
--cache E:\Projects\Fensalir\artifacts\dev-reload\cultcache\aquarium-client.msgpack
```

Headless runs use a separate headless cache path so smoke tests do not overwrite
the visible dev window's state.

Direct host runs can also use `--cache <path>` or `AQUARIUM_CULTCACHE_PATH`.

## Live State

`AquariumLiveState` is the primary client state document:

- schema name: `epiphany.aquarium.live_state`
- schema version: `epiphany.aquarium.live_state.v0`
- record key: `aquarium-client/live-state`
- name: `aquarium-client`

It stores camera target, yaw, pitch, distance, runtime time, Face voice endpoint
rows, voice routing settings, and a monotonic save generation. The runtime saves
it periodically and during orderly shutdown.

If the backing store is truncated or unreadable, the client quarantines the
corrupt snapshot and boots fresh typed state. Broken bytes are not a schema
migration.

## Graphics Settings

`AquariumGraphicsSettingsState` persists renderer presentation knobs:

- schema name: `epiphany.aquarium.graphics_settings`
- schema version: `epiphany.aquarium.graphics_settings.v0`
- record key: `aquarium-client/graphics-settings`
- name: `aquarium-client-graphics`

It stores render debug mode, exposure, bloom intensity, and bloom veil. The
`GraphicsSettings` type lives in Fensalir contracts so the host and client share
one typed surface.

## Voice State

Face voice endpoint rows are client state. Each endpoint owns its id, display
name, thread id, voice, prompt, enabled flag, and XY routing anchor. Audio
output is transient and goes through Fensalir's generic queued PCM lane.

Transcripts and counters are local runtime/debug surface. They are not durable
Epiphany memory.
