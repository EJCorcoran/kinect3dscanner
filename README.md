# Azure Kinect 3D Scanner

A comprehensive 3D scanning application using Azure Kinect DK for creating 3D printable models.

## Features

- **Real-time 3D scanning** using Azure Kinect DK
- **Background removal** for clean object isolation
- **Multi-angle scanning** with object rotation or camera movement
- **Point cloud processing** and mesh generation
- **3D file export** (STL, PLY, OBJ) for 3D printing
- **Interactive preview** with real-time visualization

## Architecture

### Core Components

1. **KinectCore (C#)** - Camera interface and data capture
2. **Scanner.UI (WPF/C#)** - Main application interface
3. **PointCloudProcessor (C#)** - 3D data processing
4. **MeshGenerator (C#)** - Surface reconstruction
5. **FileExporter (C#)** - 3D file format export

### Technology Stack

- **Azure Kinect SDK** - Camera interface
- **WPF** - User interface
- **Open3D.NET** - Point cloud processing
- **MeshLab.NET** - Mesh operations
- **Math.NET** - Mathematical operations

## Requirements

- Azure Kinect DK camera
- Windows 10/11
- .NET 6.0 or later
- Azure Kinect SDK
- Visual Studio 2022 (recommended)

## Quick Start

### Running the Demo (No Hardware Required)

The easiest way to see the system in action is to run the working demo:

```bash
# Using the build script
.\BuildAll.ps1 -Demo

# Or manually
dotnet run --project demo/PointCloudDemo/PointCloudDemo.csproj
```

This demonstrates the complete point cloud processing pipeline without requiring Azure Kinect hardware.

### Building the Full Solution

```bash
# Build everything
.\BuildAll.ps1

# Build with tests
.\BuildAll.ps1 -Test

# Clean and rebuild
.\BuildAll.ps1 -Clean -Test
```

### Running the WPF Application

**Note:** Requires Azure Kinect DK and SDK installed.

```bash
dotnet run --project src/Scanner.UI/Scanner.UI.csproj
```

## Demo Files Output

When you run the demo, it creates these files in the `output/` directory:
- `pointcloud.ply` - Point cloud data (viewable in CloudCompare, MeshLab)
- `mesh.stl` - 3D printable mesh (for slicers like PrusaSlicer, Cura)  
- `mesh.obj` - 3D model with materials (for Blender, Maya, etc.)

## Build Requirements

### For Core Libraries Only:
- .NET 6.0 SDK
- Windows 10/11

### For Full Azure Kinect Integration:
- Azure Kinect DK hardware
- Azure Kinect SDK v1.4.1+
- Visual Studio 2022 (recommended)
- x64 platform build

## Build Instructions

### Step 1: Setup Environment
```powershell
# Restore packages
dotnet restore

# Build core libraries (works without Kinect)
dotnet build src\PointCloudProcessor --configuration Release
dotnet build src\MeshGenerator --configuration Release
dotnet build src\FileExporter --configuration Release
```

### Step 2: Azure Kinect Setup (Optional)
1. Download and install [Azure Kinect SDK](https://docs.microsoft.com/en-us/azure/kinect-dk/sensor-sdk-download)
2. Connect Azure Kinect DK via USB 3.0
3. Build with x64 platform:
```powershell
# Build full solution with Kinect support
dotnet build --configuration Release --property:Platform=x64
```

## Project Structure

```
├── src/
│   ├── KinectCore/              # C# Azure Kinect interface
│   ├── Scanner.UI/              # WPF application
│   ├── PointCloudProcessor/     # 3D data processing
│   ├── MeshGenerator/           # Surface reconstruction
│   └── FileExporter/            # Export functionality
├── tests/                       # Unit tests
├── docs/                        # Documentation
└── samples/                     # Example scans
```

## Project Status

✅ **Core Processing Pipeline** - Complete and tested
- Point cloud processing with filtering and normal estimation
- Mesh generation using Delaunay triangulation
- File export (STL, PLY, OBJ) for 3D printing
- Modular architecture with separate processing components

✅ **Working Demo** - Functional without hardware
- Generates synthetic point cloud data
- Demonstrates full processing pipeline
- Outputs 3D-printable files

✅ **Azure Kinect Integration** - Core implementation complete
- Camera service with depth and color capture
- Background removal and filtering
- Platform-specific x64 builds for Azure Kinect SDK

🔄 **WPF User Interface** - Basic structure implemented
- Main window with Material Design
- MVVM pattern with Community Toolkit
- Camera connection and scanning controls
- Missing: Settings, Preview, and Export dialogs

## Building and Running

### Prerequisites

- .NET 6.0 or later
- Windows 10/11 (for Azure Kinect)
- Azure Kinect SDK (for hardware integration)

### Build Commands

```bash
# Complete build with demo
.\BuildAll.ps1 -Demo

# Build with tests  
.\BuildAll.ps1 -Test

# Clean rebuild
.\BuildAll.ps1 -Clean
```

## Troubleshooting

### Common Build Issues

**Platform Target Error**: If you see "Azure Kinect only supports the x86/x64 platform ('AnyCPU' not supported)":
```bash
# Build with explicit platform
dotnet build --runtime win-x64 --configuration Debug
```

**Missing Azure Kinect SDK**: Core processing works without the SDK. Only full WPF app requires it.

**Build Script Issues**: Use the enhanced build script for complex builds:
```bash
.\BuildAll.ps1 -Clean -Test -Demo
```

### Runtime Issues

**No Azure Kinect Device**: The demo works without hardware. For real scanning, ensure:
- Azure Kinect DK is connected via USB 3.0
- Azure Kinect SDK is installed
- Device drivers are properly installed

**File Output Location**: Generated files are in `output/` directory relative to execution path.
