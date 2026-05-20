# Fractal Surface Visible Slice

Objective: make the live Zyphos scene test authored fractal reservoir terrain instead of a decorative proxy.

Kept:
- GPU-resident 2M splat reservoir field.
- Separate SDF, PBR, and radiosity reservoir layers.
- `.aquageo` selected-cut program lowering.

Changed:
- Packed IFS transform rows now preserve cube-sphere tile face/level/x/y.
- Compute maps authored splats onto cube-sphere faces instead of a generic +Z cap.
- Visible splats render as tangent-frame surface patches, not camera-facing stickers.
- Zyphos camera targets the selected domain while keeping the parent as orbit anchor.

Still not good enough:
- The live view is fast and the reservoir is populated, but the current surface-consumption pass still reads too abstractly under the noisy starfield.
- Next coherent cut is a real clustered surface resolve or ray-query style splat surface pass, not more billboard cosmetics.
