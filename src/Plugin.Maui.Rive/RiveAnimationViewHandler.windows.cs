using System.Collections.Concurrent;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using RiveSharp;

namespace Plugin.Maui.Rive;

public partial class RiveAnimationViewHandler : ViewHandler<IRiveAnimationView, SKXamlCanvas>
{
    private readonly Scene _scene = new();
    private readonly ConcurrentQueue<Action> _sceneActions = new();
    private byte[]? _fileData;
    private bool _contentLoaded;
    private DateTime? _lastPaintTime;
    private DispatcherTimer? _timer;

    protected override SKXamlCanvas CreatePlatformView()
    {
        return new SKXamlCanvas();
    }

    protected override void ConnectHandler(SKXamlCanvas platformView)
    {
        base.ConnectHandler(platformView);
        platformView.PaintSurface += OnPaintSurface;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
        _timer.Tick += (_, _) => platformView.Invalidate();
        _timer.Start();
    }

    protected override void DisconnectHandler(SKXamlCanvas platformView)
    {
        _timer?.Stop();
        _timer = null;
        platformView.PaintSurface -= OnPaintSurface;
        _contentLoaded = false;
        _lastPaintTime = null;
        base.DisconnectHandler(platformView);
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
        }
        _lastPaintTime = now;

        e.Surface.Canvas.Clear();
        var renderer = new RiveSharp.Renderer(e.Surface.Canvas);
        renderer.Save();
        renderer.Transform(ComputeAlignment(e.Info.Width, e.Info.Height));
        _scene.Draw(renderer);
        renderer.Restore();
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
            _fileData = ms.ToArray();
            _sceneActions.Enqueue(() => UpdateScene(view));
            _contentLoaded = true;
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
            _sceneActions.Enqueue(() => UpdateScene(view));
            _contentLoaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Error loading URL: {ex}");
        }
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

    public partial string? GetTextRunValue(string textRunName) => null;
    public partial string? GetTextRunValueAtPath(string textRunName, string path) => null;

    public partial string[] GetArtboardNames()
    {
        // rive-sharp Scene API doesn't expose artboard enumeration directly;
        // the native interop only has LoadArtboard by name.
        return _scene.IsLoaded ? [_scene.Name] : [];
    }

    public partial string[] GetAnimationNames() => [];
    public partial string[] GetStateMachineNames() => [];
    public partial string[] GetStateMachineInputNames() => [];

    public partial RiveInputInfo[] GetStateMachineInputs() => [];

    // --- Property Mappers ---

    public static void MapResourceName(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        handler._contentLoaded = false;
        handler.LoadRiveContent();
    }

    public static void MapUrl(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        handler._contentLoaded = false;
        handler.LoadRiveContent();
    }

    public static void MapAutoPlay(RiveAnimationViewHandler handler, IRiveAnimationView view) { }
    public static void MapFit(RiveAnimationViewHandler handler, IRiveAnimationView view) { }
    public static void MapAlignment(RiveAnimationViewHandler handler, IRiveAnimationView view) { }
    public static void MapLayoutScaleFactor(RiveAnimationViewHandler handler, IRiveAnimationView view) { }

    // --- Command Mappers ---

    public static void MapPlay(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        // Scene is always advancing when loaded; play is implicit
        view.OnPlaybackStarted(new RivePlaybackEventArgs(null));
    }

    public static void MapPause(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._timer?.Stop();
        view.OnPlaybackPaused(new RivePlaybackEventArgs(null));
    }

    public static void MapStop(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler._timer?.Stop();
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

    public static void MapSetTextRunValue(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args) { }
    public static void MapSetTextRunValueAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args) { }

    public static void MapSetRiveBytes(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is byte[] bytes)
        {
            handler._fileData = bytes;
            handler._sceneActions.Enqueue(() => handler.UpdateScene(view));
            handler._contentLoaded = true;
        }
    }
}
