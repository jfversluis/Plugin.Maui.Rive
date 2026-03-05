using CoreGraphics;
using Microsoft.Maui.Handlers;
using Foundation;
using ObjCRuntime;
using RiveRuntime;
using UIKit;

namespace Plugin.Maui.Rive;

/// <summary>
/// Container view that defers adding the RiveView until it has a valid frame.
/// MAUI's layout system does things to the platform view (like setting frame to zero,
/// manipulating the layer, etc.) that interfere with RiveView's Metal rendering.
/// By using a container, MAUI manages the container while the RiveView
/// lives inside with its Metal layer undisturbed.
/// </summary>
internal class RiveHostView : UIView
{
    private RiveViewModel? _viewModel;
    private RiveView? _riveView;
    private bool _isSetUp;
    private readonly string? _resourceName;
    private readonly string? _url;
    private readonly string? _stateMachineName;
    private readonly string? _artboardName;
    private readonly bool _autoPlay;
    private readonly RiveFit _fit;
    private readonly RiveAlignment _alignment;

    public RiveHostView(
        string? resourceName, string? url,
        string? stateMachineName, string? artboardName,
        bool autoPlay, RiveFit fit, RiveAlignment alignment)
    {
        _resourceName = resourceName;
        _url = url;
        _stateMachineName = stateMachineName;
        _artboardName = artboardName;
        _autoPlay = autoPlay;
        _fit = fit;
        _alignment = alignment;
        ClipsToBounds = true;
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        if (!_isSetUp && Bounds.Width > 0 && Bounds.Height > 0 && Window != null)
        {
            SetupRiveView();
        }

        if (_riveView != null)
        {
            _riveView.Frame = Bounds;
        }
    }

    private void SetupRiveView()
    {
        try
        {
            if (!string.IsNullOrEmpty(_resourceName))
            {
                _viewModel = new RiveViewModel(
                    _resourceName!,
                    "riv",
                    NSBundle.MainBundle,
                    _stateMachineName,
                    _fit,
                    _alignment,
                    _autoPlay,
                    _artboardName,
                    true,
                    null);
            }
            else if (!string.IsNullOrEmpty(_url))
            {
                _viewModel = new RiveViewModel(
                    _url!,
                    _stateMachineName,
                    _fit,
                    _alignment,
                    _autoPlay,
                    true,
                    _artboardName);
            }

            if (_viewModel != null)
            {
                _riveView = _viewModel.CreateRiveView();
                _riveView.Frame = Bounds;
                AddSubview(_riveView);
                _isSetUp = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] SetupRiveView error: {ex}");
        }
    }

    public RiveViewModel? ViewModel => _viewModel;
    public RiveView? RiveView => _riveView;
    public bool IsSetUp => _isSetUp;

    public void TearDown()
    {
        _viewModel?.DeregisterView();
        _viewModel?.Dispose();
        _viewModel = null;
        _riveView?.RemoveFromSuperview();
        _riveView = null;
        _isSetUp = false;
    }

