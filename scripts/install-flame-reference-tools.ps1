param(
    [string]$Destination = "artifacts\reference-tools",
    [string]$JWildfireUrl = "https://jwildfire.overwhale.com/downloads/j-wildfire-9.00.zip",
    [string]$TemurinJreUrl = "https://api.adoptium.net/v3/binary/latest/17/ga/windows/x64/jre/hotspot/normal/eclipse",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$destinationPath = if ([System.IO.Path]::IsPathRooted($Destination)) { $Destination } else { Join-Path $repoRoot $Destination }
New-Item -ItemType Directory -Force -Path $destinationPath | Out-Null

$jwildfireZip = Join-Path $destinationPath "j-wildfire-9.00.zip"
$jwildfireRoot = Join-Path $destinationPath "j-wildfire-9.00"
if ($Force -or -not (Test-Path $jwildfireRoot)) {
    if ($Force -or -not (Test-Path $jwildfireZip)) {
        Invoke-WebRequest -Uri $JWildfireUrl -OutFile $jwildfireZip
    }

    Expand-Archive -LiteralPath $jwildfireZip -DestinationPath $destinationPath -Force
}

$temurinZip = Join-Path $destinationPath "temurin-jre-17.zip"
$temurinRoot = Join-Path $destinationPath "temurin-jre-17"
if ($Force -or -not (Test-Path $temurinRoot)) {
    if ($Force -or -not (Test-Path $temurinZip)) {
        Invoke-WebRequest -Uri $TemurinJreUrl -OutFile $temurinZip
    }

    New-Item -ItemType Directory -Force -Path $temurinRoot | Out-Null
    Expand-Archive -LiteralPath $temurinZip -DestinationPath $temurinRoot -Force
}

$java = Get-ChildItem $temurinRoot -Recurse -Filter java.exe | Select-Object -First 1
$farender = Join-Path $jwildfireRoot "lib\FARenderJWF\FARender.exe"
$jar = Join-Path $jwildfireRoot "lib\j-wildfire.jar"

if (-not (Test-Path $jar)) {
    throw "JWildfire install completed, but lib\j-wildfire.jar was not found under $jwildfireRoot."
}

if (-not (Test-Path $farender)) {
    Write-Warning "JWildfire installed, but FARender.exe was not found. Java rendering may still work."
}

if ($null -eq $java) {
    throw "Temurin JRE install completed, but java.exe was not found under $temurinRoot."
}

Write-Host "AQUARIUM_JWILDFIRE_ROOT=$jwildfireRoot"
Write-Host "AQUARIUM_JWILDFIRE_JAR=$jar"
Write-Host "AQUARIUM_FARENDER=$farender"
Write-Host "AQUARIUM_JAVA=$($java.FullName)"
