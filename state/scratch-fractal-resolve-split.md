Objective: keep the shared Form reservoir while making render resolve semantics explicit.

Current mechanism before this slice: one D3D12FractalSplatRender pixel path drew both opaque SDF surface claims and transparent density claims. That preserved the packet stride but hid depth/blend behavior in shader conditionals.

Invariant after this slice: one GPU-resident splat/reservoir set can carry multiple Form encodings, but renderer pass ownership must split where output-merge semantics split. SignedDistance owns opaque depth-writing surface resolve. Density/Extinction owns transparent additive color accumulation until a real volumetric target exists.

Cut line: do not add a parallel transparent reservoir or CPU-side splat list to paper over pass semantics. Add targets/passes only when the invariant they protect is explicit and measured.
