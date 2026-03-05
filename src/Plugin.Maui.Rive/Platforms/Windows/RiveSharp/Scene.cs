// Copyright 2022 Rive

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RiveSharp
{
    public enum Loop
    {
        // Play until the duration or end of work area of the animation.
        OneShot = 0,

        // Play until the duration or end of work area of the animation and then go back to the
        // start (0 seconds).
        Loop = 1,

        // Play to the end of the duration/work area and then play back.
        PingPong = 2
    };

    public class Scene
    {
        public readonly IntPtr NativePtr;

        public Scene()
        {
            NativePtr = RiveAPI.Scene_New(RiveAPI.CreateNativeRef(Factory.Instance));
        }
        ~Scene()
        {
            RiveAPI.Scene_Delete(NativePtr);
        }

        private bool _isLoaded = false;
        public bool IsLoaded => _isLoaded;

        public bool LoadFile(Stream stream)
        {
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            return LoadFile(data);
        }

        public bool LoadFile(byte[] data)
        {
            _isLoaded = false;
            if (data == null || data.Length == 0)
            {
                return false;
            }
            return RiveAPI.Scene_LoadFile(NativePtr, data, data.Length) != 0;
        }

        // Loads an artboard and animation from the already-loaded file.
        public bool LoadArtboard(string artboardName)
        {
            _isLoaded = false;
            return RiveAPI.Scene_LoadArtboard(NativePtr, artboardName) != 0;
        }

        // Loads a state machine from the already-loaded artboard and file.
        public bool LoadStateMachine(string stateMachineName)
        {
            _isLoaded = RiveAPI.Scene_LoadStateMachine(NativePtr, stateMachineName) != 0;
            return this.IsLoaded;
        }

        // Loads an animation from the already-loaded artboard and file.
        public bool LoadAnimation(string animationName)
        {
            _isLoaded = RiveAPI.Scene_LoadAnimation(NativePtr, animationName) != 0;
            return this.IsLoaded;
        }

        public void SetBool(string name, bool value)
        {
            if (this.IsLoaded && RiveAPI.Scene_SetBool(NativePtr, name, value ? 1 : 0) == 0)
            {
                throw new Exception($"State machine bool input '{name}' not found.");
            }
        }

        public void SetNumber(string name, float value)
        {
            if (this.IsLoaded && RiveAPI.Scene_SetNumber(NativePtr, name, value) == 0)
            {
                throw new Exception($"State machine number input '{name}' not found.");
            }
        }

        public void FireTrigger(string name)
        {
            if (this.IsLoaded && RiveAPI.Scene_FireTrigger(NativePtr, name) == 0)
            {
                throw new Exception($"State machine trigger input '{name}' not found.");
            }
        }

        public float Width => RiveAPI.Scene_Width(NativePtr);
        public float Height => RiveAPI.Scene_Height(NativePtr);

        public string Name
        {
            get
            {
                int numChars = RiveAPI.Scene_Name(NativePtr, null);
                if (numChars > 1)
                {
                    char[] charArray = new char[numChars];
                    RiveAPI.Scene_Name(NativePtr, charArray);
                    return new string(charArray);
                }
                return "";
            }
        }

        // Returns OneShot if this has no looping (e.g. a statemachine)
        public Loop Loop => (Loop)RiveAPI.Scene_Loop(NativePtr);

        // Returns true iff the Scene is known to not be fully opaque
        public bool IsTranslucent => RiveAPI.Scene_IsTranslucent(NativePtr) != 0;

        // returns -1 for continuous
        public double DurationSeconds => RiveAPI.Scene_DurationSeconds(NativePtr);

        // returns true if Draw() should be called
        public bool AdvanceAndApply(double elapsedSeconds)
        {
            return RiveAPI.Scene_AdvanceAndApply(NativePtr, (float)elapsedSeconds) != 0;
        }

        public void Draw(Renderer renderer)
        {
            var gch = GCHandle.Alloc(renderer);
            RiveAPI.Scene_Draw(NativePtr, GCHandle.ToIntPtr(gch));
            gch.Free();
        }

        public void PointerDown(Vec2D pos) => RiveAPI.Scene_PointerDown(NativePtr, pos);
        public void PointerMove(Vec2D pos) => RiveAPI.Scene_PointerMove(NativePtr, pos);
        public void PointerUp(Vec2D pos) => RiveAPI.Scene_PointerUp(NativePtr, pos);

        // --- Introspection ---

        private static string GetNativeString(Func<char[], int> getter)
        {
            int numChars = getter(null);
            if (numChars > 0)
            {
                char[] charArray = new char[numChars];
                getter(charArray);
                return new string(charArray);
            }
            return "";
        }

        public string[] GetArtboardNames()
        {
            int count = RiveAPI.Scene_ArtboardCount(NativePtr);
            var names = new string[count];
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                names[i] = GetNativeString(buf => RiveAPI.Scene_ArtboardName(NativePtr, idx, buf));
            }
            return names;
        }

        public string[] GetAnimationNames()
        {
            int count = RiveAPI.Scene_AnimationCount(NativePtr);
            var names = new string[count];
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                names[i] = GetNativeString(buf => RiveAPI.Scene_AnimationName(NativePtr, idx, buf));
            }
            return names;
        }

        public string[] GetStateMachineNames()
        {
            int count = RiveAPI.Scene_StateMachineCount(NativePtr);
            var names = new string[count];
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                names[i] = GetNativeString(buf => RiveAPI.Scene_StateMachineName(NativePtr, idx, buf));
            }
            return names;
        }

        public int InputCount => RiveAPI.Scene_InputCount(NativePtr);

        public string GetInputName(int index)
        {
            return GetNativeString(buf => RiveAPI.Scene_InputName(NativePtr, index, buf));
        }

        // Returns 0=trigger, 1=bool, 2=number
        public int GetInputType(int index)
        {
            return RiveAPI.Scene_InputType(NativePtr, index);
        }

        public int StateChangedCount => RiveAPI.Scene_StateChangedCount(NativePtr);

        public string GetStateChangedName(int index)
        {
            return GetNativeString(buf => RiveAPI.Scene_StateChangedName(NativePtr, index, buf));
        }

        public int ReportedEventCount => RiveAPI.Scene_ReportedEventCount(NativePtr);

        public string GetReportedEventName(int index)
        {
            return GetNativeString(buf => RiveAPI.Scene_ReportedEventName(NativePtr, index, buf));
        }

        // --- Text runs ---

        public string? GetTextRunValue(string name, string? path = null)
        {
            int numChars = RiveAPI.Scene_GetTextRunValue(NativePtr, name, path ?? "", null!);
            if (numChars > 0)
            {
                char[] charArray = new char[numChars];
                RiveAPI.Scene_GetTextRunValue(NativePtr, name, path ?? "", charArray);
                return new string(charArray);
            }
            return null;
        }

        public bool SetTextRunValue(string name, string value, string? path = null)
        {
            return RiveAPI.Scene_SetTextRunValue(NativePtr, name, path ?? "", value) != 0;
        }
    }
}
