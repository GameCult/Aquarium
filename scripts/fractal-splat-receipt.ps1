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
    [int]$ReadbackSplats = 64
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$programArgs = @("--program-transforms", "$ProgramTransforms")
if (-not [string]::IsNullOrWhiteSpace($ProgramFlame)) {
    $programFlamePath = if ([System.IO.Path]::IsPathRooted($ProgramFlame)) { $ProgramFlame } else { Join-Path $repoRoot $ProgramFlame }
    $programArgs = @("--program-flame", $programFlamePath)
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
    --readback-splats $ReadbackSplats
