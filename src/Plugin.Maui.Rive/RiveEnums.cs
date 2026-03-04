namespace Plugin.Maui.Rive;

/// <summary>
/// How the Rive animation content should fit within its view.
/// </summary>
public enum RiveFitMode
{
    Fill,
    Contain,
    Cover,
    FitHeight,
    FitWidth,
    ScaleDown,
    NoFit,
    Layout
}

/// <summary>
/// How the Rive animation content should be aligned within its view.
/// </summary>
public enum RiveAlignmentMode
{
    TopLeft,
    TopCenter,
    TopRight,
    CenterLeft,
    Center,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

/// <summary>Loop mode for playback.</summary>
public enum RiveLoopMode
{
    OneShot,
    Loop,
    PingPong,
    Auto
}

/// <summary>Playback direction.</summary>
public enum RiveDirectionMode
{
    Backwards,
    Forwards,
    Auto
}

/// <summary>Arguments for Play() command.</summary>
public record RivePlayArgs(string? AnimationName, RiveLoopMode Loop, RiveDirectionMode Direction);

/// <summary>Represents a boolean input for a Rive state machine.</summary>
public record RiveBoolInput(string Name, bool Value);

/// <summary>Represents a number input for a Rive state machine.</summary>
public record RiveNumberInput(string Name, float Value);

/// <summary>Represents a text run value to set.</summary>
public record RiveTextRun(string TextRunName, string TextValue, string? Path = null);

/// <summary>Represents a trigger input at a nested artboard path.</summary>
public record RiveTriggerAtPath(string InputName, string Path);

/// <summary>Represents a boolean input at a nested artboard path.</summary>
public record RiveBoolInputAtPath(string InputName, bool Value, string Path);

/// <summary>Represents a number input at a nested artboard path.</summary>
public record RiveNumberInputAtPath(string InputName, float Value, string Path);

/// <summary>Event args for Rive events fired from the state machine.</summary>
public class RiveEventReceivedEventArgs : EventArgs
{
    public string Name { get; }
    public float Delay { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }

    public RiveEventReceivedEventArgs(string name, float delay = 0, IReadOnlyDictionary<string, object>? properties = null)
    {
        Name = name;
        Delay = delay;
        Properties = properties ?? new Dictionary<string, object>();
    }
}

/// <summary>Event args for playback state changes.</summary>
public class RivePlaybackEventArgs : EventArgs
{
    public string? AnimationName { get; }

    public RivePlaybackEventArgs(string? animationName = null)
    {
        AnimationName = animationName;
    }
}
