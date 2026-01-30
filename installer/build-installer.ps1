# FastStart Installer Build Script
# Requires: Inno Setup 6 (https://jrsoftware.org/isinfo.php)
# Run from the repository root or installer directory

param(
    [switch]$SkipBuild,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$InstallerDir = $PSScriptRoot
$PublishDir = Join-Path $RepoRoot "publish"
$DistDir = Join-Path $RepoRoot "dist"

Write-Host "FastStart Installer Build Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build and publish the UI project
if (-not $SkipBuild) {
    Write-Host "[1/4] Building FastStart.UI ($Configuration)..." -ForegroundColor Yellow

    # Clean previous publish
    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }

    # Find MSBuild
    $MSBuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    if (-not (Test-Path $MSBuildPath)) {
        $MSBuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    }
    if (-not (Test-Path $MSBuildPath)) {
        $MSBuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    }
    if (-not (Test-Path $MSBuildPath)) {
        $MSBuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    }

    if (-not (Test-Path $MSBuildPath)) {
        Write-Error "MSBuild not found. Please install Visual Studio 2022 Build Tools."
        exit 1
    }

    Write-Host "Using MSBuild: $MSBuildPath" -ForegroundColor Gray

    # Build and publish
    $UIProject = Join-Path $RepoRoot "src\FastStart.UI\FastStart.UI.csproj"
    & $MSBuildPath $UIProject `
        -t:Publish `
        -p:Configuration=$Configuration `
        -p:PublishDir=$PublishDir `
        -p:PublishSingleFile=false `
        -p:SelfContained=false `
        -p:RuntimeIdentifier=win-x64 `
        -restore `
        -v:minimal

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        exit 1
    }

    Write-Host "[1/4] Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "[1/4] Skipping build (using existing publish output)" -ForegroundColor Gray
}

# Step 2: Copy icon to publish directory
Write-Host "[2/4] Copying assets..." -ForegroundColor Yellow
$IconSource = Join-Path $RepoRoot "assets\FastStart.ico"
if (Test-Path $IconSource) {
    Copy-Item $IconSource -Destination $PublishDir -Force
    Write-Host "       Icon copied to publish directory" -ForegroundColor Gray
}

# Step 3: Create dist directory
Write-Host "[3/4] Preparing output directory..." -ForegroundColor Yellow
if (-not (Test-Path $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir | Out-Null
}

# Step 4: Compile installer with Inno Setup
Write-Host "[4/4] Compiling installer with Inno Setup..." -ForegroundColor Yellow

# Find Inno Setup compiler
$InnoCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $InnoCompiler)) {
    $InnoCompiler = "C:\Program Files\Inno Setup 6\ISCC.exe"
}

if (-not (Test-Path $InnoCompiler)) {
    Write-Error @"
Inno Setup 6 not found. Please install it from:
https://jrsoftware.org/isdl.php

After installation, run this script again.
"@
    exit 1
}

$IssFile = Join-Path $InstallerDir "FastStart.iss"
& $InnoCompiler $IssFile

if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup compilation failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Installer built successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Output: $DistDir\FastStartSetup-1.0.0.exe" -ForegroundColor White
Write-Host ""
