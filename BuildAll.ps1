# Enhanced Build script for Azure Kinect 3D Scanner

param(
    [string]$Configuration = "Debug",
    [switch]$Clean,
    [switch]$Test,
    [switch]$Demo
)

Write-Host "Azure Kinect 3D Scanner - Build Script" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

$ErrorActionPreference = "Stop"

try {
    if ($Clean) {
        Write-Host "Cleaning solution..." -ForegroundColor Yellow
        dotnet clean KinectScanner.sln --configuration $Configuration
        if (Test-Path "output") { Remove-Item "output" -Recurse -Force }
    }

    Write-Host "Restoring packages..." -ForegroundColor Yellow
    dotnet restore KinectScanner.sln

    Write-Host "Building core processing modules..." -ForegroundColor Yellow
    
    $coreProjects = @(
        "src/PointCloudProcessor/PointCloudProcessor.csproj",
        "src/MeshGenerator/MeshGenerator.csproj", 
        "src/FileExporter/FileExporter.csproj"
    )
    
    foreach ($project in $coreProjects) {
        Write-Host "  Building $project" -ForegroundColor Gray
        dotnet build $project --configuration $Configuration --no-restore
    }
    
    Write-Host "Building Azure Kinect modules..." -ForegroundColor Yellow
    
    Write-Host "  Building KinectCore (x64 only)" -ForegroundColor Gray
    dotnet build "src/KinectCore/KinectCore.csproj" --configuration $Configuration --runtime win-x64 --no-restore
    
    Write-Host "  Building Scanner.UI" -ForegroundColor Gray
    dotnet build "src/Scanner.UI/Scanner.UI.csproj" --configuration $Configuration --no-restore
    
    Write-Host "Building tests..." -ForegroundColor Yellow
    dotnet build "tests/KinectCore.Tests/KinectCore.Tests.csproj" --configuration $Configuration --no-restore
    
    Write-Host "Building demo..." -ForegroundColor Yellow
    dotnet build "demo/PointCloudDemo/PointCloudDemo.csproj" --configuration $Configuration --no-restore

    if ($Test) {
        Write-Host "Running tests..." -ForegroundColor Yellow
        dotnet test "tests/KinectCore.Tests/KinectCore.Tests.csproj" --configuration $Configuration --no-build
    }

    if ($Demo) {
        Write-Host "Running demo..." -ForegroundColor Yellow
        dotnet run --project "demo/PointCloudDemo/PointCloudDemo.csproj" --configuration $Configuration --no-build
    }

    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Available commands:" -ForegroundColor Cyan
    Write-Host "  Run demo:      dotnet run --project demo/PointCloudDemo/PointCloudDemo.csproj" -ForegroundColor White
    Write-Host "  Run WPF app:   dotnet run --project src/Scanner.UI/Scanner.UI.csproj" -ForegroundColor White
    Write-Host "  Run tests:     dotnet test tests/KinectCore.Tests/KinectCore.Tests.csproj" -ForegroundColor White

} catch {
    Write-Host "Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
