# Plugin.Maui.Rive - Copilot Instructions

## Project Overview

This is a .NET MAUI plugin that provides Rive animation runtime bindings. It targets Android, iOS, and Windows.

## Architecture

**Native binding architecture** with Handler pattern (not static facade):

- `Plugin.Maui.Rive/` - MAUI handler-based control
- `Plugin.Maui.Rive.iOS.Binding/` - iOS native binding
- `Plugin.Maui.Rive.Android.Binding/` - Android native binding

Uses `Platforms/` folder structure (not the `.android.cs`/`.macios.cs` naming convention).
Windows uses RiveSharp (pure C# renderer).

## Code Conventions

### Namespace
All code uses: `Plugin.Maui.Rive`

### File Naming
- `*.shared.cs` - Cross-platform code
- `*.android.cs` - Android-specific code
- `*.macios.cs` - iOS/macOS-specific code
- `*.windows.cs` - Windows-specific code
- `*.net.cs` - Generic .NET fallback

### Standards
- File-scoped namespaces
- `camelCase` for private fields, `PascalCase` for public
- XML docs required on all public APIs
- Null-conditional operators for platform interop

## Building

```bash
dotnet build src/Plugin.Maui.Rive/Plugin.Maui.Rive.csproj -c Release
```

## When Making Changes
1. Ensure the plugin builds on all target platforms
2. If adding public API, update the handler and platform implementations
3. Update sample app and README
