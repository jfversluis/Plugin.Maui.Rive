namespace Plugin.Maui.Rive.Sample;

public partial class ByteLoadingPage : ContentPage
{
    public ByteLoadingPage()
    {
        InitializeComponent();
    }

    void OnLoadResourceClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string name)
        {
            riveView.Url = null;
            riveView.ResourceName = name;
            SetStatus($"✅ Loaded from resource: {name}");
        }
    }

    async void OnLoadUrlClicked(object? sender, EventArgs e)
    {
        var url = urlEntry.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            SetStatus("❌ Please enter a URL.");
            return;
        }

        SetStatus($"⏳ Loading from URL...");

        try
        {
            riveView.ResourceName = null;
            riveView.Url = url;
            SetStatus($"✅ Loading from URL: {url}");
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Error: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    void SetStatus(string text)
    {
        MainThread.BeginInvokeOnMainThread(() => statusLabel.Text = $"[{DateTime.Now:HH:mm:ss}] {text}");
    }
}
