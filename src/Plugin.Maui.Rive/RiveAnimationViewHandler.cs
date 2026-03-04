#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#else
using PlatformView = System.Object;
#endif
using Microsoft.Maui.Handlers;

namespace Plugin.Maui.Rive;

/// <summary>
/// Handler for RiveAnimationView that bridges the cross-platform control to native views.
/// </summary>
public partial class RiveAnimationViewHandler
{
    public static IPropertyMapper<IRiveAnimationView, RiveAnimationViewHandler> Mapper =
        new PropertyMapper<IRiveAnimationView, RiveAnimationViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IRiveAnimationView.ResourceName)] = MapResourceName,
            [nameof(IRiveAnimationView.Url)] = MapUrl,
            [nameof(IRiveAnimationView.AutoPlay)] = MapAutoPlay,
            [nameof(IRiveAnimationView.Fit)] = MapFit,
            [nameof(IRiveAnimationView.RiveAlignment)] = MapAlignment,
        };

    public static CommandMapper<IRiveAnimationView, RiveAnimationViewHandler> CommandMapper =
        new(ViewHandler.ViewCommandMapper)
        {
            [nameof(RiveAnimationView.Play)] = MapPlay,
            [nameof(RiveAnimationView.Pause)] = MapPause,
            [nameof(RiveAnimationView.Stop)] = MapStop,
            [nameof(RiveAnimationView.Reset)] = MapReset,
            [nameof(RiveAnimationView.FireTrigger)] = MapFireTrigger,
            [nameof(RiveAnimationView.SetBoolInput)] = MapSetBoolInput,
            [nameof(RiveAnimationView.SetNumberInput)] = MapSetNumberInput,
        };

    public RiveAnimationViewHandler() : base(Mapper, CommandMapper)
    {
    }
}
