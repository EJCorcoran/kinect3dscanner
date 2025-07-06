# Build script for Azure Kinect 3D Scanner

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$Clean,
    [switch]$Restore,
    [switch]$Test,
    [switch]$Package
)

Write-Host "Azure Kinect 3D Scanner Build Script" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Platform: $Platform" -ForegroundColor Yellow

# Set location to script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning solution..." -ForegroundColor Yellow
    dotnet clean --configuration $Configuration
    if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
    if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }
}

# Restore packages if requested
if ($Restore -or $Clean) {
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Package restore failed"
        exit 1
    }
}

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Run tests if requested
if ($Test) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed"
        exit 1
    }
}

# Package if requested
if ($Package) {
    Write-Host "Creating package..." -ForegroundColor Yellow
    $OutputDir = "dist"
    if (Test-Path $OutputDir) { Remove-Item -Recurse -Force $OutputDir }
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
    
    # Publish main application
    dotnet publish src\Scanner.UI --configuration $Configuration --output "$OutputDir\Scanner" --self-contained false
    
    # Copy documentation
    Copy-Item "README.md" "$OutputDir\"
    Copy-Item -Recurse "docs" "$OutputDir\"
    
    # Copy sample data if exists
    if (Test-Path "samples") {
        Copy-Item -Recurse "samples" "$OutputDir\"
    }
    
    Write-Host "Package created in $OutputDir" -ForegroundColor Green
}

Write-Host "Build completed successfully!" -ForegroundColor Green
