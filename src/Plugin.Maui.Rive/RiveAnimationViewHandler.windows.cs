using System.Text;
using System.Text.Json;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Plugin.Maui.Rive;

public partial class RiveAnimationViewHandler : ViewHandler<IRiveAnimationView, WebView2>
{
    private bool _pageReady;
    private bool _contentLoaded;
    private readonly Queue<string> _pendingScripts = new();

    protected override WebView2 CreatePlatformView()
    {
        var webView = new WebView2
        {
            DefaultBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0)
        };
        return webView;
    }

    protected override void ConnectHandler(WebView2 platformView)
    {
        base.ConnectHandler(platformView);
        InitializeWebViewAsync(platformView);
    }

    protected override void DisconnectHandler(WebView2 platformView)
    {
        if (platformView.CoreWebView2 != null)
        {
            platformView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
        }
        _pageReady = false;
        _contentLoaded = false;
        base.DisconnectHandler(platformView);
    }

    private async void InitializeWebViewAsync(WebView2 webView)
    {
        try
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            _pageReady = false;
            webView.NavigateToString(GetRiveHtml());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] WebView2 init failed: {ex}");
        }
    }

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            var json = args.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(json)) return;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            switch (type)
            {
                case "ready":
                    _pageReady = true;
                    FlushPendingScripts();
                    if (!_contentLoaded) LoadRiveContent();
                    break;

                case "playbackStarted":
                    VirtualView?.OnPlaybackStarted(new RivePlaybackEventArgs(
                        root.TryGetProperty("animationName", out var an) ? an.GetString() : null));
                    break;

                case "playbackPaused":
                    VirtualView?.OnPlaybackPaused(new RivePlaybackEventArgs(
                        root.TryGetProperty("animationName", out var pn) ? pn.GetString() : null));
                    break;

                case "playbackStopped":
                    VirtualView?.OnPlaybackStopped(new RivePlaybackEventArgs(
                        root.TryGetProperty("animationName", out var sn) ? sn.GetString() : null));
                    break;

                case "playbackLooped":
                    VirtualView?.OnPlaybackLooped(new RivePlaybackEventArgs(
                        root.TryGetProperty("animationName", out var ln) ? ln.GetString() : null));
                    break;

                case "stateChanged":
                    VirtualView?.OnStateChanged(new RiveStateChangedEventArgs(
                        root.GetProperty("stateMachineName").GetString() ?? "",
                        root.GetProperty("stateName").GetString() ?? ""));
                    break;

                case "riveEvent":
                    var props = new Dictionary<string, object>();
                    if (root.TryGetProperty("properties", out var propsEl))
                    {
                        foreach (var prop in propsEl.EnumerateObject())
                        {
                            props[prop.Name] = prop.Value.ValueKind switch
                            {
                                JsonValueKind.Number => prop.Value.GetSingle(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                _ => prop.Value.GetString() ?? ""
                            };
                        }
                    }
                    VirtualView?.OnRiveEventReceived(new RiveEventReceivedEventArgs(
                        root.GetProperty("name").GetString() ?? "",
                        root.TryGetProperty("delay", out var d) ? d.GetSingle() : 0f,
                        props));
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Message parse error: {ex}");
        }
    }

    private async void ExecuteScript(string script)
    {
        if (!_pageReady)
        {
            _pendingScripts.Enqueue(script);
            return;
        }

        try
        {
            if (PlatformView?.CoreWebView2 != null)
                await PlatformView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Script error: {ex}");
        }
    }

    private async Task<string?> ExecuteScriptWithResultAsync(string script)
    {
        if (!_pageReady || PlatformView?.CoreWebView2 == null) return null;

        try
        {
            var result = await PlatformView.CoreWebView2.ExecuteScriptAsync(script);
            if (result == "null" || result == "undefined") return null;
            // Remove surrounding quotes from JSON string result
            if (result.StartsWith('"') && result.EndsWith('"'))
                return JsonSerializer.Deserialize<string>(result);
            return result;
        }
        catch { return null; }
    }

    private void FlushPendingScripts()
    {
        while (_pendingScripts.Count > 0)
        {
            var script = _pendingScripts.Dequeue();
            ExecuteScript(script);
        }
    }

    private void LoadRiveContent()
    {
        var virtualView = VirtualView;
        if (virtualView == null) return;

        var fit = MapFitToJs(virtualView.Fit);
        var alignment = MapAlignmentToJs(virtualView.RiveAlignment);

        if (!string.IsNullOrEmpty(virtualView.ResourceName))
        {
            LoadFromResourceAsync(virtualView, fit, alignment);
            _contentLoaded = true;
        }
        else if (!string.IsNullOrEmpty(virtualView.Url))
        {
            LoadFromUrl(virtualView, fit, alignment);
            _contentLoaded = true;
        }
    }

    private async void LoadFromResourceAsync(IRiveAnimationView virtualView, string fit, string alignment)
    {
        try
        {
            var fileName = virtualView.ResourceName + ".riv";
            using var stream = await Microsoft.Maui.Storage.FileSystem.OpenAppPackageFileAsync(fileName);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());

            var config = BuildRiveConfig(virtualView, fit, alignment);
            ExecuteScript($"loadRiveFromBase64('{base64}', {config});");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Plugin.Maui.Rive] Error loading resource: {ex}");
        }
    }

    private void LoadFromUrl(IRiveAnimationView virtualView, string fit, string alignment)
    {
        var config = BuildRiveConfig(virtualView, fit, alignment);
        var escapedUrl = JsonSerializer.Serialize(virtualView.Url);
        ExecuteScript($"loadRiveFromUrl({escapedUrl}, {config});");
    }

    private static string BuildRiveConfig(IRiveAnimationView view, string fit, string alignment)
    {
        var sb = new StringBuilder("{");
        sb.Append($"autoplay:{(view.AutoPlay ? "true" : "false")}");
        sb.Append($",fit:'{fit}'");
        sb.Append($",alignment:'{alignment}'");
        if (!string.IsNullOrEmpty(view.ArtboardName))
            sb.Append($",artboard:{JsonSerializer.Serialize(view.ArtboardName)}");
        if (!string.IsNullOrEmpty(view.StateMachineName))
            sb.Append($",stateMachines:{JsonSerializer.Serialize(view.StateMachineName)}");
        if (!string.IsNullOrEmpty(view.AnimationName))
            sb.Append($",animations:{JsonSerializer.Serialize(view.AnimationName)}");
        sb.Append('}');
        return sb.ToString();
    }

    // --- JS Enum Mapping ---

    private static string MapFitToJs(RiveFitMode fit) => fit switch
    {
        RiveFitMode.Fill => "fill",
        RiveFitMode.Contain => "contain",
        RiveFitMode.Cover => "cover",
        RiveFitMode.FitHeight => "fitHeight",
        RiveFitMode.FitWidth => "fitWidth",
        RiveFitMode.ScaleDown => "scaleDown",
        RiveFitMode.NoFit => "none",
        RiveFitMode.Layout => "layout",
        _ => "contain",
    };

    private static string MapAlignmentToJs(RiveAlignmentMode alignment) => alignment switch
    {
        RiveAlignmentMode.TopLeft => "topLeft",
        RiveAlignmentMode.TopCenter => "topCenter",
        RiveAlignmentMode.TopRight => "topRight",
        RiveAlignmentMode.CenterLeft => "centerLeft",
        RiveAlignmentMode.Center => "center",
        RiveAlignmentMode.CenterRight => "centerRight",
        RiveAlignmentMode.BottomLeft => "bottomLeft",
        RiveAlignmentMode.BottomCenter => "bottomCenter",
        RiveAlignmentMode.BottomRight => "bottomRight",
        _ => "center",
    };

    private static string MapLoopToJs(RiveLoopMode loop) => loop switch
    {
        RiveLoopMode.OneShot => "oneShot",
        RiveLoopMode.Loop => "loop",
        RiveLoopMode.PingPong => "pingPong",
        _ => "auto",
    };

    private static string MapDirectionToJs(RiveDirectionMode dir) => dir switch
    {
        RiveDirectionMode.Backwards => "backwards",
        RiveDirectionMode.Forwards => "forwards",
        _ => "auto",
    };

    // --- Introspection (synchronous-ish via blocking) ---

    public partial string? GetTextRunValue(string textRunName)
    {
        if (!_pageReady) return null;
        var escaped = JsonSerializer.Serialize(textRunName);
        return RunScriptSync($"riveGetTextRunValue({escaped})");
    }

    public partial string? GetTextRunValueAtPath(string textRunName, string path)
    {
        if (!_pageReady) return null;
        var escapedName = JsonSerializer.Serialize(textRunName);
        var escapedPath = JsonSerializer.Serialize(path);
        return RunScriptSync($"riveGetTextRunValueAtPath({escapedName}, {escapedPath})");
    }

    public partial string[] GetArtboardNames()
    {
        var result = RunScriptSync("riveGetArtboardNames()");
        return ParseJsonStringArray(result);
    }

    public partial string[] GetAnimationNames()
    {
        var result = RunScriptSync("riveGetAnimationNames()");
        return ParseJsonStringArray(result);
    }

    public partial string[] GetStateMachineNames()
    {
        var result = RunScriptSync("riveGetStateMachineNames()");
        return ParseJsonStringArray(result);
    }

    public partial string[] GetStateMachineInputNames()
    {
        var result = RunScriptSync("riveGetStateMachineInputNames()");
        return ParseJsonStringArray(result);
    }

    public partial RiveInputInfo[] GetStateMachineInputs()
    {
        var result = RunScriptSync("riveGetStateMachineInputs()");
        if (string.IsNullOrEmpty(result) || result == "null") return [];

        try
        {
            using var doc = JsonDocument.Parse(result);
            var list = new List<RiveInputInfo>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var name = item.GetProperty("name").GetString() ?? "";
                var typeStr = item.GetProperty("type").GetString() ?? "";
                var type = typeStr switch
                {
                    "trigger" => RiveInputType.Trigger,
                    "boolean" => RiveInputType.Boolean,
                    "number" => RiveInputType.Number,
                    _ => RiveInputType.Number
                };
                list.Add(new RiveInputInfo(name, type));
            }
            return [.. list];
        }
        catch { return []; }
    }

    private string? RunScriptSync(string script)
    {
        if (!_pageReady || PlatformView?.CoreWebView2 == null) return null;
        try
        {
            var task = PlatformView.CoreWebView2.ExecuteScriptAsync(script).AsTask();
            task.Wait(TimeSpan.FromSeconds(2));
            if (!task.IsCompletedSuccessfully) return null;
            var result = task.Result;
            if (result == "null" || result == "undefined") return null;
            return result;
        }
        catch { return null; }
    }

    private static string[] ParseJsonStringArray(string? json)
    {
        if (string.IsNullOrEmpty(json) || json == "null" || json == "[]") return [];
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch { return []; }
    }

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

    public static void MapAutoPlay(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        // AutoPlay is applied during load; runtime changes are not typically supported
    }

    public static void MapFit(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        var fit = MapFitToJs(view.Fit);
        handler.ExecuteScript($"riveSetFit('{fit}');");
    }

    public static void MapAlignment(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        var alignment = MapAlignmentToJs(view.RiveAlignment);
        handler.ExecuteScript($"riveSetAlignment('{alignment}');");
    }

    public static void MapLayoutScaleFactor(RiveAnimationViewHandler handler, IRiveAnimationView view)
    {
        // Layout scale factor is primarily an Android concept; no-op on Windows
    }

    // --- Command Mappers ---

    public static void MapPlay(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RivePlayArgs playArgs)
        {
            var animName = playArgs.AnimationName != null ? JsonSerializer.Serialize(playArgs.AnimationName) : "null";
            var loop = MapLoopToJs(playArgs.Loop);
            var dir = MapDirectionToJs(playArgs.Direction);
            handler.ExecuteScript($"rivePlay({animName}, '{loop}', '{dir}');");
        }
        else
        {
            handler.ExecuteScript("rivePlay(null, 'auto', 'auto');");
        }
        view.IsPlaying = true;
    }

    public static void MapPause(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler.ExecuteScript("rivePause();");
        view.IsPlaying = false;
    }

    public static void MapStop(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler.ExecuteScript("riveStop();");
        view.IsPlaying = false;
    }

    public static void MapReset(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        handler.ExecuteScript("riveReset();");
    }

    public static void MapFireTrigger(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is string triggerName)
        {
            var escaped = JsonSerializer.Serialize(triggerName);
            handler.ExecuteScript($"riveFireTrigger({escaped});");
        }
    }

    public static void MapSetBoolInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBoolInput input)
        {
            var name = JsonSerializer.Serialize(input.Name);
            handler.ExecuteScript($"riveSetBoolInput({name}, {(input.Value ? "true" : "false")});");
        }
    }

    public static void MapSetNumberInput(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveNumberInput input)
        {
            var name = JsonSerializer.Serialize(input.Name);
            handler.ExecuteScript($"riveSetNumberInput({name}, {input.Value});");
        }
    }

    public static void MapFireTriggerAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTriggerAtPath input)
        {
            var name = JsonSerializer.Serialize(input.InputName);
            var path = JsonSerializer.Serialize(input.Path);
            handler.ExecuteScript($"riveFireTriggerAtPath({name}, {path});");
        }
    }

    public static void MapSetBoolInputAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBoolInputAtPath input)
        {
            var name = JsonSerializer.Serialize(input.InputName);
            var path = JsonSerializer.Serialize(input.Path);
            handler.ExecuteScript($"riveSetBoolInputAtPath({name}, {(input.Value ? "true" : "false")}, {path});");
        }
    }

    public static void MapSetNumberInputAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveNumberInputAtPath input)
        {
            var name = JsonSerializer.Serialize(input.InputName);
            var path = JsonSerializer.Serialize(input.Path);
            handler.ExecuteScript($"riveSetNumberInputAtPath({name}, {input.Value}, {path});");
        }
    }

    public static void MapSetTextRunValue(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTextRun textRun)
        {
            var name = JsonSerializer.Serialize(textRun.TextRunName);
            var value = JsonSerializer.Serialize(textRun.TextValue);
            handler.ExecuteScript($"riveSetTextRunValue({name}, {value});");
        }
    }

    public static void MapSetTextRunValueAtPath(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveTextRun textRun && textRun.Path is string path)
        {
            var name = JsonSerializer.Serialize(textRun.TextRunName);
            var value = JsonSerializer.Serialize(textRun.TextValue);
            var pathJs = JsonSerializer.Serialize(path);
            handler.ExecuteScript($"riveSetTextRunValueAtPath({name}, {value}, {pathJs});");
        }
    }

    public static void MapSetRiveBytes(RiveAnimationViewHandler handler, IRiveAnimationView view, object? args)
    {
        if (args is RiveBytesArgs bytesArgs)
        {
            var base64 = Convert.ToBase64String(bytesArgs.Bytes);
            var fit = MapFitToJs(view.Fit);
            var alignment = MapAlignmentToJs(view.RiveAlignment);

            var sb = new StringBuilder("{");
            sb.Append($"autoplay:{(view.AutoPlay ? "true" : "false")}");
            sb.Append($",fit:'{fit}'");
            sb.Append($",alignment:'{alignment}'");
            if (!string.IsNullOrEmpty(bytesArgs.ArtboardName))
                sb.Append($",artboard:{JsonSerializer.Serialize(bytesArgs.ArtboardName)}");
            if (!string.IsNullOrEmpty(bytesArgs.StateMachineName))
                sb.Append($",stateMachines:{JsonSerializer.Serialize(bytesArgs.StateMachineName)}");
            if (!string.IsNullOrEmpty(bytesArgs.AnimationName))
                sb.Append($",animations:{JsonSerializer.Serialize(bytesArgs.AnimationName)}");
            sb.Append('}');

            handler.ExecuteScript($"loadRiveFromBase64('{base64}', {sb});");
            handler._contentLoaded = true;
        }
    }

    // --- Embedded HTML ---

    private static string GetRiveHtml() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                html, body { width: 100%; height: 100%; overflow: hidden; background: transparent; }
                canvas { width: 100%; height: 100%; display: block; }
            </style>
        </head>
        <body>
            <canvas id="riveCanvas"></canvas>
            <script src="https://unpkg.com/@rive-app/canvas@2.21.6"></script>
            <script>
                let riveInstance = null;
                const canvas = document.getElementById('riveCanvas');

                function resizeCanvas() {
                    canvas.width = window.innerWidth * window.devicePixelRatio;
                    canvas.height = window.innerHeight * window.devicePixelRatio;
                    canvas.style.width = window.innerWidth + 'px';
                    canvas.style.height = window.innerHeight + 'px';
                }
                window.addEventListener('resize', resizeCanvas);
                resizeCanvas();

                function postMsg(obj) {
                    try { window.chrome.webview.postMessage(JSON.stringify(obj)); } catch(e) {}
                }

                function createRiveCallbacks() {
                    return {
                        onLoad: () => postMsg({type:'loaded'}),
                        onPlay: (e) => postMsg({type:'playbackStarted', animationName: e && e.length > 0 ? e[0] : null}),
                        onPause: (e) => postMsg({type:'playbackPaused', animationName: e && e.length > 0 ? e[0] : null}),
                        onStop: (e) => postMsg({type:'playbackStopped', animationName: e && e.length > 0 ? e[0] : null}),
                        onLoop: (e) => postMsg({type:'playbackLooped', animationName: e && e.length > 0 ? e[0] : null}),
                        onStateChange: (e) => {
                            if (e && e.data) {
                                for (const s of e.data) {
                                    postMsg({type:'stateChanged', stateMachineName: e.type || '', stateName: s});
                                }
                            } else if (e) {
                                postMsg({type:'stateChanged', stateMachineName: '', stateName: String(e)});
                            }
                        },
                        onRiveEvent: (e) => {
                            const evt = e.data || e;
                            postMsg({
                                type:'riveEvent',
                                name: evt.name || '',
                                delay: evt.delay || 0,
                                properties: evt.properties || {}
                            });
                        }
                    };
                }

                function mapFit(f) {
                    const m = {
                        fill: rive.Fit.Fill,
                        contain: rive.Fit.Contain,
                        cover: rive.Fit.Cover,
                        fitWidth: rive.Fit.FitWidth,
                        fitHeight: rive.Fit.FitHeight,
                        scaleDown: rive.Fit.ScaleDown,
                        none: rive.Fit.None,
                        layout: rive.Fit.Layout
                    };
                    return m[f] || rive.Fit.Contain;
                }

                function mapAlignment(a) {
                    const m = {
                        topLeft: rive.Alignment.TopLeft,
                        topCenter: rive.Alignment.TopCenter,
                        topRight: rive.Alignment.TopRight,
                        centerLeft: rive.Alignment.CenterLeft,
                        center: rive.Alignment.Center,
                        centerRight: rive.Alignment.CenterRight,
                        bottomLeft: rive.Alignment.BottomLeft,
                        bottomCenter: rive.Alignment.BottomCenter,
                        bottomRight: rive.Alignment.BottomRight
                    };
                    return m[a] || rive.Alignment.Center;
                }

                function buildConfig(opts) {
                    const cfg = {
                        canvas: canvas,
                        autoplay: opts.autoplay !== false,
                        fit: mapFit(opts.fit || 'contain'),
                        alignment: mapAlignment(opts.alignment || 'center'),
                        ...createRiveCallbacks()
                    };
                    if (opts.artboard) cfg.artboard = opts.artboard;
                    if (opts.stateMachines) cfg.stateMachines = opts.stateMachines;
                    if (opts.animations) cfg.animations = opts.animations;
                    return cfg;
                }

                function loadRiveFromBase64(base64, opts) {
                    if (riveInstance) { try { riveInstance.cleanup(); } catch(e) {} }
                    const binary = atob(base64);
                    const bytes = new Uint8Array(binary.length);
                    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
                    const cfg = buildConfig(opts);
                    cfg.buffer = bytes.buffer;
                    riveInstance = new rive.Rive(cfg);
                }

                function loadRiveFromUrl(url, opts) {
                    if (riveInstance) { try { riveInstance.cleanup(); } catch(e) {} }
                    const cfg = buildConfig(opts);
                    cfg.src = url;
                    riveInstance = new rive.Rive(cfg);
                }

                // Playback
                function rivePlay(animName, loop, direction) {
                    if (!riveInstance) return;
                    riveInstance.play(animName || undefined);
                }
                function rivePause() { if (riveInstance) riveInstance.pause(); }
                function riveStop() { if (riveInstance) riveInstance.stop(); }
                function riveReset() {
                    if (riveInstance) {
                        riveInstance.reset({autoplay: false});
                        resizeCanvas();
                    }
                }

                // Fit & Alignment
                function riveSetFit(f) {
                    if (riveInstance) riveInstance.layout = new rive.Layout({fit: mapFit(f), alignment: riveInstance.layout?.alignment});
                }
                function riveSetAlignment(a) {
                    if (riveInstance) riveInstance.layout = new rive.Layout({fit: riveInstance.layout?.fit, alignment: mapAlignment(a)});
                }

                // State machine inputs
                function getInputByName(name) {
                    if (!riveInstance) return null;
                    const inputs = riveInstance.stateMachineInputs(riveInstance.stateMachineNames?.[0]);
                    if (!inputs) return null;
                    return inputs.find(i => i.name === name) || null;
                }

                function riveFireTrigger(name) {
                    const input = getInputByName(name);
                    if (input) input.fire();
                }
                function riveSetBoolInput(name, value) {
                    const input = getInputByName(name);
                    if (input) input.value = value;
                }
                function riveSetNumberInput(name, value) {
                    const input = getInputByName(name);
                    if (input) input.value = value;
                }

                // Nested artboard inputs (path-based)
                function riveFireTriggerAtPath(name, path) { riveFireTrigger(name); }
                function riveSetBoolInputAtPath(name, value, path) { riveSetBoolInput(name, value); }
                function riveSetNumberInputAtPath(name, value, path) { riveSetNumberInput(name, value); }

                // Text runs
                function riveGetTextRunValue(name) {
                    try { return riveInstance ? riveInstance.getTextRunValue(name) : null; }
                    catch(e) { return null; }
                }
                function riveSetTextRunValue(name, value) {
                    try { if (riveInstance) riveInstance.setTextRunValue(name, value); }
                    catch(e) {}
                }
                function riveGetTextRunValueAtPath(name, path) {
                    try { return riveInstance ? riveInstance.getTextRunValue(name) : null; }
                    catch(e) { return null; }
                }
                function riveSetTextRunValueAtPath(name, value, path) {
                    try { if (riveInstance) riveInstance.setTextRunValue(name, value); }
                    catch(e) {}
                }

                // Introspection
                function riveGetArtboardNames() {
                    try { return riveInstance ? JSON.stringify(riveInstance.artboardNames || []) : '[]'; }
                    catch(e) { return '[]'; }
                }
                function riveGetAnimationNames() {
                    try { return riveInstance ? JSON.stringify(riveInstance.animationNames || []) : '[]'; }
                    catch(e) { return '[]'; }
                }
                function riveGetStateMachineNames() {
                    try { return riveInstance ? JSON.stringify(riveInstance.stateMachineNames || []) : '[]'; }
                    catch(e) { return '[]'; }
                }
                function riveGetStateMachineInputNames() {
                    try {
                        if (!riveInstance) return '[]';
                        const smName = riveInstance.stateMachineNames?.[0];
                        if (!smName) return '[]';
                        const inputs = riveInstance.stateMachineInputs(smName);
                        return JSON.stringify((inputs || []).map(i => i.name));
                    } catch(e) { return '[]'; }
                }
                function riveGetStateMachineInputs() {
                    try {
                        if (!riveInstance) return '[]';
                        const smName = riveInstance.stateMachineNames?.[0];
                        if (!smName) return '[]';
                        const inputs = riveInstance.stateMachineInputs(smName);
                        return JSON.stringify((inputs || []).map(i => ({
                            name: i.name,
                            type: i.type === rive.StateMachineInputType.Trigger ? 'trigger'
                                : i.type === rive.StateMachineInputType.Boolean ? 'boolean'
                                : 'number'
                        })));
                    } catch(e) { return '[]'; }
                }

                // Signal readiness
                if (typeof rive !== 'undefined') {
                    postMsg({type:'ready'});
                } else {
                    document.querySelector('script[src*="rive-app"]').addEventListener('load', () => {
                        postMsg({type:'ready'});
                    });
                }
            </script>
        </body>
        </html>
        """;
}
