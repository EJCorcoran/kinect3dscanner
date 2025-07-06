# Installation Guide

## Prerequisites

### 1. Azure Kinect SDK
- Download and install the [Azure Kinect SDK](https://docs.microsoft.com/en-us/azure/kinect-dk/sensor-sdk-download)
- Ensure the Azure Kinect DK is connected via USB 3.0

### 2. Development Environment
- Visual Studio 2022 (Community or higher)
- .NET 6.0 SDK
- Windows 10/11 (x64)

### 3. Hardware Requirements
- Azure Kinect DK camera
- USB 3.0 port
- 8GB+ RAM recommended
- Graphics card with OpenGL 3.3+ support

## Setup Instructions

### 1. Clone and Build
```powershell
# Open PowerShell in project directory
cd C:\Users\edcorcor\VSCode\kinect3dscanner

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build --configuration Release
```

### 2. Run the Application
```powershell
# Start the WPF application
dotnet run --project src\Scanner.UI --configuration Release
```

### 3. Camera Setup
1. Connect Azure Kinect DK to USB 3.0 port
2. Click "Connect Camera" in the application
3. Position object on a stable surface
4. Adjust lighting for optimal scanning

## First Scan

### 1. Background Capture
- Clear the scanning area of the object
- Click "Capture Background" 
- Wait for 30 frames to be captured

### 2. Object Positioning
- Place object in scanning area
- Ensure object is well-lit
- Object should be within 0.5-3 meters from camera

### 3. Scanning Process
- Click "Start Scan"
- Either rotate the object manually or move camera around object
- Monitor progress in the UI
- Click "Stop Scan" when complete

### 4. Export Results
- View scan in preview window
- Export to STL, PLY, or OBJ format
- Import into 3D printing software

## Troubleshooting

### Camera Connection Issues
- Verify Azure Kinect SDK installation
- Check USB 3.0 connection
- Try different USB port
- Restart application

### Poor Scan Quality
- Improve lighting conditions
- Reduce background objects
- Move closer to object (within 1-2 meters)
- Scan object from more angles

### Performance Issues
- Close other applications
- Reduce scan frame count
- Lower depth resolution in settings
- Ensure adequate RAM available

## Advanced Configuration

### Scan Settings
- **Frames to Capture**: 50-200 frames (more = better quality, longer time)
- **Max Depth**: Adjust based on object size and distance
- **Background Removal**: Enable for cleaner scans
- **Noise Reduction**: Enable for smoother results

### Export Options
- **STL**: Best for 3D printing
- **PLY**: Includes color information
- **OBJ**: Compatible with most 3D software
- **JSON**: Metadata and scan information
