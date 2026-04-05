[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release', 'Beta')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'TrafficLightsEnhancement\TrafficLightsEnhancement.csproj'

if (-not (Test-Path $projectPath)) {
    throw "Could not find mod project at $projectPath"
}

Write-Host "Running verification build for $Configuration..." -ForegroundColor Cyan

& dotnet build $projectPath -c $Configuration -p:DisablePostProcessors=true
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Warning 'Verification build succeeded, but it did not run the Cities II postprocessor or deploy a playable mod.'
Write-Warning 'The UI bundle may still rebuild during this step. Treat that as a partial artifact update, not a full installed mod refresh.'
Write-Host 'Before launching the game or closing out work, run: .\scripts\Deploy-Mod.ps1' -ForegroundColor Yellow
