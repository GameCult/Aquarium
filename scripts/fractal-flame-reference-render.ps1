param(
    [string]$Flam3Render = $env:AQUARIUM_FLAM3_RENDER,
    [string]$Flame = "tests\Aquarium.Engine.Fractal.Tests\Fixtures\Apophysis\linear-spherical-bubble.flame",
    [string]$Seed = "0x0BADC0DE",
    [string]$QualityScale = "1",
    [string]$OutputDirectory = "artifacts\fractal-flame-reference"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Flam3Render)) {
    throw "Set -Flam3Render or AQUARIUM_FLAM3_RENDER to a local flam3-render executable. Aquarium does not vendor FLAM3 or Apophysis."
}

$rendererCommand = Get-Command $Flam3Render -ErrorAction SilentlyContinue
if ($null -eq $rendererCommand) {
    throw "Could not find flam3-render executable: $Flam3Render"
}

$renderer = $rendererCommand.Source
$flameInput = if ([System.IO.Path]::IsPathRooted($Flame)) { $Flame } else { Join-Path $repoRoot $Flame }
$flamePath = (Resolve-Path $flameInput).Path
$outDir = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$imagePath = Join-Path $outDir "flam3-reference-$stamp.ppm"

$previous = @{
    in = $env:in
    out = $env:out
    format = $env:format
    seed = $env:seed
    qs = $env:qs
    verbose = $env:verbose
}

try {
    $env:in = $flamePath
    $env:out = $imagePath
    $env:format = "ppm"
    $env:seed = $Seed
    $env:qs = $QualityScale
    $env:verbose = "0"
    & $renderer
    if ($LASTEXITCODE -ne 0) {
        throw "flam3-render failed with exit code $LASTEXITCODE."
    }
}
finally {
    $env:in = $previous.in
    $env:out = $previous.out
    $env:format = $previous.format
    $env:seed = $previous.seed
    $env:qs = $previous.qs
    $env:verbose = $previous.verbose
}

dotnet run --project (Join-Path $repoRoot "tools\Aquarium.Fractal.Receipt\Aquarium.Fractal.Receipt.csproj") -c Release -- `
    --reference-ppm $imagePath `
    --out $outDir
