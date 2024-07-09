// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;
using LibreLancer.Dialogs;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.OpenGL;
using LibreLancer.Platforms;

namespace LibreLancer
{
    class SDL2Game : IGame
    {
        int width;
        int height;
        double totalTime;
        bool fullscreen;
        private bool allowScreensaver;
        bool running = false;
        string title = "LibreLancer";
        IntPtr windowptr;
        double renderFrequency;
        public Mouse Mouse { get; } = new Mouse();
        public Keyboard Keyboard { get; } = new Keyboard();
        ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
        int mythread = -1;
        private uint wakeEvent;

        public ScreenshotSaveHandler OnScreenshotSave { get; set; }

        public RenderContext RenderContext { get; private set; }
        public string Renderer
        {
            get; private set;
        }

        public SDL2Game(int w, int h, bool fullscreen, bool allowScreensaver)
        {
            width = w;
            height = h;
            mythread = Thread.CurrentThread.ManagedThreadId;
            this.allowScreensaver = allowScreensaver;
        }

        private bool _relativeMouseMode = false;

        public bool RelativeMouseMode
        {
            get => _relativeMouseMode;
            set
            {
                if (value != _relativeMouseMode)
                    SDL.SDL_SetRelativeMouseMode(value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);
                _relativeMouseMode = value;
            }
        }

        public float DpiScale { get; set; } = 1;


        public int Width
        {
            get
            {
                return width;
            }
        }

        public unsafe void SetWindowIcon(int width, int height, ReadOnlySpan<Bgra8> data)
        {
            IntPtr surface;
            fixed (Bgra8* ptr = &data.GetPinnableReference())
            {
                surface = SDL.SDL_CreateRGBSurfaceFrom(
                    (IntPtr)ptr,
                    width,
                    height,
                    32,
                    width * 4,
                    0xFF000000,
                    0x0000FF00,
                    0x000000FF,
                    0xFF000000);
            }
            SDL.SDL_SetWindowIcon(windowptr, surface);
            SDL.SDL_FreeSurface(surface);
        }

        public ClipboardContents ClipboardStatus()
        {
            if (SDL.SDL_HasClipboardText() == SDL.SDL_bool.SDL_TRUE)
            {
                var text = SDL.SDL_GetClipboardText();
                if (L85.IsL85String(text))
                    return ClipboardContents.Array;
                return ClipboardContents.Text;
            }
            return ClipboardContents.None;
        }

        public string GetClipboardText()
        {
            var text = SDL.SDL_GetClipboardText();
            if (L85.IsL85String(text))
                return null;
            return text;
        }

        public void SetClipboardText(string text)
        {
            SDL.SDL_SetClipboardText(text);
        }

        public byte[] GetClipboardArray()
        {
            var text = SDL.SDL_GetClipboardText();
            if (L85.IsL85String(text))
                return L85.FromL85String(text);
            return null;
        }

        public void SetClipboardArray(byte[] array)
        {
            SDL.SDL_SetClipboardText(L85.ToL85String(array));
        }
        IntPtr curArrow;
        IntPtr curMove;
        IntPtr curTextInput;
        IntPtr curResizeNS;
        IntPtr curResizeEW;
        IntPtr curResizeNESW;
        IntPtr curResizeNWSE;
        IntPtr curNotAllowed;
        CursorKind cursorKind = CursorKind.Arrow;
        public CursorKind CursorKind
        {
            get
            {
                return cursorKind;
            }
            set
            {
                if (cursorKind == value) return;
                cursorKind = value;
                switch (cursorKind)
                {
                    case CursorKind.Arrow:
                        SDL.SDL_SetCursor(curArrow);
                        break;
                    case CursorKind.Move:
                        SDL.SDL_SetCursor(curMove);
                        break;
                    case CursorKind.TextInput:
                        SDL.SDL_SetCursor(curTextInput);
                        break;
                    case CursorKind.ResizeNS:
                        SDL.SDL_SetCursor(curResizeNS);
                        break;
                    case CursorKind.ResizeEW:
                        SDL.SDL_SetCursor(curResizeEW);
                        break;
                    case CursorKind.ResizeNESW:
                        SDL.SDL_SetCursor(curResizeNESW);
                        break;
                    case CursorKind.ResizeNWSE:
                        SDL.SDL_SetCursor(curResizeNWSE);
                        break;
                    case CursorKind.NotAllowed:
                        SDL.SDL_SetCursor(curNotAllowed);
                        break;
                }
                SDL.SDL_ShowCursor(value == CursorKind.None ? 0 : 1);
            }
        }
        public int Height
        {
            get
            {
                return height;
            }
        }

