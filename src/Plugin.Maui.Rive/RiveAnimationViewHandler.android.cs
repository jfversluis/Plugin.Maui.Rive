using global::Android.Content;
using Microsoft.Maui.Handlers;
using App.Rive.Runtime.Kotlin;
using global::App.Rive.Runtime.Kotlin.Core;

namespace Plugin.Maui.Rive;

public partial class RiveAnimationViewHandler : ViewHandler<IRiveAnimationView, global::Android.Views.View>
{
    private global::App.Rive.Runtime.Kotlin.RiveAnimationView? _riveView;
    private bool _contentLoaded;

    private static int _riveInitialized;

    private static void EnsureRiveInitialized(global::Android.Content.Context context)
    {
        if (Interlocked.CompareExchange(ref _riveInitialized, 1, 0) != 0) return;

        try
        {
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
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _riveInitialized, 0);
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Rive init failed: {ex}");
        }
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
        if (_riveView != null && !_contentLoaded) LoadRiveContent(_riveView);
    }

    protected override void DisconnectHandler(global::Android.Views.View platformView)
    {
        _riveView?.Reset();
        _riveView = null;
        _contentLoaded = false;
        base.DisconnectHandler(platformView);
    }

    // --- Text Runs ---

    public partial string? GetTextRunValue(string textRunName)
    {
        try { return _riveView?.GetTextRunValue(textRunName); }
        catch { return null; }
    }

    public partial string? GetTextRunValueAtPath(string textRunName, string path)
    {
        try { return _riveView?.GetTextRunValue(textRunName, path); }
        catch { return null; }
    }

    // --- Introspection ---

    public partial string[] GetArtboardNames()
    {
        try
        {
            var file = GetRiveFileViaReflection();
            if (file == null)
            {
                var name = _riveView?.ArtboardName;
                return name != null ? [name] : [];
            }

            var names = CallListStringMethod(file, "getArtboardNames");
            return names ?? [];
        }
        catch { return []; }
    }

    public partial string[] GetAnimationNames()
    {
        try
        {
            var artboard = GetFirstArtboardViaReflection();
            if (artboard == null) return [];

            var names = CallListStringMethod(artboard, "getAnimationNames");
            return names ?? [];
        }
        catch { return []; }
    }

    public partial string[] GetStateMachineNames()
    {
        try
        {
            var artboard = GetFirstArtboardViaReflection();
            if (artboard == null) return [];

            var names = CallListStringMethod(artboard, "getStateMachineNames");
            return names ?? [];
        }
        catch { return []; }
    }

    public partial string[] GetStateMachineInputNames()
    {
        try
        {
            var artboard = GetFirstArtboardViaReflection();
            if (artboard == null) return [];

            var smMethod = artboard.Class.GetMethod("getFirstStateMachine");
            var sm = smMethod.Invoke(artboard);
            if (sm == null) return [];

            var names = CallListStringMethod(sm as Java.Lang.Object, "getInputNames");
            return names ?? [];
        }
        catch { return []; }
    }

    public partial RiveInputInfo[] GetStateMachineInputs()
    {
        try
        {
            var artboard = GetFirstArtboardViaReflection();
            if (artboard == null) return [];

            var smMethod = artboard.Class.GetMethod("getFirstStateMachine");
            var sm = smMethod.Invoke(artboard) as Java.Lang.Object;
            if (sm == null) return [];

            // Get inputs list via reflection: List<SMIInput>
            var inputsMethod = sm.Class.GetMethod("getInputs");
            var inputsObj = inputsMethod.Invoke(sm) as Java.Lang.Object;
            if (inputsObj == null) return [];

            var inputsList = new global::Android.Runtime.JavaList(inputsObj.Handle, global::Android.Runtime.JniHandleOwnership.DoNotTransfer);
            var result = new List<RiveInputInfo>();

            foreach (Java.Lang.Object input in inputsList)
            {
                var name = input.Class.GetMethod("getName").Invoke(input)?.ToString() ?? "";
                var isTrigger = (bool)(Java.Lang.Boolean)input.Class.GetMethod("isTrigger").Invoke(input)!;
                var isBool = (bool)(Java.Lang.Boolean)input.Class.GetMethod("isBoolean").Invoke(input)!;

                var type = isTrigger ? RiveInputType.Trigger
                         : isBool ? RiveInputType.Boolean
                         : RiveInputType.Number;

                result.Add(new RiveInputInfo(name, type));
            }
            return [.. result];
        }
        catch { return []; }
    }

    /// <summary>
    /// Gets the Rive File object via reflection since the binding types are filtered out.
    /// Uses the controller path: RiveAnimationView -> getController() -> getFile()
    /// </summary>
    private Java.Lang.Object? GetRiveFileViaReflection()
    {
        if (_riveView == null) return null;
        try
        {
            var controllerMethod = _riveView.Class.GetMethod("getController");
            var controller = controllerMethod.Invoke(_riveView);
            if (controller == null) return null;

            var fileMethod = (controller as Java.Lang.Object)!.Class.GetMethod("getFile");
            return fileMethod.Invoke(controller) as Java.Lang.Object;
        }
        catch { return null; }
    }

    private Java.Lang.Object? GetFirstArtboardViaReflection()
    {
        var file = GetRiveFileViaReflection();
        if (file == null) return null;
        try
        {
            var method = file.Class.GetMethod("getFirstArtboard");
            return method.Invoke(file) as Java.Lang.Object;
        }
        catch { return null; }
    }

    private static string[]? CallListStringMethod(Java.Lang.Object? obj, string methodName)
    {
        if (obj == null) return null;
        var method = obj.Class.GetMethod(methodName);
        var result = method.Invoke(obj) as Java.Lang.Object;
        if (result == null) return null;

        var list = new global::Android.Runtime.JavaList<string>(result.Handle, global::Android.Runtime.JniHandleOwnership.DoNotTransfer);
        return [.. list];
    }

    // --- Content Loading ---

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
                    riveView.SetRiveResource(resId, virtualView.ArtboardName, virtualView.AnimationName,
                        virtualView.StateMachineName, virtualView.AutoPlay, false, fit, alignment, loop);
                }
                else
                {
                    LoadFromAssets(riveView, virtualView, fit, alignment, loop);
                }
                _contentLoaded = true;
            }
            else if (!string.IsNullOrEmpty(virtualView.Url))
            {
                LoadFromUrl(riveView, virtualView, fit, alignment, loop);
                _contentLoaded = true;
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

            riveView.SetRiveBytes(bytes, virtualView.ArtboardName, virtualView.AnimationName,
                virtualView.StateMachineName, virtualView.AutoPlay, false, fit, alignment, loop);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Error loading from assets: {ex}");
        }
    }

    private void LoadFromUrl(global::App.Rive.Runtime.Kotlin.RiveAnimationView riveView, IRiveAnimationView virtualView,
        Fit? fit, Alignment? alignment, Loop? loop)
    {
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var bytes = await client.GetByteArrayAsync(virtualView.Url);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    riveView.SetRiveBytes(bytes, virtualView.ArtboardName, virtualView.AnimationName,
                        virtualView.StateMachineName, virtualView.AutoPlay, false, fit, alignment, loop);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Error loading from URL: {ex}");
            }
        });
    }

    // --- Mapping Helpers ---

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

    private static Loop? MapLoopToNative(RiveLoopMode loop) => loop switch
    {
        RiveLoopMode.OneShot => Loop.Oneshot,
        RiveLoopMode.Loop => Loop.LoopMode,
        RiveLoopMode.PingPong => Loop.Pingpong,
        _ => Loop.Auto,
    };

    private static Direction? MapDirectionToNative(RiveDirectionMode dir) => dir switch
    {
        RiveDirectionMode.Backwards => Direction.Backwards,
        RiveDirectionMode.Forwards => Direction.Forwards,
        _ => Direction.Auto,
    };

    // --- Property Mappers ---

    public static void MapResourceName(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (handler._riveView != null)
        {
            handler._contentLoaded = false;
            handler.LoadRiveContent(handler._riveView);
        }
    }

    public static void MapUrl(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (handler._riveView != null)
        {
            handler._contentLoaded = false;
            handler.LoadRiveContent(handler._riveView);
        }
    }

    public static void MapAutoPlay(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (handler._riveView != null)
            handler._riveView.Autoplay = view.AutoPlay;
    }

    public static void MapFit(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        var fit = MapFitToNative(view.Fit);
        if (fit != null && handler._riveView != null)
            handler._riveView.Fit = fit;
    }

    public static void MapAlignment(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        var alignment = MapAlignmentToNative(view.RiveAlignment);
        if (alignment != null && handler._riveView != null)
            handler._riveView.Alignment = alignment;
    }

    public static void MapLayoutScaleFactor(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (handler._riveView != null)
            handler._riveView.LayoutScaleFactor = new Java.Lang.Float(view.LayoutScaleFactor);
    }

    // --- Command Mappers ---

    public static void MapPlay(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (handler._riveView == null) return;

        if (args is RivePlayArgs playArgs)
        {
            var loop = MapLoopToNative(playArgs.Loop);
            var dir = MapDirectionToNative(playArgs.Direction);

            if (!string.IsNullOrEmpty(playArgs.AnimationName))
                handler._riveView.Play(playArgs.AnimationName!, loop!, dir!, false, false);
            else
                handler._riveView.Play(loop!, dir!, false);
        }
        else
        {
            handler._riveView.Play(Loop.Auto, Direction.Auto, false);
        }

        view.IsPlaying = true;
    }

    public static void MapPause(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._riveView?.Pause();
        view.IsPlaying = false;
    }

    public static void MapStop(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._riveView?.Stop();
        view.IsPlaying = false;
    }

    public static void MapReset(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._riveView?.Reset();
    }

    public static void MapFireTrigger(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is string triggerName && view.StateMachineName is string smName)
            handler._riveView?.FireState(smName, triggerName);
    }

    public static void MapSetBoolInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBoolInput input && view.StateMachineName is string smName)
            handler._riveView?.SetBooleanState(smName, input.Name, input.Value);
    }

    public static void MapSetNumberInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveNumberInput input && view.StateMachineName is string smName)
            handler._riveView?.SetNumberState(smName, input.Name, input.Value);
    }

    public static void MapFireTriggerAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTriggerAtPath input)
            handler._riveView?.FireStateAtPath(input.InputName, input.Path);
    }

    public static void MapSetBoolInputAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBoolInputAtPath input)
            handler._riveView?.SetBooleanStateAtPath(input.InputName, input.Value, input.Path);
    }

    public static void MapSetNumberInputAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveNumberInputAtPath input)
            handler._riveView?.SetNumberStateAtPath(input.InputName, input.Value, input.Path);
    }

    public static void MapSetTextRunValue(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTextRun textRun)
        {
            try { handler._riveView?.SetTextRunValue(textRun.TextRunName, textRun.TextValue); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] SetTextRun: {ex.Message}"); }
        }
    }

    public static void MapSetTextRunValueAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTextRun textRun && textRun.Path is string path)
        {
            try { handler._riveView?.SetTextRunValue(textRun.TextRunName, textRun.TextValue, path); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] SetTextRunAtPath: {ex.Message}"); }
        }
    }

    public static void MapSetRiveBytes(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBytesArgs bytesArgs && handler._riveView != null)
        {
            try
            {
                var fit = MapFitToNative(view.Fit);
                var alignment = MapAlignmentToNative(view.RiveAlignment);
                handler._riveView.SetRiveBytes(bytesArgs.Bytes, bytesArgs.ArtboardName, bytesArgs.AnimationName,
                    bytesArgs.StateMachineName, view.AutoPlay, false, fit, alignment, Loop.Auto);
                handler._contentLoaded = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] SetRiveBytes: {ex.Message}");
            }
        }
    }
}
