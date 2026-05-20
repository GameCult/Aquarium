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

## JWildfire / FARender Visual Reference

Aquarium now has a reproducible higher-quality visual reference lane that does
not depend on tools already installed on the workstation:

```powershell
.\scripts\install-flame-reference-tools.ps1
.\scripts\fractal-flame-jwildfire-reference.ps1 `
  -Renderer FARender `
  -Width 1024 `
  -Height 1024 `
  -Quality 2000 `
  -NoDensityEstimation
```

`install-flame-reference-tools.ps1` downloads JWildfire 9.00 and a local
Temurin JRE into ignored `artifacts/reference-tools`. These are reference
artifacts, not Aquarium runtime dependencies.

`fractal-flame-jwildfire-reference.ps1` prefers JWildfire's bundled FARender
GPU path when available because it accepts an explicit output file and fails
the script when no image is produced. The Java batch renderer remains available
through `-Renderer JWildfireJava`, but local smoke showed that it can complete
without writing an image into the batch directory because JWildfire's output
folder is preference-driven. Silent reference success is not a reference.

Local GTX 1070 smoke receipt from 2026-05-20:

- renderer: JWildfire 9.00 bundled FARenderJWF
- fixture: `artifacts/reference-tools/j-wildfire-9.00/lib/FARenderJWF/selftest.flame`
- command: `1024x1024`, quality `2000`, CUDA, no density-estimation phase
- time: `7.69 sec`
- artifact:
  `artifacts/fractal-flame-jwildfire-reference/selftest-farender-20260520-233638.png`

The repository's tiny `linear-spherical-bubble.flame` fixture is still useful
for Aquarium subset parity, but FARender crashed on it because the fixture is
Apophysis-style minimal XML rather than full JWildfire `variationGroup` flame
XML. Do not "fix" that by making the fixture bigger until the parser and DSL
contract deliberately support the richer reference dialect.

## JWildfire Selftest Faceplant

Running JWildfire's bundled `selftest.flame` through Aquarium is the correct
kind of embarrassing:

```powershell
.\scripts\fractal-splat-receipt.ps1 `
  -ProgramFlame artifacts\reference-tools\j-wildfire-9.00\lib\FARenderJWF\selftest.flame `
  -Splats 2000000 `
  -SplatUpdates 50000 `
  -Warmup 0 `
  -Frames 1 `
  -Depth 64 `
  -ReservoirUpdates 15000 `
  -ReadbackSplats 250000 `
  -VisualParity `
  -VisualParityReferenceSamples 1000000 `
  -HistogramSize 256x256 `
  -HistogramBounds '-128,-128,128,128'
