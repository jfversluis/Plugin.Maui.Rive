namespace Plugin.Maui.Rive.Sample;

public partial class FitAlignmentPage : ContentPage
{
    public FitAlignmentPage()
    {
        InitializeComponent();
    }

    void OnFitClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        if (Enum.TryParse<RiveFitMode>(btn.Text, out var fit))
        {
            riveView.Fit = fit;
            UpdateLabel();
        }
    }

    void OnAlignClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string param) return;
        if (Enum.TryParse<RiveAlignmentMode>(param, out var align))
        {
            riveView.RiveAlignment = align;
            UpdateLabel();
        }
    }

    void OnScaleChanged(object? sender, ValueChangedEventArgs e)
    {
        riveView.LayoutScaleFactor = (float)e.NewValue;
        scaleLabel.Text = $"{e.NewValue:F1}x";
    }

    void UpdateLabel()
    {
        settingsLabel.Text = $"Fit: {riveView.Fit}  •  Alignment: {riveView.RiveAlignment}";
    }
}
