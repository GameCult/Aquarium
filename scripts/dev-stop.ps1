$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$fensalirRoot = Resolve-Path (Join-Path $repoRoot "..\Fensalir")
$script = Join-Path $fensalirRoot "scripts\dev-stop.ps1"

& $script