```

First receipt after adding the JWildfire `variationGroup`/coefficient-layout
slice and the `disc`, `julian`, and `gaussian_blur` variation subset, before
lowering `post`:

- GPU time: `66.446 ms/frame`, `15.0 FPS` equivalent
- visual score: `5.01%`
- occupancy overlap: `3.04%`
- L1 distance: `1.899840`
- artifact:
  `artifacts/fractal-jwildfire-faceplant/jwildfire-vs-aquarium-expanded.png`

The first unexpanded run scored `0.00%` and rendered a single dot. The expanded
run produces a recognizable central flame knot, but it still does not match the
reference. This is not just "more variations needed." It exposes three distinct
gaps:

1. **DSL expressiveness.** A serious flame dialect needs named variation
   nodes, parameters, post transforms, palette/color semantics, xaos/chaos
   weights, and optional weighting fields. The current Aquarium flame subset is
   a compact test dialect, not a full JWildfire import surface.
2. **Translation quality.** JWildfire's coefficient layout differs from the
   older Apophysis-style fixtures. `variationGroup` can hide the real variation
   list from the xform attributes. Rich dialect import needs a translation
   report that says exactly which fields were accepted, approximated, ignored,
   or rejected.
3. **Hot-path shape.** Brute replaying 64 flame iterations per splat in one
   compute invocation is too slow. Rich flame rendering should store continuing
   iteration state in GPU reservoirs and advance it stochastically across
   frames, then density-estimate/shade from those resident samples. The reservoir
   is the renderer, not an expensive reset button.

The next coherent slice is therefore not "add 200 variations." It is an
import/lowering contract: parse a richer flame AST, produce an explicit
translation report, lower supported variation nodes into compact GPU op rows,
and carry persistent per-sample iteration state so zoom/movement can reuse
sample history instead of starting every splat from zero.

That import report now exists. GPU and histogram flame receipts emit a sibling
`fractal-flame-import-report-*.json` next to the numeric receipt. For the
JWildfire selftest flame, the first report after `post` lowering says:

- accepted fields: `15`
- approximated fields: `3`
- ignored fields: `45`
- rejected fields: `11`
- rejected geometry/sampling losses: `wfield_*` weighting-field attributes on
  the disc xform

This report is the next expansion gate. A flame may parse successfully and
still be a bad import; unsupported fields must stay visible until the DSL,
compiler, or renderer can actually own them.

Lowering `post` required expanding only the small authored-program transform
row, not the resident million-splat or reservoir packets. The closer
CPU/GPU-slice receipt now gives:

- bounds `-8,-8,8,8`: `84.19%` GPU-vs-Aquarium-CPU slice score at
  `129.073 ms/frame`
- bounds `-128,-128,128,128`: `88.01%` GPU-vs-Aquarium-CPU slice score at
  `129.404 ms/frame`
- artifact:
  `artifacts/fractal-jwildfire-faceplant/jwildfire-vs-aquarium-post-close.png`

This is not external JWildfire visual parity. It says Aquarium's GPU path is
now much closer to Aquarium's CPU interpretation of the supported slice. The
image is still missing JWildfire's palette, density estimation, weighting-field
behavior, render filters, and likely more dialect detail. The speed is also
unacceptable for realtime because depth-64 replay is the wrong execution model.

The first persistent-iteration invariant is now tested on CPU:
`FractalFlameIterationState` plus `FractalFlameIterationStepper` prove that
advancing one sample by 64 iterations in one call matches advancing the same
state in eight 8-iteration chunks. This is not a CPU renderer. It is the
mockable contract the GPU state buffer must preserve when the shader stops
replaying every splat from zero.

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

## View-Conditioned Parity

Global parity is a weak metric. Realtime usefulness depends on the current
camera window: panning or zooming should keep the visible image smooth while the
cache spends new work on underrepresented regions.

The receipt tool therefore accepts named viewport bounds:

```powershell
.\scripts\fractal-splat-receipt.ps1 `
  -Splats 2000000 `
  -SplatUpdates 50000 `
  -Warmup 10 `
  -Frames 60 `
  -Depth 16 `
  -ReservoirUpdates 15000 `
  -ProgramFlame tests\Aquarium.Engine.Fractal.Tests\Fixtures\Apophysis\linear-spherical-bubble.flame `
  -ReadbackSplats 250000 `
  -VisualParity `
  -VisualParityReferenceSamples 1000000 `
  -HistogramSize 128x128 `
  -VisualParityView global:-8,-8,8,8 `
  -VisualParityView mid:-1,-1,1,1 `
  -VisualParityView close:-0.5,-0.5,0.5,0.5 `
  -VisualParityView micro:-0.25,-0.25,0.25,0.25
```

First view-conditioned receipt:

| View | Distribution score | GPU hits | Reference hits | Starved bins | Underrepresented mass |
| --- | ---: | ---: | ---: | ---: | ---: |
| global | 97.09% | 249,972 | 999,836 | 1,034 | 2.91% |
| mid | 91.93% | 166,654 | 667,105 | 1,792 | 8.07% |
| close | 88.97% | 125,448 | 501,033 | 2,266 | 11.03% |
| micro | 82.05% | 71,348 | 284,011 | 2,656 | 17.95% |

This is the first metric that catches the real problem. The reservoir is fast,
but it is not yet view-adaptive. It needs camera-conditioned target weights,
underrepresented-bin feedback, and temporal reuse validation before it deserves
claims about maintaining flame quality during movement and zoom.

## Contact Sheets

