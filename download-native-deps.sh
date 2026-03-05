#!/bin/bash
# Downloads native dependencies required to build Plugin.Maui.Rive.
# The xcframework and AAR files are too large for git and must be downloaded separately.

set -euo pipefail

RIVE_IOS_VERSION="6.16.0"
RIVE_ANDROID_VERSION="11.2.1"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== Downloading native dependencies for Plugin.Maui.Rive ==="

# --- iOS: RiveRuntime.xcframework ---
IOS_DIR="$SCRIPT_DIR/native/ios"
IOS_ZIP="$IOS_DIR/RiveRuntime.xcframework.zip"
IOS_FRAMEWORK="$IOS_DIR/RiveRuntime.xcframework"

if [ -d "$IOS_FRAMEWORK" ]; then
    echo "✅ iOS: RiveRuntime.xcframework already exists, skipping."
else
    echo "📦 iOS: Downloading RiveRuntime.xcframework v${RIVE_IOS_VERSION}..."
    mkdir -p "$IOS_DIR"
    curl -L -o "$IOS_ZIP" \
        "https://github.com/rive-app/rive-ios/releases/download/${RIVE_IOS_VERSION}/RiveRuntime.xcframework.zip"
    echo "📂 iOS: Extracting..."
    unzip -q -o "$IOS_ZIP" -d "$IOS_DIR"
    rm -f "$IOS_ZIP"
    echo "✅ iOS: RiveRuntime.xcframework ready."
fi

# --- Android: rive-android AAR ---
ANDROID_DIR="$SCRIPT_DIR/native/android"
ANDROID_AAR="$ANDROID_DIR/rive-android-${RIVE_ANDROID_VERSION}.aar"

if [ -f "$ANDROID_AAR" ]; then
    echo "✅ Android: rive-android AAR already exists, skipping."
else
    echo "📦 Android: Downloading rive-android v${RIVE_ANDROID_VERSION}..."
    mkdir -p "$ANDROID_DIR"
    curl -L -o "$ANDROID_AAR" \
        "https://repo1.maven.org/maven2/app/rive/rive-android/${RIVE_ANDROID_VERSION}/rive-android-${RIVE_ANDROID_VERSION}.aar"
    echo "✅ Android: rive-android AAR ready."
fi

echo ""
echo "=== All native dependencies downloaded ==="

# --- Windows: rive.dll (built from rive-cpp) ---
WINDOWS_DIR="$SCRIPT_DIR/src/Plugin.Maui.Rive/Platforms/Windows/Native"
WINDOWS_DLL="$WINDOWS_DIR/rive.dll"

if [ -f "$WINDOWS_DLL" ]; then
    echo "✅ Windows: rive.dll already exists, skipping."
else
    echo "📦 Windows: Building rive.dll from rive-cpp source..."

    RIVE_CPP_DIR="$SCRIPT_DIR/native/rive-cpp"
    RIVE_SHARP_DIR="$SCRIPT_DIR/native/rive-sharp-interop"

    # Clone rive-cpp if not present
    if [ ! -d "$RIVE_CPP_DIR" ]; then
        echo "   Cloning rive-cpp..."
        git clone --depth 1 https://github.com/rive-app/rive-cpp.git "$RIVE_CPP_DIR"
    fi

    # Clone rive-sharp interop file if not present
    if [ ! -f "$RIVE_SHARP_DIR/RiveSharpInterop.cpp" ]; then
        echo "   Cloning rive-sharp for interop..."
        mkdir -p "$RIVE_SHARP_DIR"
        curl -L -o "$RIVE_SHARP_DIR/RiveSharpInterop.cpp" \
            "https://raw.githubusercontent.com/rive-app/rive-sharp/main/native/RiveSharpInterop.cpp"
        curl -L -o "$RIVE_SHARP_DIR/premake5.lua" \
            "https://raw.githubusercontent.com/rive-app/rive-sharp/main/native/premake5.lua"
    fi

    # Download premake5
    PREMAKE_DIR="$SCRIPT_DIR/native/premake5"
    if [ ! -f "$PREMAKE_DIR/premake5.exe" ]; then
        echo "   Downloading premake5..."
        mkdir -p "$PREMAKE_DIR"
        curl -L -o "$PREMAKE_DIR/premake5.zip" \
            "https://github.com/premake/premake-core/releases/download/v5.0.0-beta4/premake-5.0.0-beta4-windows.zip"
        unzip -q -o "$PREMAKE_DIR/premake5.zip" -d "$PREMAKE_DIR"
        rm -f "$PREMAKE_DIR/premake5.zip"
    fi

    # Generate vcxproj and build
    echo "   Generating build files..."
    cd "$RIVE_SHARP_DIR"

    # Create premake5.lua that points to the cloned rive-cpp
    cat > premake5.lua << 'PREMAKE_EOF'
workspace "rive-cpp"
configurations {"Debug", "Release"}
platforms {"x64"}

RIVE_RUNTIME_DIR = "../rive-cpp"

project "rive"
    kind "SharedLib"
    language "C++"
    cppdialect "C++17"
    targetdir "bin/%{cfg.platform}/%{cfg.buildcfg}"
    objdir "obj/%{cfg.platform}/%{cfg.buildcfg}"
    staticruntime "off"
    includedirs {
        RIVE_RUNTIME_DIR .. "/include",
    }
    files {
        RIVE_RUNTIME_DIR .. "/src/**.cpp",
        "RiveSharpInterop.cpp"
    }
    filter "configurations:Release"
        defines {"RELEASE", "NDEBUG"}
        optimize "Size"
    filter "platforms:x64"
        architecture "x64"
        toolset "clang"
    filter {}
PREMAKE_EOF

    "$PREMAKE_DIR/premake5.exe" vs2022

    echo "   Building rive.dll (this may take a few minutes)..."
    # Find MSBuild
    MSBUILD=$(vswhere -latest -prerelease -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | head -1)
    if [ -z "$MSBUILD" ]; then
        MSBUILD="MSBuild.exe"
    fi
    "$MSBUILD" rive.vcxproj /p:Configuration=Release /p:Platform=x64 /v:minimal

    mkdir -p "$WINDOWS_DIR"
    cp "bin/x64/Release/rive.dll" "$WINDOWS_DLL"
    echo "✅ Windows: rive.dll built and copied."
    cd "$SCRIPT_DIR"
fi

echo ""
echo "=== All native dependencies ready ==="
echo "You can now build the solution with: dotnet build"
