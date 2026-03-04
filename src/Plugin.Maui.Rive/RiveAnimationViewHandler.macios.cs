using CoreGraphics;
using Microsoft.Maui.Handlers;
using Foundation;
using ObjCRuntime;
using RiveRuntime;
using UIKit;

namespace Plugin.Maui.Rive;

public partial class RiveAnimationViewHandler : ViewHandler<IRiveAnimationView, UIView>
{
    private RiveViewModel? _viewModel;
    private RiveView? _riveView;
    private RiveStateMachineDelegateProxy? _stateMachineDelegate;
    private RivePlayerDelegateProxy? _playerDelegate;

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
                // Create a container UIView and embed the RiveView inside with autolayout
                _riveView = _viewModel.CreateRiveView();
                var container = new UIView();
                container.AddSubview(_riveView);
                _riveView.TranslatesAutoresizingMaskIntoConstraints = false;
                NSLayoutConstraint.ActivateConstraints(new[]
                {
                    _riveView.LeadingAnchor.ConstraintEqualTo(container.LeadingAnchor),
                    _riveView.TrailingAnchor.ConstraintEqualTo(container.TrailingAnchor),
                    _riveView.TopAnchor.ConstraintEqualTo(container.TopAnchor),
                    _riveView.BottomAnchor.ConstraintEqualTo(container.BottomAnchor),
                });
                return container;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Error: {ex}");
        }

        return new UIView { BackgroundColor = UIColor.SystemRed };
    }

    protected override void ConnectHandler(UIView platformView)
    {
        base.ConnectHandler(platformView);

        if (_riveView != null && _viewModel != null)
        {
            // Wire up delegates for events and playback callbacks
            _stateMachineDelegate = new RiveStateMachineDelegateProxy(this);
            _playerDelegate = new RivePlayerDelegateProxy(this);

            // Set delegates via ObjC runtime on the view model
            SetDelegatesOnViewModel(_viewModel, _stateMachineDelegate, _playerDelegate);

            // Restart playback to ensure CADisplayLink connects
            NSTimer.CreateScheduledTimer(0.5, false, _ =>
            {
                _viewModel.Stop();
                _viewModel.Play(null, RiveRuntime.RiveLoop.AutoLoop, RiveRuntime.RiveDirection.AutoDirection);
            });
        }
    }

    private static void SetDelegatesOnViewModel(RiveViewModel viewModel, RiveStateMachineDelegateProxy smDelegate, RivePlayerDelegateProxy playerDelegate)
    {
        try
        {
            // Use objc_msgSend directly to set delegates
            var smDelegSel = Selector.GetHandle("setStateMachineDelegate:");
            if (viewModel.RespondsToSelector(new Selector("setStateMachineDelegate:")))
                void_objc_msgSend_IntPtr(viewModel.Handle, smDelegSel, smDelegate.Handle);

            var playerDelegSel = Selector.GetHandle("setPlayerDelegate:");
            if (viewModel.RespondsToSelector(new Selector("setPlayerDelegate:")))
                void_objc_msgSend_IntPtr(viewModel.Handle, playerDelegSel, playerDelegate.Handle);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Delegate setup: {ex.Message}");
        }
    }

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern void void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    protected override void DisconnectHandler(UIView platformView)
    {
        _viewModel?.DeregisterView();
        _viewModel?.Dispose();
        _viewModel = null;
        _riveView = null;
        _stateMachineDelegate = null;
        _playerDelegate = null;
        base.DisconnectHandler(platformView);
    }

    // --- Text Runs ---

    public partial string? GetTextRunValue(string textRunName)
    {
        return _viewModel?.GetTextRunValue(textRunName);
    }

    public partial string? GetTextRunValueAtPath(string textRunName, string path)
    {
        return _viewModel?.GetTextRunValue(textRunName);
    }

    // --- Introspection ---

    public partial string[] GetArtboardNames()
    {
        try { return _viewModel?.ArtboardNames ?? []; }
        catch { return []; }
    }

    public partial string[] GetAnimationNames()
    {
        try
        {
            // Access artboard via the RiveModel -> RiveFile -> Artboard chain
            var riveModel = _viewModel?.RiveModel;
            if (riveModel == null) return [];

            var riveFileSel = Selector.GetHandle("riveFile");
            if (!riveModel.RespondsToSelector(new Selector("riveFile"))) return [];

            var fileHandle = IntPtr_objc_msgSend(riveModel.Handle, riveFileSel);
            if (fileHandle == IntPtr.Zero) return [];

            var riveFile = ObjCRuntime.Runtime.GetNSObject<RiveFile>(fileHandle);
            if (riveFile == null) return [];

            var artboard = riveFile.GetArtboard(out _);
            return artboard?.AnimationNames ?? [];
        }
        catch { return []; }
    }

    public partial string[] GetStateMachineNames()
    {
        try
        {
            var riveModel = _viewModel?.RiveModel;
            if (riveModel == null) return [];

            var riveFileSel = Selector.GetHandle("riveFile");
            if (!riveModel.RespondsToSelector(new Selector("riveFile"))) return [];

            var fileHandle = IntPtr_objc_msgSend(riveModel.Handle, riveFileSel);
            if (fileHandle == IntPtr.Zero) return [];

            var riveFile = ObjCRuntime.Runtime.GetNSObject<RiveFile>(fileHandle);
            if (riveFile == null) return [];

            var artboard = riveFile.GetArtboard(out _);
            return artboard?.StateMachineNames ?? [];
        }
        catch { return []; }
    }

    public partial string[] GetStateMachineInputNames()
    {
        try
        {
            var riveModel = _viewModel?.RiveModel;
            if (riveModel == null) return [];

            var riveFileSel = Selector.GetHandle("riveFile");
            if (!riveModel.RespondsToSelector(new Selector("riveFile"))) return [];

            var fileHandle = IntPtr_objc_msgSend(riveModel.Handle, riveFileSel);
            if (fileHandle == IntPtr.Zero) return [];

            var riveFile = ObjCRuntime.Runtime.GetNSObject<RiveFile>(fileHandle);
            if (riveFile == null) return [];

            var artboard = riveFile.GetArtboard(out _);
            var sm = artboard?.DefaultStateMachine;
            return sm?.InputNames ?? [];
        }
        catch { return []; }
    }

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    // --- Mapping helpers ---

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

    private static RiveRuntime.RiveLoop MapLoopToNative(RiveLoopMode loop) => loop switch
    {
        RiveLoopMode.OneShot => RiveRuntime.RiveLoop.OneShot,
        RiveLoopMode.Loop => RiveRuntime.RiveLoop.Loop,
        RiveLoopMode.PingPong => RiveRuntime.RiveLoop.PingPong,
        _ => RiveRuntime.RiveLoop.AutoLoop,
    };

    private static RiveRuntime.RiveDirection MapDirectionToNative(RiveDirectionMode dir) => dir switch
    {
        RiveDirectionMode.Backwards => RiveRuntime.RiveDirection.Backwards,
        RiveDirectionMode.Forwards => RiveRuntime.RiveDirection.Forwards,
        _ => RiveRuntime.RiveDirection.AutoDirection,
    };

    // --- Property Mappers ---

    public static void MapResourceName(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        // Reload the entire view when the resource changes at runtime
        if (handler._viewModel == null) return;
        handler.ReloadRiveContent();
    }

    public static void MapUrl(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (handler._viewModel == null) return;
        handler.ReloadRiveContent();
    }

    private void ReloadRiveContent()
    {
        var virtualView = VirtualView;
        if (virtualView == null) return;

        _viewModel?.DeregisterView();
        _viewModel?.Dispose();

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

            if (_viewModel != null && _riveView != null)
            {
                _viewModel.SetRiveView(_riveView);
                if (_stateMachineDelegate != null && _playerDelegate != null)
                    SetDelegatesOnViewModel(_viewModel, _stateMachineDelegate, _playerDelegate);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Reload error: {ex}");
        }
    }

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

    public static void MapLayoutScaleFactor(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        // iOS RiveViewModel does not expose layoutScaleFactor directly; no-op on iOS
    }

    // --- Command Mappers ---

    public static void MapPlay(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RivePlayArgs playArgs)
        {
            handler._viewModel?.Play(
                playArgs.AnimationName,
                MapLoopToNative(playArgs.Loop),
                MapDirectionToNative(playArgs.Direction));
        }
        else
        {
            handler._viewModel?.Play(null, RiveRuntime.RiveLoop.AutoLoop, RiveRuntime.RiveDirection.AutoDirection);
        }
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

    public static void MapFireTriggerAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTriggerAtPath input)
            handler._viewModel?.TriggerInput(input.InputName);
    }

    public static void MapSetBoolInputAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBoolInputAtPath input)
            handler._viewModel?.SetBooleanInput(input.InputName, input.Value);
    }

    public static void MapSetNumberInputAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveNumberInputAtPath input)
            handler._viewModel?.SetFloatInput(input.InputName, input.Value);
    }

    public static void MapSetTextRunValue(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTextRun textRun)
        {
            try { handler._viewModel?.SetTextRunValue(textRun.TextRunName, textRun.TextValue, out _); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] SetTextRun: {ex.Message}"); }
        }
    }

    public static void MapSetTextRunValueAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTextRun textRun)
        {
            try { handler._viewModel?.SetTextRunValue(textRun.TextRunName, textRun.TextValue, out _); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] SetTextRunAtPath: {ex.Message}"); }
        }
    }

    public static void MapSetRiveBytes(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBytesArgs bytesArgs)
        {
            try
            {
                var data = NSData.FromArray(bytesArgs.Bytes);
                var riveFile = new RiveFile(data, true, out var error);
                if (error != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] SetRiveBytes error: {error}");
                    return;
                }

                // Reconfigure the view model with the new file's artboard
                handler._viewModel?.ConfigureModel(
                    bytesArgs.ArtboardName,
                    bytesArgs.StateMachineName,
                    bytesArgs.AnimationName,
                    out _);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] SetRiveBytes: {ex.Message}");
            }
        }
    }
}

