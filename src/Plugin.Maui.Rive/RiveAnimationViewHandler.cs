#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.Controls.WebView2;
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
            [nameof(IRiveAnimationView.LayoutScaleFactor)] = MapLayoutScaleFactor,
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
            [nameof(RiveAnimationView.FireTriggerAtPath)] = MapFireTriggerAtPath,
            [nameof(RiveAnimationView.SetBoolInputAtPath)] = MapSetBoolInputAtPath,
            [nameof(RiveAnimationView.SetNumberInputAtPath)] = MapSetNumberInputAtPath,
            [nameof(RiveAnimationView.SetTextRunValue)] = MapSetTextRunValue,
            [nameof(RiveAnimationView.SetTextRunValueAtPath)] = MapSetTextRunValueAtPath,
            [nameof(RiveAnimationView.SetRiveBytes)] = MapSetRiveBytes,
        };

    public RiveAnimationViewHandler() : base(Mapper, CommandMapper)
    {
    }

    // Partial methods implemented per platform for text run reads
    public partial string? GetTextRunValue(string textRunName);
    public partial string? GetTextRunValueAtPath(string textRunName, string path);

    // Partial methods for introspection
    public partial string[] GetArtboardNames();
    public partial string[] GetAnimationNames();
    public partial string[] GetStateMachineNames();
    public partial string[] GetStateMachineInputNames();
    public partial RiveInputInfo[] GetStateMachineInputs();
}
