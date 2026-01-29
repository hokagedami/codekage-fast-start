# FastStart

Windows 11 Start Menu replacement focused on speed and low memory usage.

## Prerequisites

- Windows 10/11 (x64)
- .NET 8.0 SDK
- Visual Studio 2022 Build Tools (required for WinUI 3)
- Windows App SDK 1.6+

## Project Structure

```
src/
  FastStart.Core/      # Domain models, interfaces, services (search, caching)
  FastStart.Data/      # EF Core + SQLite persistence layer
  FastStart.Native/    # Windows-specific: COM interop, UWP enumeration, app launching
  FastStart.UI/        # WinUI 3 application (unpackaged)
tests/
  FastStart.Tests/     # Unit tests
  FastStart.Benchmarks/ # Performance benchmarks
```

## Build

WinUI 3 projects require MSBuild (Visual Studio Build Tools). The `dotnet` CLI cannot build the UI project due to MRT/PRI tooling requirements.

```powershell
# Build with MSBuild (required for FastStart.UI)
& "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" FastStart.sln -p:Configuration=Debug -p:Platform=x64

# Or open in Visual Studio / Rider and build from IDE
```

For library projects only (Core, Data, Native):
```powershell
dotnet build src/FastStart.Core
dotnet build src/FastStart.Data
dotnet build src/FastStart.Native
```

## Run

After building, run the application:

```powershell
# From the output directory
.\src\FastStart.UI\bin\x64\Debug\net8.0-windows10.0.22621.0\win-x64\FastStart.UI.exe

# Or for Release builds
.\src\FastStart.UI\bin\x64\Release\net8.0-windows10.0.22621.0\win-x64\FastStart.UI.exe
```

Or run directly from Visual Studio / Rider using F5.

## Run Tests

```powershell
dotnet test tests/FastStart.Tests
```

## Run Benchmarks

```powershell
dotnet run -c Release --project tests/FastStart.Benchmarks
```

## Architecture

- **Unpackaged WinUI 3** - No MSIX packaging required, runs as standard exe
- **Self-contained Windows App SDK** - No runtime installation needed
- **EF Core + SQLite** - Local database for app index, pins, and recent launches
- **In-memory caching** - Thread-safe cache with `ReaderWriterLockSlim` for fast searches
- **Token-based search** - Prefix matching on tokenized app names for instant results
