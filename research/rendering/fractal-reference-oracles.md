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
3. DONE: add an optional external FLAM3-compatible render receipt path. When
   `flam3-render` is available, Aquarium renders the fixture to binary PPM and
   records image-shape, non-black pixel count, RGB checksum, and luminance
   checksum under `artifacts/fractal-flame-reference`.
4. DONE: lower proven flame definitions into GPU program rows for the receipt
   shader's `flame 2D affine/variation program` mode.
5. Use those rows as a visual test scene only through GPU-resident reservoirs,
   not by restoring a CPU direct renderer.

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

## External Renderer Receipt

FLAM3 stays outside the repo and outside runtime binaries. If a local
`flam3-render` executable is installed, run:

```powershell
.\scripts\fractal-flame-reference-render.ps1 -Flam3Render C:\path\to\flam3-render.exe
```

The script follows FLAM3's documented environment-variable interface: `in`
selects the flame file, `out` selects the output image, `format=ppm` requests a
plain reference image, `seed` fixes the random sequence, and `qs` controls
quality scale. Aquarium then hashes the PPM through
`tools/Aquarium.Fractal.Receipt --reference-ppm`.

This is the first external renderer receipt. It proves the fixture can be
rendered by the reference family and gives Aquarium a stable image artifact to
compare as the local flame subset grows.

## GPU Flame Receipt

The flame fixture can now drive the D3D12 reservoir receipt directly:

```powershell
.\scripts\fractal-splat-receipt.ps1 `
  -Splats 2000000 `
  -SplatUpdates 50000 `
  -Warmup 10 `
  -Frames 60 `
  -Depth 8 `
  -ReservoirUpdates 15000 `
  -ProgramFlame tests\Aquarium.Engine.Fractal.Tests\Fixtures\Apophysis\linear-spherical-bubble.flame
```

Local GTX 1070 receipt from 2026-05-20:

- resident splats: `2,000,000`
- resident reservoir rows: `2,000,000` each for SDF, PBR, and radiosity
- program transforms: `3`
- splat updates/frame: `50,000`
- reservoir updates/pass/frame: `15,000`
- GPU time: `1.030 ms/frame`
- equivalent FPS: `971.3`
- readback checksum: `0x2647D8B08575B848`

## Visual Parity Receipt

The receipt tool can compare GPU flame splat positions against the local CPU
flame oracle:

```powershell
.\scripts\fractal-splat-receipt.ps1 `
  -Splats 2000000 `
  -SplatUpdates 50000 `
  -Warmup 0 `
  -Frames 20 `
  -Depth 8 `
  -ReservoirUpdates 15000 `
  -ProgramFlame tests\Aquarium.Engine.Fractal.Tests\Fixtures\Apophysis\linear-spherical-bubble.flame `
  -ReadbackSplats 250000 `
  -VisualParity `
  -VisualParityReferenceSamples 1000000 `
  -HistogramSize 128x128 `
  -HistogramBounds -8,-8,8,8
```

First local scores, GPU 250k readback samples against 1M CPU reference samples:

| Frames | GPU ms/frame | Distribution score | L1 distance | RMSE | Cosine |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 15.645 | 84.05% | 0.319072 | 0.000090 | 0.995365 |
| 5 | 4.017 | 84.10% | 0.318042 | 0.000089 | 0.995402 |
| 20 | 1.738 | 84.20% | 0.316019 | 0.000088 | 0.995591 |
| 60 + 10 warmup | 0.952 | 84.24% | 0.315136 | 0.000088 | 0.995505 |

This proves the current GPU flame program has high distribution similarity, but
it does **not** prove learned reservoir convergence. The score plateaus because
the current flame path initializes resident splats from the program and then
performs stochastic replacement; it does not yet use visual error feedback,
MIS/ReSTIR weights, or an external rendered-image residual to steer updates.

## Sources

- Apophysis 7x repository: https://github.com/wanily/apophysis7x
- FLAM3 repository and variation list: https://github.com/scottdraves/flam3
- Draves, *The Fractal Flame Algorithm*: https://flam3.com/flame_draves.pdf