        public double TotalTime
        {
            get
            {
                return totalTime;
            }
        }
        Stopwatch timer;
        public double TimerTick
        {
            get
            {
                return timer.ElapsedMilliseconds / 1000.0;
            }
        }
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                SDL.SDL_SetWindowTitle(windowptr, title);
            }
        }

        public double RenderFrequency
        {
            get
            {
                return renderFrequency;
            }
        }
        double frameTime;
        public double FrameTime
        {
            get
            {
                return frameTime;
            }
        }


        public void QueueUIThread(Action work)
        {
            actions.Enqueue(work);
            InterruptWait();
        }

        public bool IsUiThread() =>  Thread.CurrentThread.ManagedThreadId == mythread;

        string _screenshotpath;
        bool _screenshot;
        public void Screenshot(string filename)
        {
            _screenshotpath = filename;
            _screenshot = true;
        }

        unsafe void TakeScreenshot()
        {
            GL.ReadBuffer(GL.GL_BACK);
            var colordata = new Bgra8[width * height];
            fixed (Bgra8* ptr = colordata)
                GL.ReadPixels(0, 0, width, height, GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, (IntPtr)ptr);
            for (int i = 0; i < colordata.Length; i++) {
                    colordata[i].A = 0xFF;
            }
            OnScreenshotSave(_screenshotpath, width, height, colordata);
        }

        private bool isVsync = false;

        public void SetVSync(bool vsync)
        {
            isVsync = vsync;
        }

        Point minWindowSize = Point.Zero;

        public Point MinimumWindowSize
        {
            get
            {
                return minWindowSize;
            }
            set
            {
                minWindowSize = value;
                if (windowptr != IntPtr.Zero)
                    SDL.SDL_SetWindowMinimumSize(windowptr, value.X, value.Y);
            }
        }

        public void BringToFront()
        {
            if(windowptr != IntPtr.Zero)
                SDL.SDL_RaiseWindow(windowptr);
        }

        private bool waitForEvent = false;
        private int waitTimeout = 2000;
        public void WaitForEvent(int timeout = 2000)
        {
            waitForEvent = true;
            waitTimeout = timeout;
        }

        public void InterruptWait()
        {
            var ev = new SDL.SDL_Event();
            ev.type = (SDL.SDL_EventType) wakeEvent;
            SDL.SDL_PushEvent(ref ev);
        }

        public bool Focused { get; private set; }
        public bool EventsThisFrame { get; private set; }


        [DllImport("user32.dll", SetLastError=true)]
        static extern bool SetProcessDPIAware();


        public void Run(Game loop)
        {
            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_ALLOW_SCREENSAVER, allowScreensaver ? "1" : "0");
            //Try to set DPI Awareness on Win32
            if (Platform.RunningOS == OS.Windows)
            {
                try {
                    SetProcessDPIAware();
                }
                catch {
                }
            }

            FLLog.Info("Engine", "Version: " + Platform.GetInformationalVersion<Game>());
            //TODO: This makes i5-7200U on mesa 18 faster, but this should probably be a configurable option
            bool setMesaThread = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("mesa_glthread"));
            if(setMesaThread) Environment.SetEnvironmentVariable("mesa_glthread", "true");
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
            {
                FLLog.Error("SDL", "SDL_Init failed, exiting.");
                return;
            }
            KeysExtensions.FillKeyNamesSDL();

            wakeEvent = SDL.SDL_RegisterEvents(1);
            SDL.SDL_SetHint(SDL.SDL_HINT_IME_INTERNAL_EDITING, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR, "0");
            //Set GL states
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            //Create Window

            var hiddenFlag = loop.Splash ? SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN :  0;
            var flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                        SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI | hiddenFlag;
            if (fullscreen)
                flags |= SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
            var sdlWin = SDL.SDL_CreateWindow(
                             "LibreLancer",
                             SDL.SDL_WINDOWPOS_CENTERED,
                             SDL.SDL_WINDOWPOS_CENTERED,
                             width,
                             height,
                             flags
                         );
            Platform.Init(SDL.SDL_GetCurrentVideoDriver());
            //Cursors
            curArrow = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
            curMove = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_CROSSHAIR);
            curTextInput = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM);
            curResizeNS = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENS);
            curResizeEW = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEWE);
            curResizeNESW = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENESW);
            curResizeNWSE = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENWSE);
            curNotAllowed = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NO);
            //Window sizing
            if (sdlWin == IntPtr.Zero)
            {
                Dialogs.CrashWindow.Run("Librelancer", "Failed to create SDL window",
                    "SDL Error: " + (SDL.SDL_GetError() ?? ""));
                return;
            }
            if (minWindowSize != Point.Zero)
            {
                SDL.SDL_SetWindowMinimumSize(sdlWin, minWindowSize.X, minWindowSize.Y);
            }
            SDL.SDL_EventState(SDL.SDL_EventType.SDL_DROPFILE, SDL.SDL_ENABLE);
            SDL.SDL_EventState(SDL.SDL_EventType.SDL_SYSWMEVENT, SDL.SDL_ENABLE);
            windowptr = sdlWin;
            IRenderContext renderBackend = GLRenderContext.Create(sdlWin);
            if (renderBackend == null)
            {
                Dialogs.CrashWindow.Run("Librelancer", "Failed to create OpenGL context",
                    "Your driver or gpu does not support at least OpenGL 3.2 or OpenGL ES 3.1\n" + SDL.SDL_GetError() ?? "");
            }

            Renderer = renderBackend.GetRenderer();
            FLLog.Info("Graphics", $"Renderer: {Renderer}");
            SetVSync(true);
            //Init game state
            RenderContext = new RenderContext(renderBackend);
            FLLog.Info("Graphics", $"Max Anisotropy: {RenderContext.MaxAnisotropy}");
            FLLog.Info("Graphics", $"Max AA: {RenderContext.MaxSamples}");
            SDL.SDL_GetWindowSize(sdlWin, out int windowWidth, out int windowHeight);
            var drawable = RenderContext.Backend.GetDrawableSize(sdlWin);
            width = drawable.X;
            height = drawable.Y;
            var scaleW = (float) width / windowWidth;
            var scaleH = (float) height / windowHeight;
            if (Platform.RunningOS != OS.Windows) DpiScale = scaleH;
            else
            {
                if (SDL.SDL_GetDisplayDPI(0, out float ddpi, out _, out _) == 0)
                {
                    DpiScale = ddpi / 96.0f;
                }
            }
            FLLog.Info("GL", $"Dpi Scale: {DpiScale:F4}");
            Texture2D splashTexture;
            if (loop.Splash && (splashTexture = loop.GetSplashInternal()) != null)
            {
                var win2 = SDL.SDL_CreateWindow(
                    "Librelancer",
                    SDL.SDL_WINDOWPOS_CENTERED,
                    SDL.SDL_WINDOWPOS_CENTERED,
                    750, 250, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
                              SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
                              SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS |
                              SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
                RenderContext.Backend.MakeCurrent(win2);
                RenderContext.ClearColor = Color4.Black;
                var dw = RenderContext.Backend.GetDrawableSize(win2);
                RenderContext.ReplaceViewport(0,0, dw.X, dw.Y);
                RenderContext.ClearAll();
                RenderContext.Renderer2D.DrawImageStretched(splashTexture, new Rectangle(0,0,dw.X,dw.Y), Color4.White, true);
                RenderContext.EndFrame();
                RenderContext.Backend.SwapWindow(win2, false, false);
                loop.OnLoad();
                splashTexture.Dispose();
                RenderContext.Backend.MakeCurrent(sdlWin);
                SDL.SDL_DestroyWindow(win2);
            }
            else
            {
                loop.OnLoad();
            }
            SDL.SDL_ShowWindow(sdlWin);
            NFD.NFD_Init();
            using var events = Platform.SubscribeEvents(this);
            //kill the value we set so it doesn't crash child processes
            if(setMesaThread) Environment.SetEnvironmentVariable("mesa_glthread",null);
            //Start game
            running = true;
            timer = new Stopwatch();
            timer.Start();
            double last = 0;
            double elapsed = 0;
            SDL.SDL_Event e = new SDL.SDL_Event();
            SDL.SDL_StopTextInput();
            MouseButtons doRelease = 0;
            while (running)
            {
                events.Poll();
                //Window State
                var winFlags = (SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(sdlWin);
                Focused = (winFlags & SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS) ==
                          SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS ||
                          (winFlags & SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) ==
                          SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS;
                EventsThisFrame = false;
                //Get Size
                SDL.SDL_GetWindowSize(sdlWin, out  windowWidth, out  windowHeight);
                var dw = RenderContext.Backend.GetDrawableSize(sdlWin);
                width = dw.X;
                height = dw.Y;
                scaleW = (float) width / windowWidth;
                scaleH = (float) height / windowHeight;
                if (Platform.RunningOS != OS.Windows) DpiScale = scaleH;
                //This allows for press/release in same frame to have
                //button down for one frame, e.g. trackpoint middle click on Linux/libinput.
                MouseButtons pressedThisFrame = 0;
                Mouse.Buttons &= ~doRelease;
                doRelease = 0;
                bool eventWaited = false;
                if (waitForEvent)
                {
                    waitForEvent = false;
                    if (SDL.SDL_WaitEventTimeout(out e, waitTimeout) != 0)
                    {
                        eventWaited = true;
                    }
                }
                Mouse.Wheel = 0;
                //Pump message queue
                while (eventWaited || SDL.SDL_PollEvent(out e) != 0)
                {
                    eventWaited = false;
                    EventsThisFrame = true;
                    switch (e.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            {
                                if(loop.OnWillClose())
                                    running = false;
                                break;
                            }
                        case SDL.SDL_EventType.SDL_MOUSEMOTION:
                        {
                            var x = RelativeMouseMode ? e.motion.xrel : e.motion.x;
                            var y = RelativeMouseMode ? e.motion.yrel : e.motion.y;
                                Mouse.X = (int) (scaleW * e.motion.x);
                                Mouse.Y = (int) (scaleH * e.motion.y);
                                Mouse.OnMouseMove((int)(scaleW * x),(int)(scaleH * y));
                                break;
                            }
                        case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                            {
                                Mouse.X = (int) (scaleW * e.button.x);
                                Mouse.Y = (int) (scaleH * e.button.y);
                                var btn = GetMouseButton(e.button.button);
                                Mouse.Buttons |= btn;
                                pressedThisFrame |= btn;
                                Mouse.OnMouseDown(btn);
                                if(e.button.clicks == 2)
                                    Mouse.OnMouseDoubleClick(btn);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                            {
                                Mouse.X = (int) (scaleW * e.button.x);
                                Mouse.Y = (int) (scaleH * e.button.y);
                                var btn = GetMouseButton(e.button.button);
                                if ((pressedThisFrame & btn) == btn)
                                    doRelease |= btn;
                                else
                                    Mouse.Buttons &= ~btn;
                                Mouse.OnMouseUp(btn);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                            {
                                Mouse.OnMouseWheel(e.wheel.x, e.wheel.y);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_TEXTINPUT:
                            {
                                Keyboard.OnTextInput(GetEventText(ref e));
                                break;
                            }
                        case SDL.SDL_EventType.SDL_KEYDOWN:
                            {
                                Keyboard.OnKeyDown((Keys)e.key.keysym.scancode, (KeyModifiers)e.key.keysym.mod, e.key.repeat != 0);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_KEYUP:
                            {
                                Keyboard.OnKeyUp((Keys)e.key.keysym.scancode, (KeyModifiers)e.key.keysym.mod);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_SYSWMEVENT:
                            {
                                events.WndProc(ref e);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_KEYMAPCHANGED:
                            KeysExtensions.ResetKeyNames();
                            break;
                        case SDL.SDL_EventType.SDL_WINDOWEVENT:
                            if (e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                            {
                                loop.SignalResize();
                            }
                            break;
                        case SDL.SDL_EventType.SDL_DROPFILE:
                            {
                                var file = UnsafeHelpers.PtrToStringUTF8(e.drop.file);
                                loop.SignalDrop(file);
                                SDL.SDL_free(e.drop.file);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_CLIPBOARDUPDATE:
                            {
                                loop.SignalClipboardUpdate();
                                break;
                            }
                    }
                }
                Mouse.Wheel /= 2.5f;
                //Do game things
                if (!running)
                    break;
                while (actions.TryDequeue(out Action work))
                    work();
                totalTime = timer.Elapsed.TotalSeconds;
                loop.OnUpdate(elapsed);
                if (!running)
                    break;
                loop.OnDraw(elapsed);
                RenderContext.EndFrame();
                //Frame time before, FPS after
                var tk = timer.Elapsed.TotalSeconds - totalTime;
                frameTime = CalcAverageTime(tk);
                if (_screenshot)
                {
                    TakeScreenshot();
                    _screenshot = false;
                }
                RenderContext.Backend.SwapWindow(sdlWin, isVsync, fullscreen);

                elapsed = timer.Elapsed.TotalSeconds - last;
                renderFrequency = (1.0 / CalcAverageTick(elapsed));
                last = timer.Elapsed.TotalSeconds;
                totalTime = timer.Elapsed.TotalSeconds;
                if (elapsed < 0)
                {
                    elapsed = 0;
                    FLLog.Warning("Timing", "Stopwatch returned negative time!");
                }
            }
            loop.OnCleanup();
            Platform.Shutdown();
            NFD.NFD_Quit();
            SDL.SDL_Quit();
        }

        public void ToggleFullScreen()
        {
            if (fullscreen)
                SDL.SDL_SetWindowFullscreen(windowptr, 0);
            else
                SDL.SDL_SetWindowFullscreen(windowptr, (int)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
            fullscreen = !fullscreen;
        }

        //TODO: Terrible Hack
        public void Crashed()
        {
            SDL.SDL_Quit();
        }

        const int FPS_MAXSAMPLES = 50;
        int tickindex = 0;
        double ticksum = 0;
        double[] ticklist = new double[FPS_MAXSAMPLES];

        double CalcAverageTick(double newtick)
        {
            ticksum -= ticklist[tickindex];
            ticksum += newtick;
            ticklist[tickindex] = newtick;
            if (++tickindex == FPS_MAXSAMPLES)
                tickindex = 0;
            return ((double)ticksum / FPS_MAXSAMPLES);
        }

        int timeindex = 0;
        double timesum = 0;
        double[] timelist = new double[FPS_MAXSAMPLES];

        double CalcAverageTime(double newtick)
        {
            timesum -= timelist[timeindex];
            timesum += newtick;
            timelist[timeindex] = newtick;
            if (++timeindex == FPS_MAXSAMPLES)
                timeindex = 0;
            return ((double)timesum / FPS_MAXSAMPLES);
        }

        //Convert from SDL2 button to saner button
        MouseButtons GetMouseButton(byte b)
        {
            if (b == SDL.SDL_BUTTON_LEFT)
                return MouseButtons.Left;
            if (b == SDL.SDL_BUTTON_MIDDLE)
                return MouseButtons.Middle;
            if (b == SDL.SDL_BUTTON_RIGHT)
                return MouseButtons.Right;
            if (b == SDL.SDL_BUTTON_X1)
                return MouseButtons.X1;
            if (b == SDL.SDL_BUTTON_X2)
                return MouseButtons.X2;
            throw new Exception("SDL2 gave undefined mouse button"); //should never happen
        }

        unsafe string GetEventText(ref SDL.SDL_Event e)
        {
            byte[] rawBytes = new byte[SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE];
            fixed (byte* txtPtr = e.text.text)
            {
                Marshal.Copy((IntPtr)txtPtr, rawBytes, 0, SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE);
            }
            int nullIndex = Array.IndexOf(rawBytes, (byte)0);
            string text = Encoding.UTF8.GetString(rawBytes, 0, nullIndex);
            return text;
        }
        bool textInputEnabled = false;
        public bool TextInputEnabled
        {
            get { return textInputEnabled; }
            set
            {
                if (textInputEnabled == value) return;
                if (value) EnableTextInput();
                else DisableTextInput();
            }
        }
        public void EnableTextInput()
        {
            if (!textInputEnabled)
            {
                SDL.SDL_StartTextInput();
                textInputEnabled = true;
            }
        }
        public void DisableTextInput()
        {
            if (textInputEnabled)
            {
                SDL.SDL_StopTextInput();
                textInputEnabled = false;
            }
        }
        public void Exit()
        {
            running = false;
        }
    }
}

