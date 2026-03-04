namespace Plugin.Maui.Rive.Sample;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    void OnPlayClicked(object? sender, EventArgs e) => riveView.Play();
    void OnPauseClicked(object? sender, EventArgs e) => riveView.Pause();
    void OnStopClicked(object? sender, EventArgs e) => riveView.Stop();
    void OnResetClicked(object? sender, EventArgs e) => riveView.Reset();
}
