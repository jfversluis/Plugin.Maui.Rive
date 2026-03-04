# Plugin.Maui.Rive

[![NuGet](https://img.shields.io/nuget/v/Plugin.Maui.Rive.svg)](https://www.nuget.org/packages/Plugin.Maui.Rive)

[Rive](https://rive.app/) animation runtime bindings for .NET MAUI. Display and control interactive Rive animations in your cross-platform .NET MAUI applications.

## Supported Platforms

| Platform | Status |
|----------|--------|
| iOS 14+  | ✅ Working |
| Android 21+ | ✅ Working |

## Getting Started

### 1. Download Native Dependencies

The native Rive SDKs are too large for git. Run the download script first:

```bash
./download-native-deps.sh
```

This downloads:
- **iOS**: `RiveRuntime.xcframework` v6.15.2 (~90MB)
- **Android**: `rive-android` v11.2.1 AAR (~10MB)

### 2. Register Rive in MauiProgram.cs

```csharp
using Plugin.Maui.Rive;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseRive();  // Register Rive handler

    return builder.Build();
}
```

### 3. Add a .riv File

Place your `.riv` file in `Resources/Raw/` (e.g., `Resources/Raw/animation.riv`).

### 4. Use RiveAnimationView in XAML

```xml
<ContentPage xmlns:rive="clr-namespace:Plugin.Maui.Rive;assembly=Plugin.Maui.Rive">

    <rive:RiveAnimationView
        ResourceName="animation"
        AutoPlay="True"
        Fit="Contain"
        HeightRequest="400" />

</ContentPage>
```

## API Reference

### RiveAnimationView Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ResourceName` | `string?` | `null` | Name of the .riv file (without extension) in Resources/Raw |
| `AutoPlay` | `bool` | `true` | Whether the animation plays automatically |
| `Fit` | `RiveFitMode` | `Contain` | How the animation fits within the view |
| `RiveAlignment` | `RiveAlignmentMode` | `Center` | Alignment of the animation within the view |
| `ArtboardName` | `string?` | `null` | Specific artboard name (default artboard if null) |
| `StateMachineName` | `string?` | `null` | State machine to use |
| `AnimationName` | `string?` | `null` | Specific animation name |

### RiveAnimationView Methods

```csharp
riveView.Play();       // Start/resume playback
riveView.Pause();      // Pause playback
riveView.Stop();       // Stop playback
riveView.Reset();      // Reset to initial state

// State machine inputs
riveView.FireTrigger("triggerName");
riveView.SetBoolInput("inputName", true);
riveView.SetNumberInput("inputName", 42.0);
```

### Enums

**RiveFitMode**: `Fill`, `Contain`, `Cover`, `FitWidth`, `FitHeight`, `ScaleDown`, `NoFit`, `Layout`

**RiveAlignmentMode**: `TopLeft`, `TopCenter`, `TopRight`, `CenterLeft`, `Center`, `CenterRight`, `BottomLeft`, `BottomCenter`, `BottomRight`

## Building from Source

```bash
# Clone the repository
git clone https://github.com/jfversluis/Plugin.Maui.Rive.git
cd Plugin.Maui.Rive

# Download native dependencies
./download-native-deps.sh

# Build
dotnet build src/Plugin.Maui.Rive/Plugin.Maui.Rive.csproj
```

## Native SDK Versions

- **iOS**: [RiveRuntime](https://github.com/rive-app/rive-ios) v6.15.2
- **Android**: [rive-android](https://github.com/rive-app/rive-android) v11.2.1

## License

MIT License - see [LICENSE](LICENSE) for details.