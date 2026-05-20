# Fractal Reference Oracles

Aquarium needs boring reference pressure before it earns pretty fractal claims.
The renderer is not allowed to prove itself by looking interesting.

## Apophysis/FLAM3 Boundary

Apophysis 7x is the classic editor lineage for fractal flames, and the
repository is GPL-2.0. FLAM3 is the older portable renderer/genome toolchain
with the public `.flame`/`.flam3` ecosystem and documented variation list.

Use them like AquaSynth uses synth references:

- Reference fixtures live in tests or artifacts, not engine runtime.
- GPL renderer/source stays external; Aquarium does not paste implementation
  code into the runtime.
- Aquarium DSL candidates must reproduce reference behavior through numeric
  gates before GPU rendering claims are accepted.
- Image parity receipts should be produced by an external reference renderer
  when a local FLAM3/Apophysis-compatible binary is available.

## First Gate

The first committed fixture is deliberately small:

- `tests/Aquarium.Engine.Fractal.Tests/Fixtures/Apophysis/linear-sierpinski.flame`
- `tests/Aquarium.Engine.Fractal.Tests/Fixtures/Apophysis/linear-sierpinski.aquageo`

The `.flame` file uses three Apophysis/FLAM3-style linear xforms. The
`.aquageo` file expresses the same affine IFS using `affineifs` and
`affinemap`. `ApophysisReferenceParityTests` compares both through a fixed
transform sequence and through chaos-game moment bounds.

This is not full flame parity. It is the first ruler on the desk.

## Next Gates

1. DONE: parse a minimal `.flame` subset into a renderer-agnostic flame
   definition: xform weight, color, affine coefficients, and `linear`,
   `spherical`, and `bubble` variation weights.
2. DONE: add exact point parity for `linear`, `spherical`, and `bubble`,
   plus a deterministic histogram checksum for the mixed fixture.
3. Add deterministic histogram parity against a local external FLAM3/Apophysis
   compatible renderer, with generated receipts under `artifacts/parity`.
4. Lower proven flame definitions into GPU program rows. Only then should the
   live reservoir renderer use them as visual test scenes.

## Local Receipt

Run:

```powershell
.\scripts\fractal-flame-receipt.ps1
```

Default receipt:

- fixture: `linear-spherical-bubble.flame`
- samples: `8192`
- histogram: `64x64`
- bounds: `-8,-8,8,8`
- seed: `0x0BADC0DE`
- expected checksum: `0xAEB1C81B`

This receipt is Aquarium's local CPU parser/evaluator checksum. It is not yet
an external FLAM3/Apophysis renderer receipt.

## Sources

- Apophysis 7x repository: https://github.com/wanily/apophysis7x
- FLAM3 repository and variation list: https://github.com/scottdraves/flam3
- Draves, *The Fractal Flame Algorithm*: https://flam3.com/flame_draves.pdf
