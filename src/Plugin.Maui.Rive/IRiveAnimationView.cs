namespace Plugin.Maui.Rive;

/// <summary>
/// Interface for the cross-platform RiveAnimationView.
/// </summary>
public interface IRiveAnimationView : IView
{
    string? ResourceName { get; }
    string? Url { get; }
    string? ArtboardName { get; }
    string? StateMachineName { get; }
    string? AnimationName { get; }
    bool AutoPlay { get; }
    RiveFitMode Fit { get; }
    RiveAlignmentMode RiveAlignment { get; }
}