    /// <summary>
    /// Load new content from raw .riv bytes by writing to a temporary file.
    /// </summary>
    public void ReloadWithBytes(byte[] bytes, string? stateMachineName, string? artboardName,
        bool autoPlay, RiveFit fit, RiveAlignment alignment)
    {
        TearDown();

        try
        {
            var tempDir = NSFileManager.DefaultManager.GetTemporaryDirectory().Path!;
            var tempFile = System.IO.Path.Combine(tempDir, $"rive_{Guid.NewGuid():N}.riv");
            System.IO.File.WriteAllBytes(tempFile, bytes);

            var fileUrl = NSUrl.FromFilename(tempFile);
            _viewModel = new RiveViewModel(
                fileUrl!.AbsoluteString!,
                stateMachineName,
                fit,
                alignment,
                autoPlay,
                false,
                artboardName);

            _riveView = _viewModel.CreateRiveView();
            _riveView.Frame = Bounds;
            AddSubview(_riveView);
            _isSetUp = true;

            // Clean up temp file after load
            NSTimer.CreateScheduledTimer(5.0, false, _ =>
            {
                try { System.IO.File.Delete(tempFile); } catch { }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] ReloadWithBytes error: {ex}");
        }
    }

    /// <summary>
    /// Recreate with new parameters (for dynamic resource changes).
    /// </summary>
    public void Reload(string? resourceName, string? url, string? stateMachineName, string? artboardName, bool autoPlay, RiveFit fit, RiveAlignment alignment)
    {
        TearDown();

        try
        {
            RiveViewModel? newVm = null;
            if (!string.IsNullOrEmpty(resourceName))
            {
                newVm = new RiveViewModel(
                    resourceName!,
                    "riv",
                    NSBundle.MainBundle,
                    stateMachineName,
                    fit,
                    alignment,
                    autoPlay,
                    artboardName,
                    true,
                    null);
            }
            else if (!string.IsNullOrEmpty(url))
            {
                newVm = new RiveViewModel(
                    url!,
                    stateMachineName,
                    fit,
                    alignment,
                    autoPlay,
                    true,
                    artboardName);
            }

            if (newVm != null)
            {
                _viewModel = newVm;
                _riveView = _viewModel.CreateRiveView();
                _riveView.Frame = Bounds;
                AddSubview(_riveView);
                _isSetUp = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Reload error: {ex}");
        }
    }
}

public partial class RiveAnimationViewHandler : ViewHandler<IRiveAnimationView, UIView>
{
    private RiveHostView? _hostView;
    private RiveStateMachineDelegateProxy? _stateMachineDelegate;
    private RivePlayerDelegateProxy? _playerDelegate;

    // Convenience accessors
    private RiveViewModel? _viewModel => _hostView?.ViewModel;
    private RiveView? _riveView => _hostView?.RiveView;

    protected override UIView CreatePlatformView()
    {
        var virtualView = VirtualView;
        _hostView = new RiveHostView(
            virtualView.ResourceName,
            virtualView.Url,
            virtualView.StateMachineName,
            virtualView.ArtboardName,
            virtualView.AutoPlay,
            MapFitToNative(virtualView.Fit),
            MapAlignmentToNative(virtualView.RiveAlignment));

        return _hostView;
    }

    protected override void ConnectHandler(UIView platformView)
    {
        base.ConnectHandler(platformView);

        // Delegates will be wired once the host view sets up the Rive view
        WireDelegatesWhenReady();
    }

    private int _wireRetryCount;
    private const int MaxWireRetries = 50; // 50 * 200ms = 10 seconds max

    private void WireDelegatesWhenReady()
    {
        if (_hostView?.IsSetUp == true && _viewModel != null)
        {
            _stateMachineDelegate = new RiveStateMachineDelegateProxy(this);
            _playerDelegate = new RivePlayerDelegateProxy(this);
            SetDelegatesOnViewModel(_viewModel, _stateMachineDelegate, _playerDelegate);
            _wireRetryCount = 0;
        }
        else if (_wireRetryCount < MaxWireRetries && _hostView != null)
        {
            _wireRetryCount++;
            NSTimer.CreateScheduledTimer(0.2, false, _ => WireDelegatesWhenReady());
        }
    }

    private static void SetDelegatesOnViewModel(RiveViewModel viewModel, RiveStateMachineDelegateProxy smDelegate, RivePlayerDelegateProxy playerDelegate)
    {
        try
        {
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
        _hostView?.TearDown();
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
        // iOS binding does not expose path-based text run query; falls back to name-only
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
            using var riveFile = GetRiveFileForCurrentResource();
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
            using var riveFile = GetRiveFileForCurrentResource();
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
            using var riveFile = GetRiveFileForCurrentResource();
            if (riveFile == null) return [];
            var artboard = riveFile.GetArtboard(out _);
            if (artboard == null) return [];

            // Try the default state machine first
            var sm = artboard.DefaultStateMachine;
            if (sm != null)
            {
                var names = sm.InputNames;
                if (names != null && names.Length > 0)
                    return names;
            }

            // Fallback: try each state machine by index
            var count = artboard.StateMachineCount;
            for (nint i = 0; i < count; i++)
            {
                sm = artboard.StateMachineFromIndex(i, out _);
                if (sm != null)
                {
                    var names = sm.InputNames;
                    if (names != null && names.Length > 0)
                        return names;
                }
            }
            return [];
        }
        catch { return []; }
    }

    public partial RiveInputInfo[] GetStateMachineInputs()
    {
        try
        {
            using var riveFile = GetRiveFileForCurrentResource();
            if (riveFile == null) return [];
            var artboard = riveFile.GetArtboard(out _);
            if (artboard == null) return [];

            var sm = artboard.DefaultStateMachine;
            if (sm == null)
            {
                var count = artboard.StateMachineCount;
                for (nint i = 0; i < count && sm == null; i++)
                    sm = artboard.StateMachineFromIndex(i, out _);
            }
            if (sm == null) return [];

            var names = sm.InputNames;
            if (names == null || names.Length == 0) return [];

            var result = new List<RiveInputInfo>();
            foreach (var name in names)
            {
                try
                {
                    if (sm.GetTrigger(name) != null) { result.Add(new(name, RiveInputType.Trigger)); continue; }
                }
                catch { }
                try
                {
                    if (sm.GetBool(name) != null) { result.Add(new(name, RiveInputType.Boolean)); continue; }
                }
                catch { }
                try
                {
                    if (sm.GetNumber(name) != null) { result.Add(new(name, RiveInputType.Number)); continue; }
                }
                catch { }
                result.Add(new(name, RiveInputType.Trigger)); // fallback
            }
            return [.. result];
        }
        catch { return []; }
    }

    private RiveFile? GetRiveFileForCurrentResource()
    {
        var resourceName = VirtualView?.ResourceName;
        if (string.IsNullOrEmpty(resourceName)) return null;
        try
        {
            return new RiveFile(resourceName!, true, out _);
        }
        catch { return null; }
    }

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
        if (handler._hostView == null) return;
        handler._hostView.Reload(
            view.ResourceName, view.Url,
            view.StateMachineName, view.ArtboardName,
            view.AutoPlay,
            MapFitToNative(view.Fit), MapAlignmentToNative(view.RiveAlignment));
    }

    public static void MapUrl(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (handler._hostView == null) return;
        handler._hostView.Reload(
            view.ResourceName, view.Url,
            view.StateMachineName, view.ArtboardName,
            view.AutoPlay,
            MapFitToNative(view.Fit), MapAlignmentToNative(view.RiveAlignment));
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
        if (args is RiveBytesArgs bytesArgs && handler._hostView != null)
        {
            try
            {
                handler._hostView.ReloadWithBytes(
                    bytesArgs.Bytes,
                    bytesArgs.StateMachineName,
                    bytesArgs.ArtboardName,
                    view.AutoPlay,
                    MapFitToNative(view.Fit),
                    MapAlignmentToNative(view.RiveAlignment));

                handler._wireRetryCount = 0;
                handler.WireDelegatesWhenReady();
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
