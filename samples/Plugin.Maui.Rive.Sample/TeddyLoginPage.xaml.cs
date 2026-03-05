namespace Plugin.Maui.Rive.Sample;

public partial class TeddyLoginPage : ContentPage
{
    public TeddyLoginPage()
    {
        InitializeComponent();
    }

    void OnEmailFocused(object? sender, FocusEventArgs e)
    {
        riveView.SetBoolInput("isChecking", true);
        UpdateLookDirection();
    }

    void OnEmailUnfocused(object? sender, FocusEventArgs e)
    {
        riveView.SetBoolInput("isChecking", false);
    }

    void OnEmailTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateLookDirection();
    }

    void UpdateLookDirection()
    {
        // Map text length (0-30) to look direction (0-100)
        var length = emailEntry.Text?.Length ?? 0;
        var lookValue = Math.Min(length * 3.3f, 100f);
        riveView.SetNumberInput("numLook", lookValue);
    }

    void OnPasswordFocused(object? sender, FocusEventArgs e)
    {
        riveView.SetBoolInput("isHandsUp", true);
    }

    void OnPasswordUnfocused(object? sender, FocusEventArgs e)
    {
        riveView.SetBoolInput("isHandsUp", false);
    }

    async void OnLoginClicked(object? sender, EventArgs e)
    {
        // Unfocus fields
        emailEntry.IsEnabled = false;
        passwordEntry.IsEnabled = false;
        loginButton.IsEnabled = false;

        riveView.SetBoolInput("isHandsUp", false);
        riveView.SetBoolInput("isChecking", false);

        statusLabel.Text = "Checking credentials...";

        await Task.Delay(1500);

        if (passwordEntry.Text == "rive")
        {
            riveView.FireTrigger("trigSuccess");
            statusLabel.Text = "✅ Welcome back!";
        }
        else
        {
            riveView.FireTrigger("trigFail");
            statusLabel.Text = "❌ Wrong password. Try 'rive'";
        }

        await Task.Delay(2000);

        emailEntry.IsEnabled = true;
        passwordEntry.IsEnabled = true;
        loginButton.IsEnabled = true;
    }
}
