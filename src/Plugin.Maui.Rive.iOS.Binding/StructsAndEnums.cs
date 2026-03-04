using ObjCRuntime;

namespace RiveRuntime
{
    [Native]
    public enum RiveLoop : long
    {
        OneShot = 0,
        Loop = 1,
        PingPong = 2,
        AutoLoop = 3
    }

    [Native]
    public enum RiveDirection : long
    {
        Backwards = 0,
        Forwards = 1,
        AutoDirection = 2
    }

    [Native]
    public enum RiveFit : long
    {
        Fill = 0,
        Contain = 1,
        Cover = 2,
        FitHeight = 3,
        FitWidth = 4,
        ScaleDown = 5,
        NoFit = 6,
        Layout = 7
    }

    [Native]
    public enum RiveAlignment : long
    {
        TopLeft = 0,
        TopCenter = 1,
        TopRight = 2,
        CenterLeft = 3,
        Center = 4,
        CenterRight = 5,
        BottomLeft = 6,
        BottomCenter = 7,
        BottomRight = 8
    }

    [Native]
    public enum RiveErrorCode : long
    {
        NoArtboardsFound = 100,
        NoArtboardFound = 101,
        NoAnimations = 200,
        NoAnimationFound = 201,
        NoStateMachines = 300,
        NoStateMachineFound = 301,
        NoStateMachineInputFound = 400,
        UnknownStateMachineInput = 401,
        NoStateChangeFound = 402,
        UnsupportedVersion = 500,
        MalformedFile = 600,
        UnknownError = 700
    }

    [Native]
    public enum RiveHitResult : long
    {
        None = 0,
        Hit = 1,
        HitOpaque = 2
    }

    [Native]
    public enum RendererType : long
    {
        RiveRenderer = 0,
        CgRenderer = 1
    }
}
