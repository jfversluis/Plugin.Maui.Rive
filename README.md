# Plugin.Maui.Rive

[![NuGet](https://img.shields.io/nuget/v/Plugin.Maui.Rive.svg)](https://www.nuget.org/packages/Plugin.Maui.Rive)

[Rive](https://rive.app/) animation runtime bindings for .NET MAUI. Display and control interactive Rive animations in your cross-platform .NET MAUI applications.

## Supported Platforms

| Platform | Version | Status |
|----------|---------|--------|
| iOS | 14+ | ✅ Supported |
| Android | 21+ | ✅ Supported |
| Windows | 10.0.19041+ | ✅ Supported |

## Getting Started

### 1. Install the NuGet package

```bash
dotnet add package Plugin.Maui.Rive
```

Or search for `Plugin.Maui.Rive` in the NuGet Package Manager in Visual Studio.

### 2. Initialize in MauiProgram.cs

```csharp
using Plugin.Maui.Rive;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseRive();

    return builder.Build();
}
```

### 3. Add a .riv file

Place your `.riv` file in the `Resources/Raw/` folder of your project. You can download free animations from the [Rive Community](https://rive.app/community/).

Some example `.riv` files are included in the [sample app](samples/Plugin.Maui.Rive.Sample/Resources/Raw/).

### 4. Use in XAML

```xml
<ContentPage xmlns:rive="clr-namespace:Plugin.Maui.Rive;assembly=Plugin.Maui.Rive">

    <rive:RiveAnimationView
        ResourceName="animation"
        AutoPlay="True"
        Fit="Contain"
        HeightRequest="400" />

</ContentPage>
```

### 5. Or use in code

```csharp
var riveView = new RiveAnimationView
{
    ResourceName = "animation",
    AutoPlay = true,
    Fit = RiveFitMode.Contain,
    HeightRequest = 400
};
```

You can also load animations from a URL:

```csharp
var riveView = new RiveAnimationView
{
    Url = "https://cdn.rive.app/animations/vehicles.riv",
    AutoPlay = true
};
```

## API Reference

### RiveAnimationView Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ResourceName` | `string?` | `null` | Name of the .riv file (without extension) in Resources/Raw |
| `Url` | `string?` | `null` | URL of a .riv file to load from the web |
| `AutoPlay` | `bool` | `true` | Whether the animation plays automatically |
| `Fit` | `RiveFitMode` | `Contain` | How the animation fits within the view |
| `RiveAlignment` | `RiveAlignmentMode` | `Center` | Alignment of the animation within the view |
| `ArtboardName` | `string?` | `null` | Specific artboard name (default artboard if null) |
| `StateMachineName` | `string?` | `null` | State machine to use |
| `AnimationName` | `string?` | `null` | Specific animation name |
| `IsPlaying` | `bool` | `false` | Whether the animation is currently playing (updated automatically) |
| `LayoutScaleFactor` | `float` | `1.0` | Scale factor for Fit.Layout mode (Android only) |

### RiveAnimationView Methods

```csharp
// Playback control with optional parameters
riveView.Play();                                           // Play with defaults
riveView.Play("animationName");                            // Play specific animation
riveView.Play(loop: RiveLoopMode.OneShot);                 // Play once
riveView.Play(direction: RiveDirectionMode.Backwards);     // Play backwards
riveView.Pause();
riveView.Stop();
riveView.Reset();

// State machine inputs
riveView.FireTrigger("triggerName");
riveView.SetBoolInput("inputName", true);
riveView.SetNumberInput("inputName", 42.0f);

// Nested artboard inputs
riveView.FireTriggerAtPath("triggerName", "path/to/artboard");
riveView.SetBoolInputAtPath("inputName", true, "path/to/artboard");
riveView.SetNumberInputAtPath("inputName", 42.0f, "path/to/artboard");

// Text runs
string? text = riveView.GetTextRunValue("textRunName");
riveView.SetTextRunValue("textRunName", "new value");
string? nestedText = riveView.GetTextRunValueAtPath("textRunName", "path");
riveView.SetTextRunValueAtPath("textRunName", "new value", "path");

// Load from byte array
byte[] riveBytes = File.ReadAllBytes("animation.riv");
riveView.SetRiveBytes(riveBytes, artboardName: "MyArtboard");

// Introspection
string[] artboards = riveView.GetArtboardNames();
string[] animations = riveView.GetAnimationNames();
string[] stateMachines = riveView.GetStateMachineNames();
string[] inputs = riveView.GetStateMachineInputNames();
```

### Events

```csharp
riveView.PlaybackStarted += (s, e) => Console.WriteLine("Playing");
riveView.PlaybackPaused += (s, e) => Console.WriteLine("Paused");
riveView.PlaybackStopped += (s, e) => Console.WriteLine("Stopped");
riveView.PlaybackLooped += (s, e) => Console.WriteLine("Looped");
riveView.RiveEventReceived += (s, e) =>
{
    Console.WriteLine($"Event: {e.Name}, Delay: {e.Delay}");
    foreach (var prop in e.Properties)
        Console.WriteLine($"  {prop.Key} = {prop.Value}");
};
riveView.StateChanged += (s, e) =>
    Console.WriteLine($"State changed: {e.StateMachineName} -> {e.StateName}");
```

### Enums

**RiveFitMode**: `Fill`, `Contain`, `Cover`, `FitWidth`, `FitHeight`, `ScaleDown`, `NoFit`, `Layout`

**RiveAlignmentMode**: `TopLeft`, `TopCenter`, `TopRight`, `CenterLeft`, `Center`, `CenterRight`, `BottomLeft`, `BottomCenter`, `BottomRight`

**RiveLoopMode**: `OneShot`, `Loop`, `PingPong`, `Auto`

**RiveDirectionMode**: `Backwards`, `Forwards`, `Auto`

## Building from Source

If you want to contribute or build from source:

```bash
# Clone the repository
git clone https://github.com/jfversluis/Plugin.Maui.Rive.git
cd Plugin.Maui.Rive

# Download native dependencies (iOS xcframework, Android AAR)
./download-native-deps.sh

# Build
dotnet build src/Plugin.Maui.Rive.sln
```

> **Note:** The Windows native runtime (`rive.dll`) is included in the repository. iOS and Android native SDKs must be downloaded separately using the script above due to their size.

## Native SDK Versions

| Platform | SDK | Version |
|----------|-----|---------|
| iOS | [RiveRuntime](https://github.com/rive-app/rive-ios) | v6.16.0 |
| Android | [rive-android](https://github.com/rive-app/rive-android) | v11.2.1 |
| Windows | [rive-cpp](https://github.com/rive-app/rive-cpp) + [SkiaSharp](https://github.com/mono/SkiaSharp) | latest |

## License

MIT License - see [LICENSE](LICENSE) for details.