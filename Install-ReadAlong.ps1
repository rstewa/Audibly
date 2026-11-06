param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Start-ReadAlong.ps1') -Install -NoBuild:$NoBuild
