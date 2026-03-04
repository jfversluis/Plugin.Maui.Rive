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
    bool IsPlaying { get; set; }

    /// <summary>Raised when a Rive event is fired from the state machine.</summary>
    event EventHandler<RiveEventReceivedEventArgs>? RiveEventReceived;

    /// <summary>Raised when playback starts or resumes.</summary>
    event EventHandler<RivePlaybackEventArgs>? PlaybackStarted;

    /// <summary>Raised when playback is paused.</summary>
    event EventHandler<RivePlaybackEventArgs>? PlaybackPaused;

    /// <summary>Raised when playback is stopped.</summary>
    event EventHandler<RivePlaybackEventArgs>? PlaybackStopped;

    /// <summary>Raised when an animation loops.</summary>
    event EventHandler<RivePlaybackEventArgs>? PlaybackLooped;

    void OnRiveEventReceived(RiveEventReceivedEventArgs e);
    void OnPlaybackStarted(RivePlaybackEventArgs e);
    void OnPlaybackPaused(RivePlaybackEventArgs e);
    void OnPlaybackStopped(RivePlaybackEventArgs e);
    void OnPlaybackLooped(RivePlaybackEventArgs e);
}
