[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release', 'Beta')]
    [string]$Configuration = 'Beta'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'TrafficLightsEnhancement\TrafficLightsEnhancement.csproj'
$modId = 'C2VM.TrafficLightsEnhancement'
$localModsPath = [System.Environment]::GetEnvironmentVariable('CSII_LOCALMODSPATH', 'User')

if (-not (Test-Path $projectPath)) {
    throw "Could not find mod project at $projectPath"
}

if ([string]::IsNullOrWhiteSpace($localModsPath)) {
    throw 'CSII_LOCALMODSPATH is not set. Install or refresh the Cities II modding toolchain before deploying.'
}

$deployDir = Join-Path $localModsPath $modId
$citiesProcess = Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue

if ($citiesProcess) {
    Write-Warning 'Cities II is currently running. The deploy can still update files on disk, but restart the game before testing the rebuilt mod.'
}

Write-Host "Running full playable build for $Configuration..." -ForegroundColor Cyan

& dotnet build $projectPath -c $Configuration -v:m
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$requiredFiles = @(
    'C2VM.TrafficLightsEnhancement.dll',
    'C2VM.TrafficLightsEnhancement_win_x86_64.dll',
    'TrafficLightsEnhancement.Logic.dll',
    'C2VM.CommonLibraries.LaneSystem.dll',
    'C2VM.TrafficLightsEnhancement.mjs'
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    $path = Join-Path $deployDir $file
    if (-not (Test-Path $path)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    throw "Deploy completed, but required files are missing from ${deployDir}: $($missingFiles -join ', ')"
}

Write-Host "Playable mod deployed to $deployDir" -ForegroundColor Green
Write-Host 'Required managed, native, and UI artifacts were found in the deployed folder.' -ForegroundColor Green

if ($citiesProcess) {
    Write-Warning 'Restart Cities II so it loads the rebuilt mod package from disk.'
}
