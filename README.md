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

**Note:** Requires Azure Kinect DK and SDK installed. This opens a GUI window (no terminal output).

```bash
# Build first, then run the WPF GUI
.\BuildAll.ps1
dotnet run --project src/Scanner.UI/Scanner.UI.csproj
```

**Troubleshooting:** If the WPF app doesn't appear to run, check for GUI windows or try the demo instead.

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

🔄 **WPF User Interface** - Live camera feed implemented
- Main window with Material Design styling
- MVVM pattern with Community Toolkit
- Camera connection/disconnection with real-time status
- **Live preview with side-by-side color and depth feeds**
- Settings dialog with camera and scan configuration options
- Scan progress tracking with frame count, points, and elapsed time
- Real-time image display from Azure Kinect (color BGRA32, depth grayscale)
- Missing: Preview window for completed scans, Export dialog

## Building and Running

### Prerequisites

- .NET 6.0 or later
- Windows 10/11 (for Azure Kinect)
- Azure Kinect SDK (for hardware integration)

### Build Commands

```powershell
# Complete build with demo (RECOMMENDED - shows output!)
.\BuildAll.ps1 -Demo

# Build with tests  
.\BuildAll.ps1 -Test

# Clean rebuild (builds but doesn't run anything)
.\BuildAll.ps1 -Clean

# Manual demo run (after building)
dotnet run --project demo/PointCloudDemo/PointCloudDemo.csproj
```

**⚠️ Important:** Type the commands exactly as shown above. Don't copy from VS Code links - they may include invalid references!

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

**Black Camera Feed**: If you see black rectangles instead of camera feed:
1. **Check Camera Connection**: Ensure the green light is on the front of the Kinect
2. **Verify USB 3.0**: Kinect requires USB 3.0 for full functionality
3. **Check Device Manager**: Look for "Azure Kinect DK" under Cameras
4. **Restart Application**: Disconnect and reconnect the camera in the UI
5. **Power Cycle**: Unplug and reconnect the Kinect USB cable

**Live Preview Not Working**: If color/depth feeds don't display:
- Click "Connect Camera" button in the UI
- Check status message for connection errors
- Try "Disconnect" then "Connect Camera" again
- Ensure no other applications are using the Kinect

**Capture/Disposal Errors**: If you see "Cannot access a disposed object" errors:
- **Fixed in latest version** - Capture disposal timing has been corrected
- If still occurring, try restarting the application
- Ensure you're using the latest build with proper capture lifecycle management
- This was caused by premature disposal of Azure Kinect Capture objects

**File Output Location**: Generated files are in `output/` directory relative to execution path.

### IDE and IntelliSense Issues

**InitializeComponent Error**: If you see "The name 'InitializeComponent' does not exist in the current context":

1. **Clean and Rebuild**:
   ```bash
   dotnet clean src/Scanner.UI/Scanner.UI.csproj
   dotnet build src/Scanner.UI/Scanner.UI.csproj
   ```

2. **VS Code Reload**: Reload the window (Ctrl+Shift+P → "Developer: Reload Window")

3. **Delete obj/bin folders**: Remove generated files and rebuild:
   ```bash
   Remove-Item -Recurse -Force src/Scanner.UI/obj, src/Scanner.UI/bin
   dotnet build src/Scanner.UI/Scanner.UI.csproj
   ```

4. **Check XAML Compilation**: Ensure XAML files are building correctly:
   ```bash
   dotnet build src/Scanner.UI/Scanner.UI.csproj --verbosity normal
   ```

**IntelliSense Issues**: XAML code-behind files may show red squiggles in the editor but compile successfully. This is a known issue with VS Code's C# extension and WPF projects.

### Code Quality

**CS1998 Warnings**: If you see "async method lacks 'await' operators", the codebase uses `Task.Run()` for CPU-bound operations in async methods to maintain proper async patterns.

**Compiler Warnings**: The project builds with zero warnings and follows C# best practices for async/await patterns.