// --- Delegate Proxies ---

/// <summary>Proxy for RiveStateMachineDelegate to receive Rive events.</summary>
internal class RiveStateMachineDelegateProxy : RiveStateMachineDelegate
{
    private readonly WeakReference<RiveAnimationViewHandler> _handlerRef;

    public RiveStateMachineDelegateProxy(RiveAnimationViewHandler handler)
    {
        _handlerRef = new WeakReference<RiveAnimationViewHandler>(handler);
    }

    [Export("onRiveEventReceived:")]
    public void OnRiveEventReceived(NSObject riveEvent)
    {
        if (!_handlerRef.TryGetTarget(out var handler)) return;

        var name = riveEvent.ValueForKey(new NSString("name"))?.ToString() ?? "";
        var delay = 0f;
        var props = new Dictionary<string, object>();

        try
        {
            var delayVal = riveEvent.ValueForKey(new NSString("delay"));
            if (delayVal is NSNumber num)
                delay = num.FloatValue;

            var propsVal = riveEvent.ValueForKey(new NSString("properties"));
            if (propsVal is NSDictionary dict)
            {
                foreach (var key in dict.Keys)
                {
                    var val = dict[key];
                    if (val is NSString s) props[key.ToString()] = s.ToString();
                    else if (val is NSNumber n) props[key.ToString()] = n.DoubleValue;
                    else if (val != null) props[key.ToString()] = val.ToString()!;
                }
            }
        }
        catch { /* best effort */ }

        handler.VirtualView?.OnRiveEventReceived(new RiveEventReceivedEventArgs(name, delay, props));
    }

