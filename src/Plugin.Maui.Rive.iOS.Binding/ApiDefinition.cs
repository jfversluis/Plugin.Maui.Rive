using System;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
using MetalKit;

namespace RiveRuntime
{
    // @interface RiveSMIInput : NSObject
    [BaseType(typeof(NSObject))]
    interface RiveSMIInput
    {
        [Export("name")]
        string Name { get; }

        [Export("isBoolean")]
        bool IsBoolean { get; }

        [Export("isTrigger")]
        bool IsTrigger { get; }

        [Export("isNumber")]
        bool IsNumber { get; }
    }

    // @interface RiveSMITrigger : RiveSMIInput
    [BaseType(typeof(RiveSMIInput))]
    interface RiveSMITrigger
    {
        [Export("fire")]
        void Fire();
    }

    // @interface RiveSMIBool : RiveSMIInput
    [BaseType(typeof(RiveSMIInput))]
    interface RiveSMIBool
    {
        [Export("value")]
        bool Value { get; [Export("setValue:")] set; }
    }

    // @interface RiveSMINumber : RiveSMIInput
    [BaseType(typeof(RiveSMIInput))]
    interface RiveSMINumber
    {
        [Export("value")]
        float Value { get; [Export("setValue:")] set; }
    }

    // @interface RiveFile : NSObject
    [BaseType(typeof(NSObject))]
    interface RiveFile
    {
        [Static]
        [Export("majorVersion")]
        nuint MajorVersion { get; }

        [Static]
        [Export("minorVersion")]
        nuint MinorVersion { get; }

        [Export("isLoaded")]
        bool IsLoaded { get; set; }

        [Export("artboardCount")]
        nint ArtboardCount { get; }

        [Export("artboardNames")]
        string[] ArtboardNames { get; }

        [Export("initWithData:loadCdn:error:")]
        NativeHandle Constructor(NSData bytes, bool cdn, out NSError error);

        [Export("initWithResource:withExtension:loadCdn:error:")]
        NativeHandle Constructor(string resourceName, string extension, bool cdn, out NSError error);

        [Export("initWithResource:loadCdn:error:")]
        NativeHandle Constructor(string resourceName, bool cdn, out NSError error);

        [Export("artboard:")]
        [return: NullAllowed]
        RiveArtboard GetArtboard(out NSError error);

        [Export("artboardFromIndex:error:")]
        [return: NullAllowed]
        RiveArtboard ArtboardFromIndex(nint index, out NSError error);

        [Export("artboardFromName:error:")]
        [return: NullAllowed]
        RiveArtboard ArtboardFromName(string name, out NSError error);
    }

    // @protocol RiveFileDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface RiveFileDelegate
    {
        [Abstract]
        [Export("riveFileDidLoad:error:")]
        bool RiveFileDidLoad(RiveFile riveFile, out NSError error);

        [Abstract]
        [Export("riveFileDidError:")]
        void RiveFileDidError(NSError error);
    }

    // @interface RiveArtboard : NSObject
    [BaseType(typeof(NSObject))]
    interface RiveArtboard
    {
        [Export("name")]
        string Name { get; }

        [Export("bounds")]
        CGRect Bounds { get; }

        [Export("width")]
        double Width { get; }

        [Export("height")]
        double Height { get; }

        [Export("animationCount")]
        nint AnimationCount { get; }

        [Export("animationNames")]
        string[] AnimationNames { get; }

        [Export("stateMachineCount")]
        nint StateMachineCount { get; }

        [Export("stateMachineNames")]
        string[] StateMachineNames { get; }

        [Export("stateMachineFromIndex:error:")]
        [return: NullAllowed]
        RiveStateMachineInstance StateMachineFromIndex(nint index, out NSError error);

        [Export("stateMachineFromName:error:")]
        [return: NullAllowed]
        RiveStateMachineInstance StateMachineFromName(string name, out NSError error);

        [Export("defaultStateMachine")]
        [NullAllowed]
        RiveStateMachineInstance DefaultStateMachine { get; }

        [Export("advanceBy:")]
        void AdvanceBy(double elapsedSeconds);
    }

    // @interface RiveStateMachineInstance : NSObject
    [BaseType(typeof(NSObject))]
    interface RiveStateMachineInstance
    {
        [Export("name")]
        string Name { get; }

        [Export("advanceBy:")]
        bool AdvanceBy(double elapsedSeconds);

        [Export("getBool:")]
        RiveSMIBool GetBool(string name);

        [Export("getTrigger:")]
        RiveSMITrigger GetTrigger(string name);

        [Export("getNumber:")]
        RiveSMINumber GetNumber(string name);

        [Export("inputNames")]
        string[] InputNames { get; }

        [Export("inputCount")]
        nint InputCount { get; }

        [Export("touchBeganAtLocation:")]
        RiveHitResult TouchBeganAtLocation(CGPoint touchLocation);

