param(
    [ValidateSet("Auto", "FARender", "JWildfireJava")]
    [string]$Renderer = "Auto",
    [string]$JWildfireRoot = $env:AQUARIUM_JWILDFIRE_ROOT,
    [string]$FARender = $env:AQUARIUM_FARENDER,
    [string]$JWildfireJar = $env:AQUARIUM_JWILDFIRE_JAR,
    [string]$Java = $env:AQUARIUM_JAVA,
    [string]$Flame = "artifacts\reference-tools\j-wildfire-9.00\lib\FARenderJWF\selftest.flame",
    [int]$Width = 1024,
    [int]$Height = 1024,
    [int]$Quality = 5000,
    [string]$OutputDirectory = "artifacts\fractal-flame-jwildfire-reference",
    [switch]$NoCuda,
    [switch]$NoDensityEstimation
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($JWildfireRoot)) {
    $defaultRoot = Join-Path $repoRoot "artifacts\reference-tools\j-wildfire-9.00"
    if (Test-Path $defaultRoot) {
        $JWildfireRoot = $defaultRoot
    }
}

$flameInput = if ([System.IO.Path]::IsPathRooted($Flame)) { $Flame } else { Join-Path $repoRoot $Flame }
$flamePath = (Resolve-Path $flameInput).Path
$outDir = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $repoRoot $OutputDirectory }
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

if ([string]::IsNullOrWhiteSpace($FARender) -and -not [string]::IsNullOrWhiteSpace($JWildfireRoot)) {
    $candidate = Join-Path $JWildfireRoot "lib\FARenderJWF\FARender.exe"
    if (Test-Path $candidate) {
        $FARender = $candidate
    }
}

if ([string]::IsNullOrWhiteSpace($JWildfireJar) -and -not [string]::IsNullOrWhiteSpace($JWildfireRoot)) {
    $candidate = Join-Path $JWildfireRoot "lib\j-wildfire.jar"
    if (Test-Path $candidate) {
        $JWildfireJar = $candidate
    }
}

if ($Renderer -eq "Auto") {
    $Renderer = if (-not [string]::IsNullOrWhiteSpace($FARender)) { "FARender" } else { "JWildfireJava" }
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"

if ($Renderer -eq "FARender") {
    if ([string]::IsNullOrWhiteSpace($FARender)) {
        throw "Set -FARender, AQUARIUM_FARENDER, or AQUARIUM_JWILDFIRE_ROOT. FARender is the preferred scriptable GPU visual reference."
    }

    $farenderPath = (Resolve-Path $FARender).Path
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($flamePath)
    $outputPath = Join-Path $outDir "$baseName-farender-$stamp.png"
    $arguments = @($Width, $Height, $flamePath, "-o", $outputPath, "-q", $Quality)
    if ($NoDensityEstimation) {
        $arguments += "-nde"
    }

    if (-not $NoCuda) {
        $arguments += "-cuda"
    }

    Push-Location (Split-Path -Parent $farenderPath)
    try {
        & $farenderPath @arguments
    }
    finally {
        Pop-Location
    }

    if ($LASTEXITCODE -ne 0) {
        throw "FARender failed with exit code $LASTEXITCODE."
    }

    $rendered = Get-Item $outputPath -ErrorAction SilentlyContinue
    if ($null -eq $rendered -or $rendered.Length -le 0) {
        throw "FARender reported success but did not produce a non-empty image at $outputPath."
    }

    Write-Host "JWildfire FARender reference image: $outputPath"
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Java)) {
    $localJava = Get-ChildItem (Join-Path $repoRoot "artifacts\reference-tools\temurin-jre-17") -Recurse -Filter java.exe -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $localJava) {
        $Java = $localJava.FullName
    }
    else {
        $javaCommand = Get-Command java -ErrorAction SilentlyContinue
        if ($null -eq $javaCommand) {
            throw "Set -Java or AQUARIUM_JAVA to a Java executable. JWildfire's Java renderer needs Java."
        }

        $Java = $javaCommand.Source
    }
}

if ([string]::IsNullOrWhiteSpace($JWildfireJar)) {
    throw "Set -JWildfireJar, AQUARIUM_JWILDFIRE_JAR, or AQUARIUM_JWILDFIRE_ROOT to a local JWildfire install."
}

$jarPath = (Resolve-Path $JWildfireJar).Path
$batchDir = Join-Path $outDir "jwildfire-batch-$stamp"
New-Item -ItemType Directory -Force -Path $batchDir | Out-Null
Copy-Item -LiteralPath $flamePath -Destination (Join-Path $batchDir (Split-Path -Leaf $flamePath))

& $Java -cp $jarPath org.jwildfire.create.tina.batch.HeadlessBatchRendererController $batchDir $Width $Height $Quality
if ($LASTEXITCODE -ne 0) {
    throw "JWildfire headless renderer failed with exit code $LASTEXITCODE."
}

$renderedImages = Get-ChildItem $batchDir -Include *.png,*.jpg,*.jpeg -Recurse -ErrorAction SilentlyContinue
if ($renderedImages.Count -eq 0) {
    Write-Warning "JWildfire Java batch completed, but no image was written into the batch directory. Check JWildfire's configured output folder or use -Renderer FARender for explicit -o output."
}

Write-Host "JWildfire reference render directory: $batchDir"
