namespace Plugin.Maui.Rive;

/// <summary>
/// Extension methods for registering Rive with MAUI.
/// </summary>
public static class AppHostBuilderExtensions
{
    /// <summary>
    /// Registers the Rive animation view handler with the MAUI app builder.
    /// </summary>
    public static MauiAppBuilder UseRive(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<RiveAnimationView, RiveAnimationViewHandler>();
        });

        return builder;
    }
}
