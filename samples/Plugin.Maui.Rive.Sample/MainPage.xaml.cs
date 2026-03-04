namespace Plugin.Maui.Rive.Sample;

public partial class MainPage : ContentPage
{
    private readonly List<string> _eventLog = [];
    private RiveLoopMode _currentLoop = RiveLoopMode.Auto;
    private RiveDirectionMode _currentDirection = RiveDirectionMode.Auto;

    public MainPage()
    {
        InitializeComponent();

        riveView.PlaybackStarted += (s, e) => LogEvent($"▶ Started{(e.AnimationName != null ? $": {e.AnimationName}" : "")}");
        riveView.PlaybackPaused += (s, e) => LogEvent("⏸ Paused");
        riveView.PlaybackStopped += (s, e) => LogEvent("⏹ Stopped");
        riveView.PlaybackLooped += (s, e) => LogEvent("↻ Looped");
        riveView.RiveEventReceived += (s, e) => LogEvent($"🎯 Event: {e.Name} (delay={e.Delay:F2})");
        riveView.StateChanged += (s, e) => LogEvent($"⚡ State: {e.StateName}");
    }

    // Animation switching
    void OnVehiclesClicked(object? s, EventArgs e) => riveView.ResourceName = "vehicles";
    void OnBearClicked(object? s, EventArgs e) => riveView.ResourceName = "bear";
    void OnExplorerClicked(object? s, EventArgs e) => riveView.ResourceName = "explorer";
    void OnTreeClicked(object? s, EventArgs e) => riveView.ResourceName = "windy_tree";

    // Playback
    void OnPlayClicked(object? s, EventArgs e) => riveView.Play(loop: _currentLoop, direction: _currentDirection);
    void OnPauseClicked(object? s, EventArgs e) => riveView.Pause();
    void OnStopClicked(object? s, EventArgs e) => riveView.Stop();
    void OnResetClicked(object? s, EventArgs e) => riveView.Reset();

    // Loop modes
    void OnLoopOneShotClicked(object? s, EventArgs e) { _currentLoop = RiveLoopMode.OneShot; LogEvent("Loop → OneShot"); }
    void OnLoopLoopClicked(object? s, EventArgs e) { _currentLoop = RiveLoopMode.Loop; LogEvent("Loop → Loop"); }
    void OnLoopPingPongClicked(object? s, EventArgs e) { _currentLoop = RiveLoopMode.PingPong; LogEvent("Loop → PingPong"); }
    void OnLoopAutoClicked(object? s, EventArgs e) { _currentLoop = RiveLoopMode.Auto; LogEvent("Loop → Auto"); }

    // Direction
    void OnDirectionForwardsClicked(object? s, EventArgs e) { _currentDirection = RiveDirectionMode.Forwards; LogEvent("Dir → Forwards"); }
    void OnDirectionBackwardsClicked(object? s, EventArgs e) { _currentDirection = RiveDirectionMode.Backwards; LogEvent("Dir → Backwards"); }
    void OnDirectionAutoClicked(object? s, EventArgs e) { _currentDirection = RiveDirectionMode.Auto; LogEvent("Dir → Auto"); }

    void LogEvent(string text)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _eventLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {text}");
            if (_eventLog.Count > 8) _eventLog.RemoveAt(8);
            eventLogLabel.Text = string.Join("\n", _eventLog);
        });
    }
}
