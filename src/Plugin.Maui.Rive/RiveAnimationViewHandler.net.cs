using Microsoft.Maui.Handlers;

namespace Plugin.Maui.Rive;

public partial class RiveAnimationViewHandler : ViewHandler<IRiveAnimationView, object>
{
    protected override object CreatePlatformView() =>
        throw new PlatformNotSupportedException("Rive is not supported on this platform.");

    public static void MapResourceName(RiveAnimationViewHandler handler, IRiveAnimationView view) { }
    public static void MapUrl(RiveAnimationViewHandler handler, IRiveAnimationView view) { }
    public static void MapAutoPlay(RiveAnimationViewHandler handler, IRiveAnimationView view) { }
    public static void MapFit(RiveAnimationViewHandler handler, IRiveAnimationView view) { }
    public static void MapAlignment(RiveAnimationViewHandler handler, IRiveAnimationView view) { }
    public static void MapPlay(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args) { }
    public static void MapPause(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args) { }
    public static void MapStop(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args) { }
    public static void MapReset(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args) { }
    public static void MapFireTrigger(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args) { }
    public static void MapSetBoolInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args) { }
    public static void MapSetNumberInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args) { }
}