        [Export("touchMovedAtLocation:")]
        RiveHitResult TouchMovedAtLocation(CGPoint touchLocation);

        [Export("touchEndedAtLocation:")]
        RiveHitResult TouchEndedAtLocation(CGPoint touchLocation);
    }

    // @interface RiveModel : NSObject
    [BaseType(typeof(NSObject), Name = "_TtC11RiveRuntime9RiveModel")]
    interface RiveModel
    {
    }

    // @protocol RivePlayerDelegate
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface RivePlayerDelegate
    {
    }

    // @protocol RiveStateMachineDelegate
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface RiveStateMachineDelegate
    {
    }

    // @interface RenderContextManager : NSObject
    [BaseType(typeof(NSObject))]
    interface RenderContextManager
    {
        [Static]
        [Export("shared")]
        RenderContextManager Shared { get; }

        [Export("defaultRenderer", ArgumentSemantic.Assign)]
        RendererType DefaultRenderer { get; set; }
    }

    // @interface RiveRenderer : NSObject (NOTE: removed from v6.15.2 binary)

    // @interface RiveRendererView : RiveMTKView
    [BaseType(typeof(UIView))]
    interface RiveRendererView
    {
    }

    // @interface RiveView : RiveRendererView
    [BaseType(typeof(RiveRendererView), Name = "_TtC11RiveRuntime8RiveView")]
    interface RiveView
    {
        [NullAllowed, Export("playerDelegate", ArgumentSemantic.Weak)]
        NSObject WeakPlayerDelegate { get; set; }

        [Export("setModel:autoPlay:error:")]
        bool SetModel(RiveModel model, bool autoPlay, out NSError error);

        [Export("advanceWithDelta:")]
        void AdvanceWithDelta(double delta);
    }

    // @interface RiveViewModel : NSObject <RiveFileDelegate, RivePlayerDelegate, RiveStateMachineDelegate>
    [BaseType(typeof(NSObject), Name = "_TtC11RiveRuntime13RiveViewModel")]
    interface RiveViewModel
    {
        [Export("initWithFileName:extension:in:stateMachineName:fit:alignment:autoPlay:artboardName:loadCdn:customLoader:")]
        [DesignatedInitializer]
        NativeHandle Constructor(string fileName, string extension, NSBundle bundle,
            [NullAllowed] string stateMachineName, RiveFit fit, RiveAlignment alignment,
            bool autoPlay, [NullAllowed] string artboardName, bool loadCdn,
            [NullAllowed] NSObject customLoader);

        [Export("initWithWebURL:stateMachineName:fit:alignment:autoPlay:loadCdn:artboardName:")]
        [DesignatedInitializer]
        NativeHandle Constructor(string webURL, [NullAllowed] string stateMachineName,
            RiveFit fit, RiveAlignment alignment, bool autoPlay, bool loadCdn,
            [NullAllowed] string artboardName);

        [NullAllowed, Export("riveModel")]
        RiveModel RiveModel { get; }

        [Export("isPlaying")]
        bool IsPlaying { get; }

        [Export("autoPlay")]
        bool AutoPlay { get; set; }

        [Export("fit", ArgumentSemantic.Assign)]
        RiveFit Fit { get; set; }

        [Export("alignment", ArgumentSemantic.Assign)]
        RiveAlignment Alignment { get; set; }

        [Export("playWithAnimationName:loop:direction:")]
        void Play([NullAllowed] string animationName, RiveLoop loop, RiveDirection direction);

        [Export("pause")]
        void Pause();

        [Export("stop")]
        void Stop();

        [Export("reset")]
        void Reset();

        [Export("triggerInput:")]
        void TriggerInput(string inputName);

        [Export("setBooleanInput::")]
        void SetBooleanInput(string inputName, bool value);

        [Export("setFloatInput::")]
        void SetFloatInput(string inputName, float value);

        [Export("setDoubleInput::")]
        void SetDoubleInput(string inputName, double value);

        [Export("getTextRunValue:")]
        [return: NullAllowed]
        string GetTextRunValue(string textRunName);

        [Export("setTextRunValue:textValue:error:")]
        bool SetTextRunValue(string textRunName, string textValue, out NSError error);

        [Export("artboardNames")]
        string[] ArtboardNames { get; }

        [Export("createRiveView")]
        RiveView CreateRiveView();

        [Export("setRiveView:")]
        void SetRiveView(RiveView view);

        [Export("updateWithView:")]
        void UpdateView(RiveView view);

        [Export("deregisterView")]
        void DeregisterView();

        [Export("configureModelWithArtboardName:stateMachineName:animationName:error:")]
        bool ConfigureModel([NullAllowed] string artboardName, [NullAllowed] string stateMachineName,
            [NullAllowed] string animationName, out NSError error);

        [Export("resetToDefaultModel")]
        void ResetToDefaultModel();
    }
}
