namespace Plugin.Maui.Rive;

/// <summary>
/// A cross-platform view that displays and controls Rive animations.
/// </summary>
public class RiveAnimationView : View, IRiveAnimationView
{
    /// <summary>
    /// Bindable property for the resource name of the .riv file.
    /// </summary>
    public static readonly BindableProperty ResourceNameProperty =
        BindableProperty.Create(nameof(ResourceName), typeof(string), typeof(RiveAnimationView), default(string));

    /// <summary>
    /// Bindable property for the URL of the .riv file.
    /// </summary>
    public static readonly BindableProperty UrlProperty =
        BindableProperty.Create(nameof(Url), typeof(string), typeof(RiveAnimationView), default(string));

    /// <summary>
    /// Bindable property for the artboard name.
    /// </summary>
    public static readonly BindableProperty ArtboardNameProperty =
        BindableProperty.Create(nameof(ArtboardName), typeof(string), typeof(RiveAnimationView), default(string));

    /// <summary>
    /// Bindable property for the state machine name.
    /// </summary>
    public static readonly BindableProperty StateMachineNameProperty =
        BindableProperty.Create(nameof(StateMachineName), typeof(string), typeof(RiveAnimationView), default(string));

    /// <summary>
    /// Bindable property for the animation name.
    /// </summary>
    public static readonly BindableProperty AnimationNameProperty =
        BindableProperty.Create(nameof(AnimationName), typeof(string), typeof(RiveAnimationView), default(string));

    /// <summary>
    /// Bindable property for auto play.
    /// </summary>
    public static readonly BindableProperty AutoPlayProperty =
        BindableProperty.Create(nameof(AutoPlay), typeof(bool), typeof(RiveAnimationView), true);

    /// <summary>
    /// Bindable property for the fit mode.
    /// </summary>
    public static readonly BindableProperty FitProperty =
        BindableProperty.Create(nameof(Fit), typeof(RiveFitMode), typeof(RiveAnimationView), RiveFitMode.Contain);

    /// <summary>
    /// Bindable property for the alignment.
    /// </summary>
    public static readonly BindableProperty AlignmentProperty =
        BindableProperty.Create(nameof(RiveAlignment), typeof(RiveAlignmentMode), typeof(RiveAnimationView), RiveAlignmentMode.Center);

    /// <summary>
    /// The resource name of the .riv file in the app bundle (without extension).
    /// </summary>
    public string? ResourceName
    {
        get => (string?)GetValue(ResourceNameProperty);
        set => SetValue(ResourceNameProperty, value);
    }

    /// <summary>
    /// The URL of the .riv file to load from the web.
    /// </summary>
    public string? Url
    {
        get => (string?)GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    /// <summary>
    /// The artboard name. If null, uses the default artboard.
    /// </summary>
    public string? ArtboardName
    {
        get => (string?)GetValue(ArtboardNameProperty);
        set => SetValue(ArtboardNameProperty, value);
    }

    /// <summary>
    /// The state machine name. If null, uses the default state machine.
    /// </summary>
    public string? StateMachineName
    {
        get => (string?)GetValue(StateMachineNameProperty);
        set => SetValue(StateMachineNameProperty, value);
    }

    /// <summary>
    /// The animation name.
    /// </summary>
    public string? AnimationName
    {
        get => (string?)GetValue(AnimationNameProperty);
        set => SetValue(AnimationNameProperty, value);
    }

    /// <summary>
    /// Whether the animation should auto-play when loaded.
    /// </summary>
    public bool AutoPlay
    {
        get => (bool)GetValue(AutoPlayProperty);
        set => SetValue(AutoPlayProperty, value);
    }

    /// <summary>
    /// How the animation should fit within the view.
    /// </summary>
    public RiveFitMode Fit
    {
        get => (RiveFitMode)GetValue(FitProperty);
        set => SetValue(FitProperty, value);
    }

    /// <summary>
    /// How the animation should be aligned within the view.
    /// </summary>
    public RiveAlignmentMode RiveAlignment
    {
        get => (RiveAlignmentMode)GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    /// <summary>
    /// Play the animation.
    /// </summary>
    public void Play() => Handler?.Invoke(nameof(Play));

    /// <summary>
    /// Pause the animation.
    /// </summary>
    public void Pause() => Handler?.Invoke(nameof(Pause));

    /// <summary>
    /// Stop the animation.
    /// </summary>
    public void Stop() => Handler?.Invoke(nameof(Stop));

    /// <summary>
    /// Reset the animation.
    /// </summary>
    public void Reset() => Handler?.Invoke(nameof(Reset));

    /// <summary>
    /// Fire a trigger input on the active state machine.
    /// </summary>
    public void FireTrigger(string inputName)
        => Handler?.Invoke(nameof(FireTrigger), inputName);

    /// <summary>
    /// Set a boolean input on the active state machine.
    /// </summary>
    public void SetBoolInput(string inputName, bool value)
        => Handler?.Invoke(nameof(SetBoolInput), new RiveBoolInput(inputName, value));

    /// <summary>
    /// Set a number input on the active state machine.
    /// </summary>
    public void SetNumberInput(string inputName, float value)
        => Handler?.Invoke(nameof(SetNumberInput), new RiveNumberInput(inputName, value));
}
