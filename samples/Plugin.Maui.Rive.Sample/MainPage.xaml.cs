namespace Plugin.Maui.Rive.Sample;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        riveView.PlaybackStarted += (s, e) => UpdateStatus("Playing");
        riveView.PlaybackPaused += (s, e) => UpdateStatus("Paused");
        riveView.PlaybackStopped += (s, e) => UpdateStatus("Stopped");
        riveView.PlaybackLooped += (s, e) => UpdateStatus("Looped");
        riveView.RiveEventReceived += (s, e) => UpdateStatus($"Event: {e.Name}");
    }

    void OnPlayClicked(object? sender, EventArgs e) => riveView.Play();
    void OnPauseClicked(object? sender, EventArgs e) => riveView.Pause();
    void OnStopClicked(object? sender, EventArgs e) => riveView.Stop();
    void OnResetClicked(object? sender, EventArgs e) => riveView.Reset();

    void UpdateStatus(string text)
    {
        MainThread.BeginInvokeOnMainThread(() => statusLabel.Text = text);
    }
}
