using Microsoft.Maui.Handlers;
using Foundation;
using RiveRuntime;
using UIKit;

namespace Plugin.Maui.Rive;

public partial class RiveAnimationViewHandler : ViewHandler<IRiveAnimationView, UIView>
{
    private RiveViewModel? _viewModel;
    private RiveView? _riveView;

    protected override UIView CreatePlatformView()
    {
        var virtualView = VirtualView;
        var fit = MapFitToNative(virtualView.Fit);
        var alignment = MapAlignmentToNative(virtualView.RiveAlignment);

        try
        {
            if (!string.IsNullOrEmpty(virtualView.ResourceName))
            {
                _viewModel = new RiveViewModel(
                    virtualView.ResourceName!,
                    "riv",
                    NSBundle.MainBundle,
                    virtualView.StateMachineName,
                    fit,
                    alignment,
                    virtualView.AutoPlay,
                    virtualView.ArtboardName,
                    true,
                    null);
            }
            else if (!string.IsNullOrEmpty(virtualView.Url))
            {
                _viewModel = new RiveViewModel(
                    virtualView.Url!,
                    virtualView.StateMachineName,
                    fit,
                    alignment,
                    virtualView.AutoPlay,
                    true,
                    virtualView.ArtboardName);
            }

            if (_viewModel != null)
            {
                _riveView = _viewModel.CreateRiveView();
                return _riveView;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Error: {ex}");
        }

        return new UIView { BackgroundColor = UIColor.SystemRed };
    }

    protected override void DisconnectHandler(UIView platformView)
    {
        _viewModel?.DeregisterView();
        _viewModel?.Dispose();
        _viewModel = null;
        _riveView = null;
        base.DisconnectHandler(platformView);
    }

    private static RiveRuntime.RiveFit MapFitToNative(RiveFitMode fit) => fit switch
    {
        RiveFitMode.Fill => RiveRuntime.RiveFit.Fill,
        RiveFitMode.Contain => RiveRuntime.RiveFit.Contain,
        RiveFitMode.Cover => RiveRuntime.RiveFit.Cover,
        RiveFitMode.FitHeight => RiveRuntime.RiveFit.FitHeight,
        RiveFitMode.FitWidth => RiveRuntime.RiveFit.FitWidth,
        RiveFitMode.ScaleDown => RiveRuntime.RiveFit.ScaleDown,
        RiveFitMode.NoFit => RiveRuntime.RiveFit.NoFit,
        RiveFitMode.Layout => RiveRuntime.RiveFit.Layout,
        _ => RiveRuntime.RiveFit.Contain,
    };

    private static RiveRuntime.RiveAlignment MapAlignmentToNative(RiveAlignmentMode alignment) => alignment switch
    {
        RiveAlignmentMode.TopLeft => RiveRuntime.RiveAlignment.TopLeft,
        RiveAlignmentMode.TopCenter => RiveRuntime.RiveAlignment.TopCenter,
        RiveAlignmentMode.TopRight => RiveRuntime.RiveAlignment.TopRight,
        RiveAlignmentMode.CenterLeft => RiveRuntime.RiveAlignment.CenterLeft,
        RiveAlignmentMode.Center => RiveRuntime.RiveAlignment.Center,
        RiveAlignmentMode.CenterRight => RiveRuntime.RiveAlignment.CenterRight,
        RiveAlignmentMode.BottomLeft => RiveRuntime.RiveAlignment.BottomLeft,
        RiveAlignmentMode.BottomCenter => RiveRuntime.RiveAlignment.BottomCenter,
        RiveAlignmentMode.BottomRight => RiveRuntime.RiveAlignment.BottomRight,
        _ => RiveRuntime.RiveAlignment.Center,
    };

    public static void MapResourceName(RiveAnimationViewHandler handler, IRiveAnimationView view) { }
    public static void MapUrl(RiveAnimationViewHandler handler, IRiveAnimationView view) { }

    public static void MapAutoPlay(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (handler._viewModel is not null)
            handler._viewModel.AutoPlay = view.AutoPlay;
    }

    public static void MapFit(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (handler._viewModel is not null)
            handler._viewModel.Fit = MapFitToNative(view.Fit);
    }

    public static void MapAlignment(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (handler._viewModel is not null)
            handler._viewModel.Alignment = MapAlignmentToNative(view.RiveAlignment);
    }

    public static void MapPlay(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._viewModel?.Play(null, RiveRuntime.RiveLoop.AutoLoop, RiveRuntime.RiveDirection.AutoDirection);
    }

    public static void MapPause(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._viewModel?.Pause();
    }

    public static void MapStop(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._viewModel?.Stop();
    }

    public static void MapReset(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._viewModel?.Reset();
    }

    public static void MapFireTrigger(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is string triggerName)
            handler._viewModel?.TriggerInput(triggerName);
    }

    public static void MapSetBoolInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBoolInput input)
            handler._viewModel?.SetBooleanInput(input.Name, input.Value);
    }

    public static void MapSetNumberInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveNumberInput input)
            handler._viewModel?.SetFloatInput(input.Name, input.Value);
    }
}