    [Export("stateMachine:didChangeState:")]
    public void DidChangeState(NSObject stateMachine, NSString stateName)
    {
        if (!_handlerRef.TryGetTarget(out var handler)) return;

        var smName = stateMachine.ValueForKey(new NSString("name"))?.ToString() ?? "";
        handler.VirtualView?.OnStateChanged(new RiveStateChangedEventArgs(smName, stateName.ToString()));
    }
}

/// <summary>Proxy for RivePlayerDelegate to receive playback callbacks.</summary>
internal class RivePlayerDelegateProxy : RivePlayerDelegate
{
    private readonly WeakReference<RiveAnimationViewHandler> _handlerRef;

    public RivePlayerDelegateProxy(RiveAnimationViewHandler handler)
    {
        _handlerRef = new WeakReference<RiveAnimationViewHandler>(handler);
    }

    [Export("player:didAdvanceby:")]
    public void DidAdvance(NSObject player, double seconds) { }

    [Export("player:playedWithModel:")]
    public void Played(NSObject player, NSObject? model)
    {
        if (!_handlerRef.TryGetTarget(out var handler)) return;
        handler.VirtualView?.OnPlaybackStarted(new RivePlaybackEventArgs());
    }

    [Export("player:pausedWithModel:")]
    public void Paused(NSObject player, NSObject? model)
    {
        if (!_handlerRef.TryGetTarget(out var handler)) return;
        handler.VirtualView?.OnPlaybackPaused(new RivePlaybackEventArgs());
    }

    [Export("player:stoppedWithModel:")]
    public void Stopped(NSObject player, NSObject? model)
    {
        if (!_handlerRef.TryGetTarget(out var handler)) return;
        handler.VirtualView?.OnPlaybackStopped(new RivePlaybackEventArgs());
    }

    [Export("player:loopedWithModel:type:")]
    public void Looped(NSObject player, NSObject? model, nint type)
    {
        if (!_handlerRef.TryGetTarget(out var handler)) return;
        handler.VirtualView?.OnPlaybackLooped(new RivePlaybackEventArgs());
    }
}
