param(
    [string]$Flame = "tests\Aquarium.Engine.Fractal.Tests\Fixtures\Apophysis\linear-spherical-bubble.flame",
    [int]$Samples = 8192,
    [int]$BurnIn = 64,
    [string]$HistogramSize = "64x64",
    [string]$Bounds = "-8,-8,8,8",
    [string]$Seed = "0x0BADC0DE"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
dotnet run --project (Join-Path $repoRoot "tools\Aquarium.Fractal.Receipt\Aquarium.Fractal.Receipt.csproj") -c Release -- `
    --flame (Join-Path $repoRoot $Flame) `
    --histogram-samples $Samples `
    --histogram-burn-in $BurnIn `
    --histogram-size $HistogramSize `
    --histogram-bounds $Bounds `
    --seed $Seed `
    --out (Join-Path $repoRoot "artifacts\fractal-flame-receipts")
