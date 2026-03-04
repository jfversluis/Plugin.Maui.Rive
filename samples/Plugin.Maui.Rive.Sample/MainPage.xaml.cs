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
[return: MarshalAs(UnmanagedType.I1)]
static extern bool bool_objc_msgSend(IntPtr receiver, IntPtr selector);

[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
static extern void CGSize_objc_msgSend_stret(out CoreGraphics.CGSize retval, IntPtr receiver, IntPtr selector);

[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
static extern void void_objc_msgSend_CGRect(IntPtr receiver, IntPtr selector, CoreGraphics.CGRect rect);

protected override void OnAppearing()
{
base.OnAppearing();
Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), () =>
{
try
{
var diag = new System.Text.StringBuilder();
var handler = riveView.Handler as Plugin.Maui.Rive.RiveAnimationViewHandler;
if (handler?.PlatformView == null) { diagLabel.Text = "No PV"; return; }

var pv = handler.PlatformView;
var h = pv.Handle;

diag.AppendLine($"frame={pv.Frame.Width:F0}x{pv.Frame.Height:F0} bounds={pv.Bounds.Width:F0}x{pv.Bounds.Height:F0}");
diag.AppendLine($"window={pv.Window != null} scale={pv.ContentScaleFactor}");

var esnSel = ObjCRuntime.Selector.GetHandle("enableSetNeedsDisplay");
var esn = bool_objc_msgSend(h, esnSel);

var drawSizeSel = ObjCRuntime.Selector.GetHandle("drawableSize");
CGSize_objc_msgSend_stret(out var drawSize, h, drawSizeSel);

var pausedSel = ObjCRuntime.Selector.GetHandle("isPaused");
var paused = bool_objc_msgSend(h, pausedSel);

var autoResizeSel = ObjCRuntime.Selector.GetHandle("autoResizeDrawable");
var autoResize = bool_objc_msgSend(h, autoResizeSel);

diag.AppendLine($"esn={esn} draw={drawSize.Width:F0}x{drawSize.Height:F0}");
diag.AppendLine($"paused={paused} autoResize={autoResize}");

// Check if currentDrawable exists
var drawableSel = ObjCRuntime.Selector.GetHandle("currentDrawable");
var drawable = IntPtr_objc_msgSend(h, drawableSel);
diag.AppendLine($"drawable={drawable != IntPtr.Zero}");

// Try manually calling drawRect:
var drawRectSel = ObjCRuntime.Selector.GetHandle("drawRect:");
void_objc_msgSend_CGRect(h, drawRectSel, pv.Bounds);
diag.AppendLine("Called drawRect manually");

// Check again
CGSize_objc_msgSend_stret(out var drawSize2, h, drawSizeSel);
var esn2 = bool_objc_msgSend(h, esnSel);
diag.AppendLine($"After: draw={drawSize2.Width:F0}x{drawSize2.Height:F0} esn={esn2}");

diagLabel.Text = diag.ToString();
}
catch (Exception ex)
{
diagLabel.Text = $"Error: {ex.Message}\n{ex.StackTrace?.Substring(0, Math.Min(200, ex.StackTrace?.Length ?? 0))}";
}
});
}
#endif
}