The quick 1M-sample oracle image is too noisy for visual judgment. Use the image
export path with a much larger CPU oracle when making contact sheets:

```powershell
.\scripts\fractal-splat-receipt.ps1 `
  -ProgramFlame tests\Aquarium.Engine.Fractal.Tests\Fixtures\Apophysis\linear-spherical-bubble.flame `
  -VisualParity `
  -VisualParityReferenceSamples 20000000 `
  -VisualParityImageDirectory artifacts\fractal-contact-sheet-hq\source `
  -VisualParityImagePrefix zoom-frame-010 `
  -ReadbackSplats 250000 `
  -Depth 16 `
  -Frames 10 `
  -VisualParityView zoom:-0.25,-0.25,0.25,0.25
```

The current high-reference contact sheet lives at:

- `artifacts/fractal-contact-sheet-hq/flame-parity-contact-sheet-hq.png`

It uses 20M CPU oracle samples and 250k Aquarium readback samples per panel.
This still is not a full FLAM3 density-estimated render, but it is no longer a
1M-sample noise patch masquerading as ground truth.

## Sources

- Apophysis 7x repository: https://github.com/wanily/apophysis7x
- FLAM3 repository and variation list: https://github.com/scottdraves/flam3
- Draves, *The Fractal Flame Algorithm*: https://flam3.com/flame_draves.pdf

## Reference Ladder

Stop calling Aquarium's CPU histogram the visual ground truth. It is a math
oracle for a tiny variation subset. It is useful because it is deterministic,
small, and debuggable. It is not the bar for beautiful flame rendering.

Use this ladder:

1. **Gold visual reference: Chaotica.** Chaotica is the quality target for
   flame art. Its own manual frames progressive rendering as the alternative
   to Apophysis/FLAM3 fixed-quality rerendering, and says high quality often
   means stopping at a sampling level after the image is no longer noisy. Use
   Chaotica renders as imported reference artifacts when available.
2. **Scriptable visual reference: JWildfire/FARender.** JWildfire is LGPL,
   has a documented headless batch renderer path:
   `java org.jwildfire.create.tina.batch.HeadlessBatchRendererController <dir> <w> <h> <quality>`.
   Its current public bundle also ships FARenderJWF, a Windows/NVIDIA GPU
   renderer that the JWildfire changelog says may still be used while it works.
   Use `scripts/install-flame-reference-tools.ps1` to hydrate the local tools
   and `scripts/fractal-flame-jwildfire-reference.ps1 -Renderer FARender` for
   explicit PNG output.
3. **Baseline renderer reference: FLAM3/Apophysis family.** Use FLAM3 for
   portable `.flame`/`.flam3` compatibility, deterministic external receipts,
   and old-school density-estimation sanity.
4. **Aquarium CPU oracle.** Use only for unit tests, point parity, histogram
   smoke checks, and shader debugging. Never present it as market-quality
   visual ground truth.

Local machine status on 2026-05-20: Chaotica and `flam3-render` were not found
on PATH; only Ultra Fractal 6 was visible under Program Files. JWildfire 9.00
and Temurin JRE 17 were downloaded into ignored artifacts, and FARenderJWF
successfully rendered its bundled selftest flame on the GTX 1070. Aquarium can
now compare against a real external flame renderer artifact, but it still
cannot claim best-in-market visual parity until imported Chaotica renders or a
fully matched JWildfire flame dialect are wired into residual metrics.

Additional sources:

- Chaotica features: https://chaoticafractals.com/features
- Chaotica progressive rendering: https://www.chaoticafractals.com/manual/rendering/progressive_rendering
- Chaotica render settings / sampling level: https://www.chaoticafractals.com/manual/user_interface/render_settings
- JWildfire GitHub / LGPL license: https://github.com/thargor6/JWildfire
- JWildfire headless batch renderer notes: https://www.jwfsanctuary.club/tutorial/how-to/how-do-i-render-a-full-folder-of-flames/
- JWildfire bundled FARender/Swan notes: `artifacts/reference-tools/j-wildfire-9.00/CHANGES.txt`
