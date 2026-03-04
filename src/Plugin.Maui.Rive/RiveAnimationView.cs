namespace Plugin.Maui.Rive;

/// <summary>
/// A cross-platform view that displays and controls Rive animations.
/// </summary>
public class RiveAnimationView : View, IRiveAnimationView
{
    public static readonly BindableProperty ResourceNameProperty =
        BindableProperty.Create(nameof(ResourceName), typeof(string), typeof(RiveAnimationView), default(string));

    public static readonly BindableProperty UrlProperty =
        BindableProperty.Create(nameof(Url), typeof(string), typeof(RiveAnimationView), default(string));

    public static readonly BindableProperty ArtboardNameProperty =
        BindableProperty.Create(nameof(ArtboardName), typeof(string), typeof(RiveAnimationView), default(string));

    public static readonly BindableProperty StateMachineNameProperty =
        BindableProperty.Create(nameof(StateMachineName), typeof(string), typeof(RiveAnimationView), default(string));

    public static readonly BindableProperty AnimationNameProperty =
        BindableProperty.Create(nameof(AnimationName), typeof(string), typeof(RiveAnimationView), default(string));

    public static readonly BindableProperty AutoPlayProperty =
        BindableProperty.Create(nameof(AutoPlay), typeof(bool), typeof(RiveAnimationView), true);

    public static readonly BindableProperty FitProperty =
        BindableProperty.Create(nameof(Fit), typeof(RiveFitMode), typeof(RiveAnimationView), RiveFitMode.Contain);

    public static readonly BindableProperty AlignmentProperty =
        BindableProperty.Create(nameof(RiveAlignment), typeof(RiveAlignmentMode), typeof(RiveAnimationView), RiveAlignmentMode.Center);

    public static readonly BindableProperty IsPlayingProperty =
        BindableProperty.Create(nameof(IsPlaying), typeof(bool), typeof(RiveAnimationView), false);

    public string? ResourceName
    {
        get => (string?)GetValue(ResourceNameProperty);
        set => SetValue(ResourceNameProperty, value);
    }

    public string? Url
    {
        get => (string?)GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    public string? ArtboardName
    {
        get => (string?)GetValue(ArtboardNameProperty);
        set => SetValue(ArtboardNameProperty, value);
    }

    public string? StateMachineName
    {
        get => (string?)GetValue(StateMachineNameProperty);
        set => SetValue(StateMachineNameProperty, value);
    }

    public string? AnimationName
    {
        get => (string?)GetValue(AnimationNameProperty);
        set => SetValue(AnimationNameProperty, value);
    }

    public bool AutoPlay
    {
        get => (bool)GetValue(AutoPlayProperty);
        set => SetValue(AutoPlayProperty, value);
    }

    public RiveFitMode Fit
    {
        get => (RiveFitMode)GetValue(FitProperty);
        set => SetValue(FitProperty, value);
    }

    public RiveAlignmentMode RiveAlignment
    {
        get => (RiveAlignmentMode)GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    /// <summary>Whether the animation is currently playing.</summary>
    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    // --- Events ---

    /// <inheritdoc />
    public event EventHandler<RiveEventReceivedEventArgs>? RiveEventReceived;

    /// <inheritdoc />
    public event EventHandler<RivePlaybackEventArgs>? PlaybackStarted;

    /// <inheritdoc />
    public event EventHandler<RivePlaybackEventArgs>? PlaybackPaused;

    /// <inheritdoc />
    public event EventHandler<RivePlaybackEventArgs>? PlaybackStopped;

    /// <inheritdoc />
    public event EventHandler<RivePlaybackEventArgs>? PlaybackLooped;

    public void OnRiveEventReceived(RiveEventReceivedEventArgs e) => RiveEventReceived?.Invoke(this, e);
    public void OnPlaybackStarted(RivePlaybackEventArgs e) { IsPlaying = true; PlaybackStarted?.Invoke(this, e); }
    public void OnPlaybackPaused(RivePlaybackEventArgs e) { IsPlaying = false; PlaybackPaused?.Invoke(this, e); }
    public void OnPlaybackStopped(RivePlaybackEventArgs e) { IsPlaying = false; PlaybackStopped?.Invoke(this, e); }
    public void OnPlaybackLooped(RivePlaybackEventArgs e) => PlaybackLooped?.Invoke(this, e);

    // --- Playback ---

    /// <summary>Play the animation with optional name, loop mode, and direction.</summary>
    public void Play(string? animationName = null, RiveLoopMode loop = RiveLoopMode.Auto, RiveDirectionMode direction = RiveDirectionMode.Auto)
        => Handler?.Invoke(nameof(Play), new RivePlayArgs(animationName, loop, direction));

    /// <summary>Pause the animation.</summary>
    public void Pause() => Handler?.Invoke(nameof(Pause));

    /// <summary>Stop the animation.</summary>
    public void Stop() => Handler?.Invoke(nameof(Stop));

    /// <summary>Reset the animation.</summary>
    public void Reset() => Handler?.Invoke(nameof(Reset));

    // --- State Machine Inputs ---

    /// <summary>Fire a trigger input on the active state machine.</summary>
    public void FireTrigger(string inputName)
        => Handler?.Invoke(nameof(FireTrigger), inputName);

    /// <summary>Set a boolean input on the active state machine.</summary>
    public void SetBoolInput(string inputName, bool value)
        => Handler?.Invoke(nameof(SetBoolInput), new RiveBoolInput(inputName, value));

    /// <summary>Set a number input on the active state machine.</summary>
    public void SetNumberInput(string inputName, float value)
        => Handler?.Invoke(nameof(SetNumberInput), new RiveNumberInput(inputName, value));

    // --- Nested Artboard Inputs ---

    /// <summary>Fire a trigger at a nested artboard path.</summary>
    public void FireTriggerAtPath(string inputName, string path)
        => Handler?.Invoke(nameof(FireTriggerAtPath), new RiveTriggerAtPath(inputName, path));

    /// <summary>Set a boolean input at a nested artboard path.</summary>
    public void SetBoolInputAtPath(string inputName, bool value, string path)
        => Handler?.Invoke(nameof(SetBoolInputAtPath), new RiveBoolInputAtPath(inputName, value, path));

    /// <summary>Set a number input at a nested artboard path.</summary>
    public void SetNumberInputAtPath(string inputName, float value, string path)
        => Handler?.Invoke(nameof(SetNumberInputAtPath), new RiveNumberInputAtPath(inputName, value, path));

    // --- Text Runs ---

    /// <summary>Get a text run value by name.</summary>
    public string? GetTextRunValue(string textRunName)
    {
        if (Handler is RiveAnimationViewHandler h)
            return h.GetTextRunValue(textRunName);
        return null;
    }

    /// <summary>Set a text run value by name.</summary>
    public void SetTextRunValue(string textRunName, string textValue)
        => Handler?.Invoke(nameof(SetTextRunValue), new RiveTextRun(textRunName, textValue));

    /// <summary>Get a text run value at a nested artboard path.</summary>
    public string? GetTextRunValueAtPath(string textRunName, string path)
    {
        if (Handler is RiveAnimationViewHandler h)
            return h.GetTextRunValueAtPath(textRunName, path);
        return null;
    }

    /// <summary>Set a text run value at a nested artboard path.</summary>
    public void SetTextRunValueAtPath(string textRunName, string textValue, string path)
        => Handler?.Invoke(nameof(SetTextRunValueAtPath), new RiveTextRun(textRunName, textValue, path));
}
