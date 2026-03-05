namespace Plugin.Maui.Rive.Sample;

public partial class DeveloperToolsPage : ContentPage
{
    public DeveloperToolsPage()
    {
        InitializeComponent();
    }

    void OnAnimationClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string name)
        {
            riveView.Url = null;
            riveView.ResourceName = name;
            ClearResults();
        }
    }

    // ── Introspection ──
    void OnQueryAllClicked(object? sender, EventArgs e)
    {
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
        {
            var artboards = riveView.GetArtboardNames();
            artboardNamesLabel.Text = artboards.Length > 0 ? string.Join(", ", artboards) : "(none)";
            artboardNamesLabel.TextColor = artboards.Length > 0 ? Colors.Green : Colors.Gray;

            var animations = riveView.GetAnimationNames();
            animationNamesLabel.Text = animations.Length > 0 ? string.Join(", ", animations) : "(n/a)";
            animationNamesLabel.TextColor = animations.Length > 0 ? Colors.Green : Colors.Orange;

            var stateMachines = riveView.GetStateMachineNames();
            stateMachineNamesLabel.Text = stateMachines.Length > 0 ? string.Join(", ", stateMachines) : "(n/a)";
            stateMachineNamesLabel.TextColor = stateMachines.Length > 0 ? Colors.Green : Colors.Orange;

            var inputs = riveView.GetStateMachineInputs();
            if (inputs.Length > 0)
            {
                inputNamesLabel.Text = string.Join(", ", inputs.Select(i => $"{i.Name} ({i.Type})"));
                inputNamesLabel.TextColor = Colors.Green;
            }
            else
            {
                inputNamesLabel.Text = "(n/a)";
                inputNamesLabel.TextColor = Colors.Orange;
            }

            var playing = riveView.QueryIsPlaying();
            isPlayingLabel.Text = $"IsPlaying: {playing}";
            isPlayingLabel.TextColor = playing ? Colors.Green : Colors.Red;
        });
    }

    void ClearResults()
    {
        artboardNamesLabel.Text = "(tap Query All)";
        artboardNamesLabel.TextColor = Colors.Gray;
        animationNamesLabel.Text = "—";
        animationNamesLabel.TextColor = Colors.Gray;
        stateMachineNamesLabel.Text = "—";
        stateMachineNamesLabel.TextColor = Colors.Gray;
        inputNamesLabel.Text = "—";
        inputNamesLabel.TextColor = Colors.Gray;
        isPlayingLabel.Text = "";
    }

    // ── Fit & Alignment ──
    void OnFitClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        if (Enum.TryParse<RiveFitMode>(btn.Text, out var fit))
        {
            riveView.Fit = fit;
            UpdateSettingsLabel();
        }
    }

    void OnAlignClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string param) return;
        if (Enum.TryParse<RiveAlignmentMode>(param, out var align))
        {
            riveView.RiveAlignment = align;
            UpdateSettingsLabel();
        }
    }

    void OnScaleChanged(object? sender, ValueChangedEventArgs e)
    {
        riveView.LayoutScaleFactor = (float)e.NewValue;
        scaleLabel.Text = $"{e.NewValue:F1}x";
    }

    void UpdateSettingsLabel()
    {
        settingsLabel.Text = $"Fit: {riveView.Fit}  •  Align: {riveView.RiveAlignment}";
    }

    // ── URL Loading ──
    void OnLoadUrlClicked(object? sender, EventArgs e)
    {
        var url = urlEntry.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            loadStatusLabel.Text = "❌ Please enter a URL.";
            return;
        }

        riveView.ResourceName = null;
        riveView.Url = url;
        loadStatusLabel.Text = $"[{DateTime.Now:HH:mm:ss}] ⏳ Loading from URL...";
    }
}
