# FastStart Installer

This directory contains the Inno Setup installer script and build tools for FastStart.

## Prerequisites

1. **Visual Studio 2022 Build Tools** (or Visual Studio 2022)
   - Required for building WinUI 3 projects
   - Download: https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022

2. **Inno Setup 6**
   - Download: https://jrsoftware.org/isdl.php
   - Install to default location (`C:\Program Files (x86)\Inno Setup 6`)

3. **.NET 8 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0

## Building the Installer

### Using PowerShell Script (Recommended)

```powershell
# From the repository root
.\installer\build-installer.ps1

# Or from the installer directory
.\build-installer.ps1

# Skip rebuild if you already have a publish folder
.\build-installer.ps1 -SkipBuild
```

### Manual Build

1. Publish the UI project:
   ```cmd
   msbuild src\FastStart.UI\FastStart.UI.csproj -t:Publish -p:Configuration=Release -p:PublishDir=..\..\..\publish -p:RuntimeIdentifier=win-x64 -restore
   ```

2. Compile the installer:
   ```cmd
   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\FastStart.iss
   ```

## Output

The installer will be created at:
```
dist\FastStartSetup-1.0.0.exe
```

## Installer Features

- Installs to `C:\Program Files\FastStart` by default
- Creates Start Menu shortcuts
- Optional desktop shortcut
- Optional auto-start with Windows
- Checks for .NET 8 Desktop Runtime and prompts to download if missing
- Full uninstall support

## Files

- `FastStart.iss` - Inno Setup script
- `build-installer.ps1` - PowerShell build script
- `README.md` - This file
