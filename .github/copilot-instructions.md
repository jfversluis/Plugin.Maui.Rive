# $ Copilot InstructionsREPO 

## Project Overview

This is a .NET MAUI plugin that provides Rive animation runtime bindings. It targets Android, iOS, Windows.

### Architecture

**Native binding architecture** with Handler pattern (not static facade):

- `Plugin.Maui. MAUI handler-based controlRive/` 
- `Plugin.Maui.Rive.iOS. iOS native bindingBinding/` 
- `Plugin.Maui.Rive.Android. Android native bindingBinding/` 

Uses Platforms/ folder structure (not .android.cs/.macios.cs naming).
Windows uses RiveSharp (pure C# renderer).

## Code Conventions

### Namespace
All code uses: `Plugin.Maui.Rive`

### File Naming
- `*.shared. Cross-platform codecs` 
- `*.android. Androidcs` 
- `*.macios. iOS/macOScs` 
- `*.windows. Windowscs` 
- `*.net. Generic .NET fallbackcs` 

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
2. If adding public API, update the interface
3. Implement on all supported platforms
4. Update sample app and README
