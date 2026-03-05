namespace Plugin.Maui.Rive.Sample;

public partial class StateMachinePage : ContentPage
{
    private readonly List<string> _log = [];

    public StateMachinePage()
    {
        InitializeComponent();

        riveView.RiveEventReceived += (s, e) => Log($"🎯 Event: {e.Name}");
        riveView.StateChanged += (s, e) => Log($"⚡ {e.StateMachineName} → {e.StateName}");
        riveView.PlaybackStarted += (s, e) =>
        {
            Log("▶ Started");
            // Auto-refresh inputs when playback starts (animation is loaded)
            _ = Task.Delay(500).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(RefreshInputs));
        };
    }

    void OnAnimationClicked(object? s, EventArgs e)
    {
        if (s is Button btn && btn.CommandParameter is string name)
        {
            riveView.ResourceName = name;
            Log($"📂 Loaded: {name}");
            ClearInputUI();
        }
    }

    void OnRefreshInputsClicked(object? s, EventArgs e) => RefreshInputs();

    void RefreshInputs()
    {
        ClearInputUI();

        var inputs = riveView.GetStateMachineInputs();
        if (inputs.Length == 0)
        {
            noInputsLabel.Text = "No state machine inputs found.";
            noInputsLabel.IsVisible = true;
            return;
        }

        noInputsLabel.IsVisible = false;

        var triggers = inputs.Where(i => i.Type == RiveInputType.Trigger).ToArray();
        var booleans = inputs.Where(i => i.Type == RiveInputType.Boolean).ToArray();
        var numbers = inputs.Where(i => i.Type == RiveInputType.Number).ToArray();

        if (triggers.Length > 0)
        {
            triggersSection.IsVisible = true;
            foreach (var t in triggers)
            {
                var btn = new Button
                {
                    Text = $"🔫 {t.Name}",
                    FontSize = 13,
                    Padding = new Thickness(12, 4),
                    Margin = new Thickness(0, 0, 8, 4)
                };
                var triggerName = t.Name;
                btn.Clicked += (_, _) =>
                {
                    riveView.FireTrigger(triggerName);
                    Log($"🔫 Fired: {triggerName}");
                };
                triggersContainer.Add(btn);
            }
        }

        if (booleans.Length > 0)
        {
            booleansSection.IsVisible = true;
            foreach (var b in booleans)
            {
                var row = new Grid
                {
                    ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
                    Padding = new Thickness(4, 2)
                };
                row.Add(new Label { Text = b.Name, VerticalOptions = LayoutOptions.Center, FontSize = 14 });
                var toggle = new Switch { IsToggled = false };
                var boolName = b.Name;
                toggle.Toggled += (_, args) =>
                {
                    riveView.SetBoolInput(boolName, args.Value);
                    Log($"{(args.Value ? "✅" : "❌")} {boolName} = {args.Value}");
                };
                Grid.SetColumn(toggle, 1);
                row.Add(toggle);
                booleansContainer.Add(row);
            }
        }

        if (numbers.Length > 0)
        {
            numbersSection.IsVisible = true;
            foreach (var n in numbers)
            {
                var label = new Label
                {
                    Text = $"{n.Name}: 50",
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold
                };
                var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50 };
                var numName = n.Name;
                slider.ValueChanged += (_, args) =>
                {
                    label.Text = $"{numName}: {args.NewValue:F1}";
                    riveView.SetNumberInput(numName, (float)args.NewValue);
                };
                numbersContainer.Add(label);
                numbersContainer.Add(slider);
            }
        }

        Log($"🔍 Found {triggers.Length} triggers, {booleans.Length} bools, {numbers.Length} numbers");
    }

    void ClearInputUI()
    {
        triggersContainer.Clear();
        booleansContainer.Clear();
        numbersContainer.Clear();
        triggersSection.IsVisible = false;
        booleansSection.IsVisible = false;
        numbersSection.IsVisible = false;
        noInputsLabel.IsVisible = true;
        noInputsLabel.Text = "Tap Refresh to discover inputs...";
    }

    void Log(string text)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {text}");
            if (_log.Count > 8) _log.RemoveAt(8);
            logLabel.Text = string.Join("\n", _log);
        });
    }
}
