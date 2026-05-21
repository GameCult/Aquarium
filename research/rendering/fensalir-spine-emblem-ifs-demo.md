# Fensalir Spine Emblem IFS Demo Reference

## Purpose

This note preserves the visual target for a possible Fensalir IFS demo scene.
The current fullscreen splash art is strong enough to become a procedural scene
rather than only a bitmap.

## Source Asset

- Splash bitmap: `src/Aquarium.Engine/Assets/Fensalir-Splash.png`
- Startup BMP copy: `src/Aquarium.Engine/Assets/Fensalir-Splash.bmp`
- Icon derivative: `src/Aquarium.Engine/Assets/Fensalir-Icon-96.png`

## Prompt Of Record

Use case: stylized-concept

Asset type: 16:9 engine splash art concept

Primary request: A bold, logo-adjacent Fensalir engine splash direction that
could sit behind a startup screen, combining Norse fen hall mythology with a
neon rendering engine identity.

Scene/backdrop: minimal dark void fading into shallow marsh water, abstract hall
silhouette made from vertical luminous runic pillars and clean renderer-like
geometry, reeds only at the extreme edges.

Subject: a central vertical spine of light formed by stacked translucent
polygons, runic strokes, and watery reflections, suggesting the engine as the
structural backbone of visible worlds.

Style/medium: sleek futuristic key visual, premium software splash art, less
literal, high contrast, graphic and memorable.

Composition/framing: centered emblem-like composition, strong silhouette, wide
negative space, symmetrical but not flat, the spine rising from reflective water
into mist.

Lighting/mood: intense cyan-white central light, restrained magenta edge glow,
tiny gold sparks, calm authority.

Color palette: black, cyan-white, magenta, muted gold, dark green-black.

Materials/textures: glass, black metal, wet stone, reflective water,
laser-etched rune light, subtle fog.

Constraints: no readable text, no logo, no watermark, no characters, no
weapons, no horned helmets, avoid looking like a crypto logo or generic esports
background.

## Implementation Invariants

- The central cyan-white vertical spine owns the scene. Everything else supports
  it.
- The geometry should read as stacked translucent diamond/polygon forms, not a
  literal tower or generic sci-fi logo.
- Magenta belongs in restrained side glow and reflected water, not as the main
  light authority.
- The base is black reflective marsh water with minimal reeds at the edges.
- The hall/rune language should be abstract and structural. No readable runic
  text.
- The scene must remain legible at splash scale and should degrade cleanly into
  the 96 px icon silhouette.

## IFS Demo Cut Line

Keep the demo procedural if the IFS/fractal machinery can own the central spine,
pillar rhythm, and reflections directly. If the implementation needs a bag of
hand-placed quads plus post-process glow to resemble the splash, call it a
staged splash scene, not an IFS demo. The demo should prove an invariant of the
IFS work, not merely cosplay it with decoration.
