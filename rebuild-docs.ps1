#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Cleanly rebuilds the DotNetDocs documentation by clearing caches and rebuilding from scratch.

.DESCRIPTION
    This script performs a complete clean rebuild of the documentation:
    1. Kills all build server processes
    2. Clears the entire NuGet cache
    3. Rebuilds the SDK project
    4. Regenerates the documentation

.EXAMPLE
    .\rebuild-docs.ps1
#>

param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

Write-Host "Starting clean documentation rebuild..." -ForegroundColor Cyan
Write-Host ""

# Step 1: Kill all build server processes
Write-Host "Step 1: Killing build server processes..." -ForegroundColor Yellow
$processesToKill = @("dotnet", "MSBuild", "VBCSCompiler")
foreach ($processName in $processesToKill) {
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "   Killing $($processes.Count) $processName process(es)..." -ForegroundColor Gray
        $processes | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}
Write-Host "   Build server processes terminated" -ForegroundColor Green
Write-Host ""

# Step 2: Clear NuGet cache
Write-Host "Step 2: Clearing NuGet cache..." -ForegroundColor Yellow
Write-Host "   This may take a moment..." -ForegroundColor Gray
& dotnet nuget locals all --clear | Out-String | Write-Host
Write-Host "   NuGet cache cleared" -ForegroundColor Green
Write-Host ""

# Step 3: Restore packages
Write-Host "Step 3: Restoring packages..." -ForegroundColor Yellow
Write-Host "   Running dotnet restore..." -ForegroundColor Gray
& dotnet restore src\CloudNimble.DotNetDocs.slnx
if ($LASTEXITCODE -ne 0) {
    throw "Package restore failed with exit code $LASTEXITCODE"
}
Write-Host "   Packages restored" -ForegroundColor Green
Write-Host ""

# Step 4: Rebuild SDK project
Write-Host "Step 4: Rebuilding SDK project..." -ForegroundColor Yellow
Push-Location "src\CloudNimble.DotNetDocs.Sdk"
try {
    Write-Host "   Building CloudNimble.DotNetDocs.Sdk..." -ForegroundColor Gray
    & dotnet build -c $Configuration --no-incremental
    if ($LASTEXITCODE -ne 0) {
        throw "SDK build failed with exit code $LASTEXITCODE"
    }

    Write-Host "   Packing SDK..." -ForegroundColor Gray
    & dotnet pack -c $Configuration --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "SDK pack failed with exit code $LASTEXITCODE"
    }

    Write-Host "   Pushing to local feed..." -ForegroundColor Gray
    $nupkgFile = Get-ChildItem -Path "bin\$Configuration\*.nupkg" | Select-Object -First 1
    if ($nupkgFile) {
        & dotnet nuget push $nupkgFile.FullName --source local-feed --skip-duplicate
        if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 1) {  # Exit code 1 is OK (duplicate)
            throw "NuGet push failed with exit code $LASTEXITCODE"
        }
    }
    Write-Host "   SDK rebuilt and deployed" -ForegroundColor Green
}
finally {
    Pop-Location
}
Write-Host ""

# Step 5: Rebuild documentation
Write-Host "Step 5: Regenerating documentation..." -ForegroundColor Yellow
Push-Location "src\CloudNimble.DotNetDocs.Docs"
try {
    Write-Host "   Running documentation generation..." -ForegroundColor Gray
    & dotnet msbuild -t:GenerateDocumentation -p:Configuration=$Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Documentation generation failed with exit code $LASTEXITCODE"
    }
    Write-Host "   Documentation regenerated successfully" -ForegroundColor Green
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Documentation rebuild completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Generated documentation location:" -ForegroundColor Cyan
Write-Host "   src\CloudNimble.DotNetDocs.Docs\api-reference" -ForegroundColor White