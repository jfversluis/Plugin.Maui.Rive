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

/// <summary>
/// Represents a boolean input for a Rive state machine.
/// </summary>
public record RiveBoolInput(string Name, bool Value);

/// <summary>
/// Represents a number input for a Rive state machine.
/// </summary>
public record RiveNumberInput(string Name, float Value);
