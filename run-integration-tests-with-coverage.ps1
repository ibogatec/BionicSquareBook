#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

# Move to the script directory (solution root)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $ScriptDir

Write-Host "==> Cleaning previous test and coverage outputs..." -ForegroundColor Cyan
if (Test-Path "./TestResults") { Remove-Item "./TestResults" -Recurse -Force }
if (Test-Path "./CoverageReport") { Remove-Item "./CoverageReport" -Recurse -Force }

# Add dotnet global tools directory to PATH
$dotnetToolsDir = [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile), ".dotnet", "tools")
if (Test-Path $dotnetToolsDir) {
    $env:PATH = "$dotnetToolsDir$([System.IO.Path]::PathSeparator)$env:PATH"
}

# Ensure reportgenerator is available
$reportGen = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
if (-not $reportGen) {
    Write-Host "==> 'reportgenerator' not found. Installing dotnet-reportgenerator-globaltool..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

Write-Host "==> Running integration tests and collecting code coverage..." -ForegroundColor Cyan
dotnet test BionicSquare.IntegrationTests/BionicSquare.IntegrationTests.csproj `
    --settings coverlet.runsettings `
    --collect:"XPlat Code Coverage" `
    --results-directory ./TestResults

Write-Host "==> Generating HTML coverage report..." -ForegroundColor Cyan
reportgenerator `
    -reports:"./TestResults/**/coverage.cobertura.xml" `
    -targetdir:"./CoverageReport" `
    -reporttypes:Html

Write-Host ""
Write-Host "==> Integration tests coverage report successfully generated at:" -ForegroundColor Green
$reportFile = [System.IO.Path]::GetFullPath("./CoverageReport/index.html")
Write-Host "    $reportFile" -ForegroundColor Green
