using global::Android.Content;
using Microsoft.Maui.Handlers;
using App.Rive.Runtime.Kotlin;
using global::App.Rive.Runtime.Kotlin.Core;

namespace Plugin.Maui.Rive;

public partial class RiveAnimationViewHandler : ViewHandler<IRiveAnimationView, global::Android.Views.View>
{
    private global::App.Rive.Runtime.Kotlin.RiveAnimationView? _riveView;

    private static bool _riveInitialized;

    private static void EnsureRiveInitialized(global::Android.Content.Context context)
    {
        if (_riveInitialized) return;
        
        // Call Rive.init(context) via JNI since the RendererType binding has issues
        var riveClass = Java.Lang.Class.ForName("app.rive.runtime.kotlin.core.Rive");
        var instanceField = riveClass.GetField("INSTANCE");
        var riveInstance = instanceField.Get(null)!;
        
        var rendererTypeClass = Java.Lang.Class.ForName("app.rive.runtime.kotlin.core.RendererType");
        var riveField = rendererTypeClass.GetField("Rive");
        var riveRenderer = riveField.Get(null)!;
        
        var initMethod = riveInstance.Class.GetMethod("init", 
            Java.Lang.Class.FromType(typeof(global::Android.Content.Context)),
            rendererTypeClass);
        initMethod.Invoke(riveInstance, context, riveRenderer);
        
        _riveInitialized = true;
    }

    protected override global::Android.Views.View CreatePlatformView()
    {
        EnsureRiveInitialized(Context);
        _riveView = new global::App.Rive.Runtime.Kotlin.RiveAnimationView(Context, null);
        return _riveView;
    }

    protected override void ConnectHandler(global::Android.Views.View platformView)
    {
        base.ConnectHandler(platformView);
        if (_riveView != null) LoadRiveContent(_riveView);
    }

    protected override void DisconnectHandler(global::Android.Views.View platformView)
    {
        base.DisconnectHandler(platformView);
    }

    private void LoadRiveContent(global::App.Rive.Runtime.Kotlin.RiveAnimationView riveView)
    {
        var virtualView = VirtualView;
        if (virtualView is null) return;

        var fit = MapFitToNative(virtualView.Fit);
        var alignment = MapAlignmentToNative(virtualView.RiveAlignment);
        var loop = Loop.Auto;

        try
        {
            if (!string.IsNullOrEmpty(virtualView.ResourceName))
            {
                var resId = Context.Resources?.GetIdentifier(
                    virtualView.ResourceName, "raw", Context.PackageName) ?? 0;

                if (resId != 0)
                {
                    riveView.SetRiveResource(
                        resId,
                        virtualView.ArtboardName,
                        virtualView.AnimationName,
                        virtualView.StateMachineName,
                        virtualView.AutoPlay,
                        false,
                        fit,
                        alignment,
                        loop);
                }
                else
                {
                    LoadFromAssets(riveView, virtualView, fit, alignment, loop);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Error: {ex}");
        }
    }

    private void LoadFromAssets(global::App.Rive.Runtime.Kotlin.RiveAnimationView riveView, IRiveAnimationView virtualView,
        Fit? fit, Alignment? alignment, Loop? loop)
    {
        try
        {
            var fileName = virtualView.ResourceName + ".riv";
            using var stream = Context.Assets?.Open(fileName);
            if (stream == null) return;

            using var ms = new System.IO.MemoryStream();
            stream.CopyTo(ms);
            var bytes = ms.ToArray();

            riveView.SetRiveBytes(
                bytes,
                virtualView.ArtboardName,
                virtualView.AnimationName,
                virtualView.StateMachineName,
                virtualView.AutoPlay,
                false,
                fit,
                alignment,
                loop);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Error loading from assets: {ex}");
        }
    }

    private static Fit? MapFitToNative(RiveFitMode fit) => fit switch
    {
        RiveFitMode.Fill => Fit.Fill,
        RiveFitMode.Contain => Fit.Contain,
        RiveFitMode.Cover => Fit.Cover,
        RiveFitMode.FitHeight => Fit.FitHeight,
        RiveFitMode.FitWidth => Fit.FitWidth,
        RiveFitMode.ScaleDown => Fit.ScaleDown,
        RiveFitMode.NoFit => Fit.None,
        RiveFitMode.Layout => Fit.Layout,
        _ => Fit.Contain,
    };

    private static Alignment? MapAlignmentToNative(RiveAlignmentMode alignment) => alignment switch
    {
        RiveAlignmentMode.TopLeft => Alignment.TopLeft,
        RiveAlignmentMode.TopCenter => Alignment.TopCenter,
        RiveAlignmentMode.TopRight => Alignment.TopRight,
        RiveAlignmentMode.CenterLeft => Alignment.CenterLeft,
        RiveAlignmentMode.Center => Alignment.Center,
        RiveAlignmentMode.CenterRight => Alignment.CenterRight,
        RiveAlignmentMode.BottomLeft => Alignment.BottomLeft,
        RiveAlignmentMode.BottomCenter => Alignment.BottomCenter,
        RiveAlignmentMode.BottomRight => Alignment.BottomRight,
        _ => Alignment.Center,
    };

    public static void MapResourceName(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (handler._riveView != null) handler.LoadRiveContent(handler._riveView);
    }

    public static void MapUrl(RiveAnimationViewHandler handler, IRiveAnimationView view) { }

    public static void MapAutoPlay(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        handler._riveView!.Autoplay = view.AutoPlay;
    }

    public static void MapFit(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        var fit = MapFitToNative(view.Fit);
        if (fit != null) handler._riveView!.Fit = fit;
    }

    public static void MapAlignment(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        var alignment = MapAlignmentToNative(view.RiveAlignment);
        if (alignment != null) handler._riveView!.Alignment = alignment;
    }

    public static void MapPlay(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._riveView!.Play(Loop.Auto, Direction.Auto, false);
    }

    public static void MapPause(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._riveView!.Pause();
    }

    public static void MapStop(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._riveView!.Stop();
    }

    public static void MapReset(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._riveView!.Reset();
    }

    public static void MapFireTrigger(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is string triggerName && view.StateMachineName is string smName)
            handler._riveView!.FireState(smName, triggerName);
    }

    public static void MapSetBoolInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBoolInput input && view.StateMachineName is string smName)
            handler._riveView!.SetBooleanState(smName, input.Name, input.Value);
    }

    public static void MapSetNumberInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveNumberInput input && view.StateMachineName is string smName)
            handler._riveView!.SetNumberState(smName, input.Name, input.Value);
    }
}
