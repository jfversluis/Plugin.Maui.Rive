using System.Collections.Concurrent;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using RiveSharp;

namespace Plugin.Maui.Rive;

public partial class RiveAnimationViewHandler : ViewHandler<IRiveAnimationView, SKXamlCanvas>
{
    private readonly Scene _scene = new();
    private readonly ConcurrentQueue<Action> _sceneActions = new();
    private byte[]? _fileData;
    private bool _isPlaying;
    private DateTime? _lastPaintTime;
    private DispatcherTimer? _timer;
    private Mat2D _lastAlignmentMatrix;
    private int _lastCanvasWidth;
    private int _lastCanvasHeight;

    protected override SKXamlCanvas CreatePlatformView()
    {
        return new SKXamlCanvas();
    }

    protected override void ConnectHandler(SKXamlCanvas platformView)
    {
        base.ConnectHandler(platformView);
        platformView.PaintSurface += OnPaintSurface;
        platformView.PointerPressed += OnPointerPressed;
        platformView.PointerMoved += OnPointerMoved;
        platformView.PointerReleased += OnPointerReleased;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
        _timer.Tick += (_, _) => platformView.Invalidate();
        // Don't start timer until content is loaded and AutoPlay is checked
    }

    protected override void DisconnectHandler(SKXamlCanvas platformView)
    {
        _timer?.Stop();
        _timer = null;
        _isPlaying = false;
        platformView.PaintSurface -= OnPaintSurface;
        platformView.PointerPressed -= OnPointerPressed;
        platformView.PointerMoved -= OnPointerMoved;
        platformView.PointerReleased -= OnPointerReleased;
        _lastPaintTime = null;
        _fileData = null;
        // Drain any pending actions to release closures
        while (_sceneActions.TryDequeue(out _)) { }
        base.DisconnectHandler(platformView);
    }

