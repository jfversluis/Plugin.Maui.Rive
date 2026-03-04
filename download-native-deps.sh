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
echo "You can now build the solution with: dotnet build"
