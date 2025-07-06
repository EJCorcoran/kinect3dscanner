@echo off
echo Azure Kinect 3D Scanner - Quick Setup
echo =====================================

echo.
echo Checking prerequisites...

REM Check if .NET 6 is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET 6.0 SDK not found
    echo Please install .NET 6.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET SDK found: 
dotnet --version

echo.
echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

echo.
echo Building solution...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Setup completed successfully!
echo.
echo Next steps:
echo 1. Connect your Azure Kinect DK to a USB 3.0 port
echo 2. Install Azure Kinect SDK if not already installed
echo 3. Run the application with: dotnet run --project src\Scanner.UI
echo.
echo For detailed instructions, see docs\INSTALLATION.md
echo.
pause
