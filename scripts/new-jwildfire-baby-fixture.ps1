param(
    [string]$Source = "artifacts\reference-tools\j-wildfire-9.00\lib\FARenderJWF\selftest.flame",
    [string]$Output = "artifacts\fractal-flame-jwildfire-reference\julian-disc-baby.flame"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$sourcePath = if ([System.IO.Path]::IsPathRooted($Source)) { $Source } else { Join-Path $repoRoot $Source }
$outputPath = if ([System.IO.Path]::IsPathRooted($Output)) { $Output } else { Join-Path $repoRoot $Output }
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outputPath) | Out-Null

$text = [System.IO.File]::ReadAllText((Resolve-Path $sourcePath))
$text = $text.Replace('JulianDisc - 535389044', 'julian-disc-baby')
$text = $text.Replace('jwf_julian_power="126"', 'jwf_julian_power="8"')
$text = $text.Replace('jwf_gaussian_blur="0.039366260122290476"', 'jwf_gaussian_blur="0.0"')
$text = $text.Replace('wfield_var_amount="0.21416904132907888"', 'wfield_var_amount="0.0"')
$text = $text.Replace('wfield_color_amount="0.26564915787570376"', 'wfield_color_amount="0.0"')
$text = $text.Replace('wfield_jitter_amount="0.13887944166913668"', 'wfield_jitter_amount="0.0"')

[System.IO.File]::WriteAllText($outputPath, $text, [System.Text.UTF8Encoding]::new($false))
Write-Host "JWildfire baby fixture: $outputPath"
