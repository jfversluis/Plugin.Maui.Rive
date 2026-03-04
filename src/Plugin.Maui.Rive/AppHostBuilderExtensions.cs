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
#if IOS || MACCATALYST
        // On simulators, use CoreGraphics renderer since Metal PLS may not render.
        // Must be done before any RiveViewModel is created.
        try
        {
            if (ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR)
                RiveRuntime.RenderContextManager.Shared.DefaultRenderer = RiveRuntime.RendererType.CgRenderer;
        }
        catch { /* best effort */ }
#endif

        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<RiveAnimationView, RiveAnimationViewHandler>();
        });

        return builder;
    }
}
