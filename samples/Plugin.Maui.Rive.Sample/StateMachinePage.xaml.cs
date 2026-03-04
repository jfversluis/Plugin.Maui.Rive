namespace Plugin.Maui.Rive.Sample;

public partial class StateMachinePage : ContentPage
{
    private readonly List<string> _log = [];

    public StateMachinePage()
    {
        InitializeComponent();

        riveView.RiveEventReceived += (s, e) => Log($"🎯 Event: {e.Name}");
        riveView.StateChanged += (s, e) => Log($"⚡ {e.StateMachineName} → {e.StateName}");
        riveView.PlaybackStarted += (s, e) => Log("▶ Started");
    }

    void OnSkillsClicked(object? s, EventArgs e) => riveView.ResourceName = "skills";
    void OnOffRoadClicked(object? s, EventArgs e) => riveView.ResourceName = "off_road_car";
    void OnBearClicked(object? s, EventArgs e) => riveView.ResourceName = "bear";

    void OnFireTriggerClicked(object? s, EventArgs e)
    {
        var name = triggerEntry.Text;
        if (string.IsNullOrWhiteSpace(name)) return;
        riveView.FireTrigger(name);
        Log($"🔫 Fired trigger: {name}");
    }

    void OnSetBoolTrueClicked(object? s, EventArgs e)
    {
        var name = boolNameEntry.Text;
        if (string.IsNullOrWhiteSpace(name)) return;
        riveView.SetBoolInput(name, true);
        Log($"✅ {name} = true");
    }

    void OnSetBoolFalseClicked(object? s, EventArgs e)
    {
        var name = boolNameEntry.Text;
        if (string.IsNullOrWhiteSpace(name)) return;
        riveView.SetBoolInput(name, false);
        Log($"❌ {name} = false");
    }

    void OnNumberSliderChanged(object? s, ValueChangedEventArgs e)
    {
        var name = numberNameEntry.Text;
        numberValueLabel.Text = $"Value: {e.NewValue:F1}";
        if (string.IsNullOrWhiteSpace(name)) return;
        riveView.SetNumberInput(name, (float)e.NewValue);
    }

    void Log(string text)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {text}");
            if (_log.Count > 6) _log.RemoveAt(6);
            logLabel.Text = string.Join("\n", _log);
        });
    }
}
