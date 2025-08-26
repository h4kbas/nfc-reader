#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$Test,
    [switch]$Pack,
    [switch]$Help
)

if ($Help) {
    Write-Host @"
NFC Reader Library Build Script

Usage: .\build.ps1 [options]

Options:
    -Configuration <config>    Build configuration (Debug|Release) [default: Release]
    -Clean                    Clean build artifacts before building
    -Test                     Run tests after building
    -Pack                     Create NuGet package after building
    -Help                     Show this help message

Examples:
    .\build.ps1                    # Build in Release mode
    .\build.ps1 -Configuration Debug  # Build in Debug mode
    .\build.ps1 -Clean -Test      # Clean, build, and test
    .\build.ps1 -Clean -Test -Pack # Clean, build, test, and package
"@
    exit 0
}

Write-Host "NFC Reader Library Build Script" -ForegroundColor Green
Write-Host "===============================" -ForegroundColor Green
Write-Host ""

# Check if .NET is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "Using .NET SDK version: $dotnetVersion" -ForegroundColor Yellow
} catch {
    Write-Error ".NET SDK not found. Please install .NET 6.0 or later."
    exit 1
}

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow
    dotnet clean
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Clean failed"
        exit 1
    }
    Write-Host "Clean completed successfully" -ForegroundColor Green
}

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Restore failed"
    exit 1
}
Write-Host "Dependencies restored successfully" -ForegroundColor Green

# Build main project
Write-Host "Building main project..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}
Write-Host "Build completed successfully" -ForegroundColor Green

# Build examples
Write-Host "Building examples..." -ForegroundColor Yellow
dotnet build Examples/Examples.csproj --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Examples build failed"
    exit 1
}
Write-Host "Examples built successfully" -ForegroundColor Green

# Run tests if requested
if ($Test) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed"
        exit 1
    }
    Write-Host "Tests completed successfully" -ForegroundColor Green
}

# Create package if requested
if ($Pack) {
    Write-Host "Creating NuGet package..." -ForegroundColor Yellow
    dotnet pack NFCReader.csproj --configuration $Configuration --no-build --output ./nupkg
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Package creation failed"
        exit 1
    }
    Write-Host "Package created successfully in ./nupkg directory" -ForegroundColor Green
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
