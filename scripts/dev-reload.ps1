param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$fensalirRoot = Resolve-Path (Join-Path $repoRoot "..\Fensalir")
$clientProject = Join-Path $repoRoot "src\Aquarium.Epiphany\Aquarium.Epiphany.csproj"
$script = Join-Path $fensalirRoot "scripts\dev-reload.ps1"

& $script -ClientProject $clientProject @RemainingArgs
