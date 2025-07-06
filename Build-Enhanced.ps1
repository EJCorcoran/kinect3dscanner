# Enhanced Build script for Azure Kinect 3D Scanner

param(
    [string]$Configuration = "Debug",
    [switch]$Clean,
    [switch]$NoBuild,
    [switch]$NoRestore,
    [switch]$Test,
    [switch]$Demo
)

Write-Host "Azure Kinect 3D Scanner - Build Script" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

# Set error handling
$ErrorActionPreference = "Stop"

try {
    # Clean if requested
    if ($Clean) {
        Write-Host "🧹 Cleaning solution..." -ForegroundColor Yellow
        dotnet clean KinectScanner.sln --configuration $Configuration
        if (Test-Path "output") { Remove-Item "output" -Recurse -Force }
    }

    # Restore packages if not skipped
    if (-not $NoRestore) {
        Write-Host "📦 Restoring packages..." -ForegroundColor Yellow
        dotnet restore KinectScanner.sln
    }

    # Build projects if not skipped
    if (-not $NoBuild) {
        Write-Host "🔨 Building core processing modules..." -ForegroundColor Yellow
        
        # Build projects that don't require Azure Kinect first
        $coreProjects = @(
            "src/PointCloudProcessor/PointCloudProcessor.csproj",
            "src/MeshGenerator/MeshGenerator.csproj", 
            "src/FileExporter/FileExporter.csproj"
        )
        
        foreach ($project in $coreProjects) {
            Write-Host "   Building $project" -ForegroundColor Gray
            dotnet build $project --configuration $Configuration --no-restore
        }
        
        # Build Azure Kinect dependent projects with explicit platform
        Write-Host "🔨 Building Azure Kinect modules..." -ForegroundColor Yellow
        
        # Build KinectCore with specific runtime
        Write-Host "   Building KinectCore (x64 only)" -ForegroundColor Gray
        dotnet build "src/KinectCore/KinectCore.csproj" --configuration $Configuration --runtime win-x64 --no-restore
        
        # Build Scanner.UI
        Write-Host "   Building Scanner.UI" -ForegroundColor Gray
        dotnet build "src/Scanner.UI/Scanner.UI.csproj" --configuration $Configuration --no-restore
        
        # Build tests
        Write-Host "🧪 Building tests..." -ForegroundColor Yellow
        dotnet build "tests/KinectCore.Tests/KinectCore.Tests.csproj" --configuration $Configuration --no-restore
        
        # Build demo
        Write-Host "🎯 Building demo..." -ForegroundColor Yellow
        dotnet build "demo/PointCloudDemo/PointCloudDemo.csproj" --configuration $Configuration --no-restore
    }

    # Run tests if requested
    if ($Test) {
        Write-Host "🧪 Running tests..." -ForegroundColor Yellow
        dotnet test "tests/KinectCore.Tests/KinectCore.Tests.csproj" --configuration $Configuration --no-build
    }

    # Run demo if requested
    if ($Demo) {
        Write-Host "🎯 Running demo..." -ForegroundColor Yellow
        dotnet run --project "demo/PointCloudDemo/PointCloudDemo.csproj" --configuration $Configuration --no-build
    }

    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Available commands:" -ForegroundColor Cyan
    Write-Host "  Run demo:               dotnet run --project demo/PointCloudDemo/PointCloudDemo.csproj" -ForegroundColor White
    Write-Host "  Run WPF app:            dotnet run --project src/Scanner.UI/Scanner.UI.csproj" -ForegroundColor White
    Write-Host "  Run tests:              dotnet test tests/KinectCore.Tests/KinectCore.Tests.csproj" -ForegroundColor White
    Write-Host "  Build with demo:        .\Build.ps1 -Demo" -ForegroundColor White
    Write-Host "  Clean and rebuild:      .\Build.ps1 -Clean" -ForegroundColor White

} catch {
    Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
