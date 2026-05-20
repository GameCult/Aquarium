param(
    [int]$Splats = 2000000,
    [int]$SplatUpdates = 2000000,
    [int]$Warmup = 30,
    [int]$Frames = 120,
    [int]$Depth = 8,
    [int]$Candidates = 2,
    [int]$ReservoirUpdates = 50000,
    [int]$ProgramTransforms = 0,
    [string]$ProgramFlame = "",
    [switch]$VisualParity,
    [int]$VisualParityReferenceSamples = 1000000,
    [string]$HistogramSize = "64x64",
    [string]$HistogramBounds = "-8,-8,8,8",
    [string[]]$VisualParityView = @(),
    [int]$ReadbackSplats = 64
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$programArgs = @("--program-transforms", "$ProgramTransforms")
if (-not [string]::IsNullOrWhiteSpace($ProgramFlame)) {
    $programFlamePath = if ([System.IO.Path]::IsPathRooted($ProgramFlame)) { $ProgramFlame } else { Join-Path $repoRoot $ProgramFlame }
    $programArgs = @("--program-flame", $programFlamePath)
}

$parityArgs = @()
if ($VisualParity) {
    $parityArgs = @(
        "--visual-parity",
        "--visual-parity-reference-samples", "$VisualParityReferenceSamples",
        "--histogram-size", $HistogramSize,
        "--histogram-bounds", $HistogramBounds
    )
    foreach ($view in $VisualParityView) {
        $parityArgs += @("--visual-parity-view", $view)
    }
}

dotnet run --project (Join-Path $repoRoot "tools\Aquarium.Fractal.Receipt\Aquarium.Fractal.Receipt.csproj") -c Release -- `
    --splats $Splats `
    --splat-updates $SplatUpdates `
    --warmup $Warmup `
    --frames $Frames `
    --depth $Depth `
    --candidates $Candidates `
    --reservoir-updates $ReservoirUpdates `
    $programArgs `
    $parityArgs `
    --readback-splats $ReadbackSplats