    private Vec2D TransformPointerToScene(PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(PlatformView).Position;
        // Scale from DIPs to pixel coordinates (SKXamlCanvas renders at pixel resolution)
        var scale = PlatformView.XamlRoot?.RasterizationScale ?? 1.0;
        float px = (float)(pos.X * scale);
        float py = (float)(pos.Y * scale);

        // Apply the inverse of the alignment matrix to go from canvas to scene coordinates
        if (_lastAlignmentMatrix.Invert(out var inverse))
        {
            var scenePos = inverse * new Vec2D(px, py);
            return scenePos;
        }
        return new Vec2D(px, py);
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!_scene.IsLoaded) return;
        var pos = TransformPointerToScene(e);
        _sceneActions.Enqueue(() => _scene.PointerDown(pos));
        (sender as UIElement)?.CapturePointer(e.Pointer);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_scene.IsLoaded) return;
        var pos = TransformPointerToScene(e);
        _sceneActions.Enqueue(() => _scene.PointerMove(pos));
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_scene.IsLoaded) return;
        var pos = TransformPointerToScene(e);
        _sceneActions.Enqueue(() => _scene.PointerUp(pos));
        (sender as UIElement)?.ReleasePointerCapture(e.Pointer);
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        while (_sceneActions.TryDequeue(out var action))
        {
            action();
        }

        if (!_scene.IsLoaded) return;

        var now = DateTime.Now;
        if (_lastPaintTime is not null)
        {
            _scene.AdvanceAndApply((now - _lastPaintTime.Value).TotalSeconds);
            ProcessStateChanges();
            ProcessReportedEvents();
        }
        _lastPaintTime = now;

        e.Surface.Canvas.Clear();
        var alignment = ComputeAlignment(e.Info.Width, e.Info.Height);
        _lastAlignmentMatrix = alignment;
        _lastCanvasWidth = e.Info.Width;
        _lastCanvasHeight = e.Info.Height;
        var renderer = new RiveSharp.Renderer(e.Surface.Canvas);
        renderer.Save();
        renderer.Transform(alignment);
        _scene.Draw(renderer);
        renderer.Restore();
    }

    private void ProcessStateChanges()
    {
        int count = _scene.StateChangedCount;
        if (count > 0 && VirtualView != null)
        {
            var smName = _scene.Name;
            for (int i = 0; i < count; i++)
            {
                var stateName = _scene.GetStateChangedName(i);
                VirtualView.OnStateChanged(new RiveStateChangedEventArgs(smName, stateName));
            }
        }
    }

    private void ProcessReportedEvents()
    {
        int count = _scene.ReportedEventCount;
        if (count > 0 && VirtualView != null)
        {
            for (int i = 0; i < count; i++)
            {
                var eventName = _scene.GetReportedEventName(i);
                VirtualView.OnRiveEventReceived(new RiveEventReceivedEventArgs(eventName));
            }
        }
    }

    private Mat2D ComputeAlignment(double width, double height)
    {
        var fit = MapFitToNative(VirtualView?.Fit ?? RiveFitMode.Contain);
        var alignment = MapAlignmentToNative(VirtualView?.RiveAlignment ?? RiveAlignmentMode.Center);
        return RiveSharp.Renderer.ComputeAlignment(
            fit, alignment,
            new AABB(0, 0, (float)width, (float)height),
            new AABB(0, 0, _scene.Width, _scene.Height));
    }

    // --- Loading ---

    private void LoadRiveContent()
    {
        var virtualView = VirtualView;
        if (virtualView == null) return;

        if (!string.IsNullOrEmpty(virtualView.ResourceName))
        {
            LoadFromResourceAsync(virtualView);
        }
        else if (!string.IsNullOrEmpty(virtualView.Url))
        {
            LoadFromUrlAsync(virtualView);
        }
    }

    private async void LoadFromResourceAsync(IRiveAnimationView view)
    {
        try
        {
            var fileName = view.ResourceName + ".riv";
            using var stream = await Microsoft.Maui.Storage.FileSystem.OpenAppPackageFileAsync(fileName);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            if (PlatformView == null) return; // handler disconnected during await
            _fileData = ms.ToArray();
            _sceneActions.Enqueue(() => UpdateScene(view));
            PlatformView?.Invalidate(); // ensure at least one paint to process the action
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Error loading resource: {ex}");
        }
    }

    private async void LoadFromUrlAsync(IRiveAnimationView view)
    {
        try
        {
            using var http = new HttpClient();
            _fileData = await http.GetByteArrayAsync(view.Url);
            if (PlatformView == null) return; // handler disconnected during await
            _sceneActions.Enqueue(() => UpdateScene(view));
            PlatformView?.Invalidate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Error loading URL: {ex}");
        }
    }

    private void StartPlayback()
    {
        _isPlaying = true;
        _lastPaintTime = null;
        _timer?.Start();
    }

    private void StopPlayback()
    {
        _isPlaying = false;
        _timer?.Stop();
    }

    private void UpdateScene(IRiveAnimationView view)
    {
        if (_fileData == null) return;

        _scene.LoadFile(_fileData);

        if (!string.IsNullOrEmpty(view.ArtboardName))
            _scene.LoadArtboard(view.ArtboardName);
        else
            _scene.LoadArtboard(null!);

        if (!string.IsNullOrEmpty(view.StateMachineName))
        {
            _scene.LoadStateMachine(view.StateMachineName);
        }
        else if (!string.IsNullOrEmpty(view.AnimationName))
        {
            _scene.LoadAnimation(view.AnimationName);
        }
        else
        {
            // Auto-detect: try state machine first, fall back to animation
            if (!_scene.LoadStateMachine(null!))
            {
                _scene.LoadAnimation(null!);
            }
        }

        _lastPaintTime = null;

        if (view.AutoPlay)
        {
            StartPlayback();
            VirtualView?.OnPlaybackStarted(new RivePlaybackEventArgs(_scene.Name));
        }
    }

    // --- Fit/Alignment mapping ---

    private static RiveSharp.Fit MapFitToNative(RiveFitMode fit) => fit switch
    {
        RiveFitMode.Fill => RiveSharp.Fit.Fill,
        RiveFitMode.Contain => RiveSharp.Fit.Contain,
        RiveFitMode.Cover => RiveSharp.Fit.Cover,
        RiveFitMode.FitHeight => RiveSharp.Fit.FitHeight,
        RiveFitMode.FitWidth => RiveSharp.Fit.FitWidth,
        RiveFitMode.ScaleDown => RiveSharp.Fit.ScaleDown,
        RiveFitMode.NoFit => RiveSharp.Fit.None,
        RiveFitMode.Layout => RiveSharp.Fit.Layout,
        _ => RiveSharp.Fit.Contain,
    };

    private static RiveSharp.Alignment MapAlignmentToNative(RiveAlignmentMode alignment) => alignment switch
    {
        RiveAlignmentMode.TopLeft => RiveSharp.Alignment.TopLeft,
        RiveAlignmentMode.TopCenter => RiveSharp.Alignment.TopCenter,
        RiveAlignmentMode.TopRight => RiveSharp.Alignment.TopRight,
        RiveAlignmentMode.CenterLeft => RiveSharp.Alignment.CenterLeft,
        RiveAlignmentMode.Center => RiveSharp.Alignment.Center,
        RiveAlignmentMode.CenterRight => RiveSharp.Alignment.CenterRight,
        RiveAlignmentMode.BottomLeft => RiveSharp.Alignment.BottomLeft,
        RiveAlignmentMode.BottomCenter => RiveSharp.Alignment.BottomCenter,
        RiveAlignmentMode.BottomRight => RiveSharp.Alignment.BottomRight,
        _ => RiveSharp.Alignment.Center,
    };

    // --- Introspection ---

    public partial string? GetTextRunValue(string textRunName) =>
        _scene.IsLoaded ? _scene.GetTextRunValue(textRunName) : null;

    public partial string? GetTextRunValueAtPath(string textRunName, string path) =>
        _scene.IsLoaded ? _scene.GetTextRunValue(textRunName, path) : null;

    public partial string[] GetArtboardNames()
    {
        return _scene.IsLoaded ? _scene.GetArtboardNames() : [];
    }

    public partial string[] GetAnimationNames()
    {
        return _scene.IsLoaded ? _scene.GetAnimationNames() : [];
    }

    public partial string[] GetStateMachineNames()
    {
        return _scene.IsLoaded ? _scene.GetStateMachineNames() : [];
    }

    public partial string[] GetStateMachineInputNames()
    {
        if (!_scene.IsLoaded) return [];
        int count = _scene.InputCount;
        var names = new string[count];
        for (int i = 0; i < count; i++)
            names[i] = _scene.GetInputName(i);
        return names;
    }

    public partial RiveInputInfo[] GetStateMachineInputs()
    {
        if (!_scene.IsLoaded) return [];
        int count = _scene.InputCount;
        var result = new RiveInputInfo[count];
        for (int i = 0; i < count; i++)
        {
            var name = _scene.GetInputName(i);
            var nativeType = _scene.GetInputType(i);
            var type = nativeType switch
            {
                1 => RiveInputType.Boolean,
                2 => RiveInputType.Number,
                _ => RiveInputType.Trigger,
            };
            result[i] = new RiveInputInfo(name, type);
        }
        return result;
    }

    // --- Property Mappers ---

    public static void MapResourceName(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        handler.LoadRiveContent();
    }

    public static void MapUrl(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        handler.LoadRiveContent();
    }

    public static void MapAutoPlay(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        if (view.AutoPlay && !handler._isPlaying && handler._scene.IsLoaded)
        {
            handler.StartPlayback();
        }
        else if (!view.AutoPlay && handler._isPlaying)
        {
            handler.StopPlayback();
        }
    }

    public static void MapFit(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        handler.PlatformView?.Invalidate();
    }

    public static void MapAlignment(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        handler.PlatformView?.Invalidate();
    }

    public static void MapLayoutScaleFactor(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        handler.PlatformView?.Invalidate();
    }

    // --- Command Mappers ---

    public static void MapPlay(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RivePlayArgs playArgs && !string.IsNullOrEmpty(playArgs.AnimationName))
        {
            // Load specific animation by name
            handler._sceneActions.Enqueue(() =>
            {
                if (handler._fileData == null) return;
                // Reload the scene with the specified animation
                handler._scene.LoadFile(handler._fileData);
                handler._scene.LoadArtboard(view.ArtboardName ?? null!);

                if (playArgs.Loop == RiveLoopMode.Auto || playArgs.Loop == RiveLoopMode.Loop)
                {
                    // Try state machine first for auto/loop
                    if (!handler._scene.LoadStateMachine(playArgs.AnimationName))
                        handler._scene.LoadAnimation(playArgs.AnimationName);
                }
                else
                {
                    handler._scene.LoadAnimation(playArgs.AnimationName);
                }
                handler._lastPaintTime = null;
            });
        }

        handler.StartPlayback();
        view.OnPlaybackStarted(new RivePlaybackEventArgs(
            args is RivePlayArgs pa ? pa.AnimationName : null));
    }

    public static void MapPause(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler.StopPlayback();
        view.OnPlaybackPaused(new RivePlaybackEventArgs(null));
    }

    public static void MapStop(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler.StopPlayback();
        handler._lastPaintTime = null;
        view.OnPlaybackStopped(new RivePlaybackEventArgs(null));
    }

    public static void MapReset(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._sceneActions.Enqueue(() =>
        {
            if (handler._fileData != null)
                handler.UpdateScene(view);
        });
    }

    public static void MapFireTrigger(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is string triggerName)
        {
            handler._sceneActions.Enqueue(() =>
            {
                try { handler._scene.FireTrigger(triggerName); } catch { }
            });
        }
    }

    public static void MapSetBoolInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBoolInput input)
        {
            handler._sceneActions.Enqueue(() =>
            {
                try { handler._scene.SetBool(input.Name, input.Value); } catch { }
            });
        }
    }

    public static void MapSetNumberInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveNumberInput input)
        {
            handler._sceneActions.Enqueue(() =>
            {
                try { handler._scene.SetNumber(input.Name, input.Value); } catch { }
            });
        }
    }

    public static void MapFireTriggerAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTriggerAtPath input)
        {
            handler._sceneActions.Enqueue(() =>
            {
                try { handler._scene.FireTrigger(input.InputName); } catch { }
            });
        }
    }

    public static void MapSetBoolInputAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBoolInputAtPath input)
        {
            handler._sceneActions.Enqueue(() =>
            {
                try { handler._scene.SetBool(input.InputName, input.Value); } catch { }
            });
        }
    }

    public static void MapSetNumberInputAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveNumberInputAtPath input)
        {
            handler._sceneActions.Enqueue(() =>
            {
                try { handler._scene.SetNumber(input.InputName, input.Value); } catch { }
            });
        }
    }

    public static void MapSetTextRunValue(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTextRun textRun)
        {
            handler._sceneActions.Enqueue(() =>
            {
                try { handler._scene.SetTextRunValue(textRun.TextRunName, textRun.TextValue); } catch { }
            });
        }
    }

    public static void MapSetTextRunValueAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTextRun textRun && textRun.Path is string path)
        {
            handler._sceneActions.Enqueue(() =>
            {
                try { handler._scene.SetTextRunValue(textRun.TextRunName, textRun.TextValue, path); } catch { }
            });
        }
    }

    public static void MapSetRiveBytes(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBytesArgs bytesArgs)
        {
            handler._fileData = bytesArgs.Bytes;
            handler._sceneActions.Enqueue(() =>
            {
                if (handler._fileData == null) return;
                handler._scene.LoadFile(handler._fileData);

                var artboard = bytesArgs.ArtboardName;
                handler._scene.LoadArtboard(string.IsNullOrEmpty(artboard) ? null! : artboard);

                if (!string.IsNullOrEmpty(bytesArgs.StateMachineName))
                    handler._scene.LoadStateMachine(bytesArgs.StateMachineName);
                else if (!string.IsNullOrEmpty(bytesArgs.AnimationName))
                    handler._scene.LoadAnimation(bytesArgs.AnimationName);
                else if (!handler._scene.LoadStateMachine(null!))
                    handler._scene.LoadAnimation(null!);

                handler._lastPaintTime = null;
                if (view.AutoPlay)
                {
                    handler.StartPlayback();
                    handler.VirtualView?.OnPlaybackStarted(new RivePlaybackEventArgs(handler._scene.Name));
                }
            });
            handler.PlatformView?.Invalidate();
        }
    }
}
