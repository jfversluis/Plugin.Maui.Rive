<!-- 
Everything in here is of course optional. If you want to add/remove something, absolutely do so as you see fit.
This example README has some dummy APIs you'll need to replace and only acts as a placeholder for some inspiration that you can fill in with your own functionalities.
-->
<!-- 
NuGet.org allows only images from certain domains. Complete list is here: https://learn.microsoft.com/nuget/nuget-org/package-readme-on-nuget-org#allowed-domains-for-images-and-badges.
In case of GitHub there is required a raw URI of icon file - direct link to github.com domain is not permitted.
(Tip: to obtain raw URI, open the .png image file on GitHub page, click right mouse button on image and then select 'Open image in new tab')
-->
![nuget.png](https://raw.githubusercontent.com/jfversluis/Plugin.Maui.Rive/main/nuget.png)
# Plugin.Maui.Rive

`Plugin.Maui.Rive` provides the ability to do this amazing thing in your .NET MAUI application.

## Install Plugin

[![NuGet](https://img.shields.io/nuget/v/Plugin.Maui.Rive.svg?label=NuGet)](https://www.nuget.org/packages/Plugin.Maui.Rive/)

Available on [NuGet](http://www.nuget.org/packages/Plugin.Maui.Rive).

Install with the dotnet CLI: `dotnet add package Plugin.Maui.Rive`, or through the NuGet Package Manager in Visual Studio.

### Supported Platforms

| Platform | Minimum Version Supported |
|----------|---------------------------|
| iOS      | 11+                       |
| macOS    | 10.15+                    |
| Android  | 5.0 (API 21)              |
| Windows  | 11 and 10 version 1809+   |

## API Usage

`Plugin.Maui.Rive` provides the `Rive` class that has a single property `Property` that you can get or set.

You can either use it as a static class, e.g.: `Rive.Default.Property = 1` or with dependency injection: `builder.Services.AddSingleton<IRive>(Rive.Default);`

### Permissions

Before you can start using Rive, you will need to request the proper permissions on each platform.

#### iOS

No permissions are needed for iOS.

#### Android

No permissions are needed for Android.

### Dependency Injection

You will first need to register the `Rive` with the `MauiAppBuilder` following the same pattern that the .NET MAUI Essentials libraries follow.

```csharp
builder.Services.AddSingleton(Rive.Default);
```

You can then enable your classes to depend on `IRive` as per the following example.

```csharp
public class RiveViewModel
{
    readonly IRive feature;

    public RiveViewModel(IRive feature)
    {
        this.feature = feature;
    }

    public void StartRive()
    {
        feature.ReadingChanged += (sender, reading) =>
        {
            Console.WriteLine(reading.Thing);
        };

        feature.Start();
    }
}
```

### Straight usage

Alternatively if you want to skip using the dependency injection approach you can use the `Rive.Default` property.

```csharp
public class RiveViewModel
{
    public void StartRive()
    {
        feature.ReadingChanged += (sender, reading) =>
        {
            Console.WriteLine(feature.Thing);
        };

        Rive.Default.Start();
    }
}
```

### Rive

Once you have created a `Rive` you can interact with it in the following ways:

#### Events

##### `ReadingChanged`

Occurs when feature reading changes.

#### Properties

##### `IsSupported`

Gets a value indicating whether reading the feature is supported on this device.

##### `IsMonitoring`

Gets a value indicating whether the feature is actively being monitored.

#### Methods

##### `Start()`

Start monitoring for changes to the feature.

##### `Stop()`

Stop monitoring for changes to the feature.

# Acknowledgements

This project could not have came to be without these projects and people, thank you! <3
