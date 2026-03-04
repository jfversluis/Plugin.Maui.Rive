namespace Plugin.Maui.Rive.Sample;

public partial class IntrospectionPage : ContentPage
{
    public IntrospectionPage()
    {
        InitializeComponent();
    }

    void OnVehiclesClicked(object? s, EventArgs e) { riveView.ResourceName = "vehicles"; ClearResults(); }
    void OnSkillsClicked(object? s, EventArgs e) { riveView.ResourceName = "skills"; ClearResults(); }
    void OnBearClicked(object? s, EventArgs e) { riveView.ResourceName = "bear"; ClearResults(); }
    void OnExplorerClicked(object? s, EventArgs e) { riveView.ResourceName = "explorer"; ClearResults(); }

    void OnQueryAllClicked(object? s, EventArgs e)
    {
        // Small delay to ensure the animation is loaded
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
        {
            var artboards = riveView.GetArtboardNames();
            artboardNamesLabel.Text = artboards.Length > 0 ? string.Join(", ", artboards) : "(none found)";
            artboardNamesLabel.TextColor = artboards.Length > 0 ? Colors.Green : Colors.Gray;

            var animations = riveView.GetAnimationNames();
            animationNamesLabel.Text = animations.Length > 0 ? string.Join(", ", animations) : "(not available on this platform)";
            animationNamesLabel.TextColor = animations.Length > 0 ? Colors.Green : Colors.Orange;

            var stateMachines = riveView.GetStateMachineNames();
            stateMachineNamesLabel.Text = stateMachines.Length > 0 ? string.Join(", ", stateMachines) : "(not available on this platform)";
            stateMachineNamesLabel.TextColor = stateMachines.Length > 0 ? Colors.Green : Colors.Orange;

            var inputs = riveView.GetStateMachineInputNames();
            inputNamesLabel.Text = inputs.Length > 0 ? string.Join(", ", inputs) : "(not available on this platform)";
            inputNamesLabel.TextColor = inputs.Length > 0 ? Colors.Green : Colors.Orange;
        });
    }

    void OnIsPlayingClicked(object? s, EventArgs e)
    {
        isPlayingLabel.Text = $"IsPlaying: {riveView.IsPlaying}";
        isPlayingLabel.TextColor = riveView.IsPlaying ? Colors.Green : Colors.Red;
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
}
