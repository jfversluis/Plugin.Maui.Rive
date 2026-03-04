using System.Runtime.InteropServices;

namespace Plugin.Maui.Rive.Sample;

public partial class MainPage : ContentPage
{
public MainPage()
{
InitializeComponent();
}

void OnPlayClicked(object? sender, EventArgs e) => riveView.Play();
void OnPauseClicked(object? sender, EventArgs e) => riveView.Pause();
void OnStopClicked(object? sender, EventArgs e) => riveView.Stop();
void OnResetClicked(object? sender, EventArgs e) => riveView.Reset();

#if IOS
[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
static extern void void_objc_msgSend_bool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool arg);

[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
[return: MarshalAs(UnmanagedType.I1)]
static extern bool bool_objc_msgSend(IntPtr receiver, IntPtr selector);

[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
static extern void void_objc_msgSend_CGSize(IntPtr receiver, IntPtr selector, CoreGraphics.CGSize size);

[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
static extern void CGSize_objc_msgSend_stret(out CoreGraphics.CGSize retval, IntPtr receiver, IntPtr selector);

[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
static extern nfloat nfloat_objc_msgSend(IntPtr receiver, IntPtr selector);

protected override void OnAppearing()
{
base.OnAppearing();
Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(2), () =>
{
try
{
var diag = new System.Text.StringBuilder();

var handler = riveView.Handler as Plugin.Maui.Rive.RiveAnimationViewHandler;
if (handler?.PlatformView != null)
{
var pv = handler.PlatformView;
var h = pv.Handle;

// Read initial state
var pausedSel = ObjCRuntime.Selector.GetHandle("isPaused");
var drawSizeSel = ObjCRuntime.Selector.GetHandle("drawableSize");
var esnSel = ObjCRuntime.Selector.GetHandle("enableSetNeedsDisplay");
var deviceSel = ObjCRuntime.Selector.GetHandle("device");

var paused = bool_objc_msgSend(h, pausedSel);
CGSize_objc_msgSend_stret(out var drawSize, h, drawSizeSel);
var esn = bool_objc_msgSend(h, esnSel);
var dev = IntPtr_objc_msgSend(h, deviceSel);

diag.AppendLine($"Before: paused={paused} draw={drawSize.Width:F0}x{drawSize.Height:F0}");
diag.AppendLine($"esn={esn} dev={dev != IntPtr.Zero} frame={pv.Frame.Width:F0}x{pv.Frame.Height:F0}");

// Force proper MTKView configuration
var setEnableSel = ObjCRuntime.Selector.GetHandle("setEnableSetNeedsDisplay:");
void_objc_msgSend_bool(h, setEnableSel, true);

var setPausedSel = ObjCRuntime.Selector.GetHandle("setPaused:");
void_objc_msgSend_bool(h, setPausedSel, false);

var scale = pv.ContentScaleFactor;
var setDrawSel = ObjCRuntime.Selector.GetHandle("setDrawableSize:");
void_objc_msgSend_CGSize(h, setDrawSel, new CoreGraphics.CGSize(pv.Frame.Width * scale, pv.Frame.Height * scale));

pv.SetNeedsDisplay();

// Read after
paused = bool_objc_msgSend(h, pausedSel);
CGSize_objc_msgSend_stret(out drawSize, h, drawSizeSel);
esn = bool_objc_msgSend(h, esnSel);

diag.AppendLine($"After: paused={paused} draw={drawSize.Width:F0}x{drawSize.Height:F0} esn={esn}");
}

diagLabel.Text = diag.ToString();
}
catch (Exception ex)
{
diagLabel.Text = $"Error: {ex.Message}";
}
});
}
#endif
}
