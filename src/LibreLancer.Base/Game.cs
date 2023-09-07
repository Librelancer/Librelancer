// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LibreLancer
{
    public delegate void ScreenshotSaveHandler(string filename, int width, int height, byte[] data);
    public class Game : IUIThread, IGLWindow
    {
        int width;
        int height;
        double totalTime;
        bool fullscreen;
        bool running = false;
        string title = "LibreLancer";
        IntPtr windowptr;
        double renderFrequency;
        public Mouse Mouse = new Mouse();
        public Keyboard Keyboard = new Keyboard();
        ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
        int mythread = -1;
        private uint wakeEvent;
        
        public ScreenshotSaveHandler ScreenshotSave;
        public RenderContext RenderContext { get; private set; }
        public string Renderer
        {
            get; private set;
        }
        public Game(int w, int h, bool fullscreen)
        {
            width = w;
            height = h;
            mythread = Thread.CurrentThread.ManagedThreadId;
        }

        public float DpiScale { get; set; } = 1;
        protected static string GetSaveDirectory(string OrgName, string GameName)
        {
            string platform = SDL.SDL_GetPlatform();
            if (platform.Equals("Windows"))
            {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments
                    ),
                    "SavedGames",
                    GameName
                );
            }
            else if (platform.Equals("Mac OS X"))
            {
                string osConfigDir = Environment.GetEnvironmentVariable("HOME");
                if (String.IsNullOrEmpty(osConfigDir))
                {
                    return "."; // Oh well.
                }
                osConfigDir += "/Library/Application Support";
                return Path.Combine(osConfigDir, GameName);
            }
            else if (	platform.Equals("Linux") ||
                        platform.Equals("FreeBSD") ||
                        platform.Equals("OpenBSD") ||
                        platform.Equals("NetBSD")	)
            {
                string osConfigDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (String.IsNullOrEmpty(osConfigDir))
                {
                    osConfigDir = Environment.GetEnvironmentVariable("HOME");
                    if (String.IsNullOrEmpty(osConfigDir))
                    {
                        return "."; // Oh well.
                    }
                    osConfigDir += "/.local/share";
                }
                return Path.Combine(osConfigDir, GameName);
            }
            else
            {
                return SDL.SDL_GetPrefPath(OrgName, GameName);
            }
        }

        public int Width
        {
            get
            {
                return width;
            }
        }

        protected List<object> Services = new List<object>();
        public T GetService<T>()
        {
            return Services.OfType<T>().FirstOrDefault();
        }
        public IntPtr GetHwnd()
        {
            if (Platform.RunningOS != OS.Windows) return IntPtr.Zero;
            var wminfo = new SDL.SDL_SysWMinfo();
            SDL.SDL_VERSION(out wminfo.version);
            if (SDL.SDL_GetWindowWMInfo(windowptr, ref wminfo) != SDL.SDL_bool.SDL_TRUE) return IntPtr.Zero;
            if (wminfo.subsystem != SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS) return IntPtr.Zero;
            return wminfo.info.win.window;
        }

        public unsafe void SetWindowIcon(int width, int height, byte[] data)
        {
            IntPtr surface;
            fixed (byte* ptr = data)
            {
                surface = SDL.SDL_CreateRGBSurfaceFrom(
                    (IntPtr)ptr,
                    width,
                    height,
                    32,
                    width * 4,
                    0x000000FF,
                    0x0000FF00,
                    0x00FF0000,
                    0xFF000000);
            }
            SDL.SDL_SetWindowIcon(windowptr, surface);
            SDL.SDL_FreeSurface(surface);
        }

        public bool GetX11Info(out IntPtr display, out IntPtr window)
        {
            window = display = IntPtr.Zero;
            var wminfo = new SDL.SDL_SysWMinfo();
            SDL.SDL_VERSION(out wminfo.version);
            if (SDL.SDL_GetWindowWMInfo(windowptr, ref wminfo) != SDL.SDL_bool.SDL_TRUE) return false;
            if (wminfo.subsystem != SDL.SDL_SYSWM_TYPE.SDL_SYSWM_X11) return false;
            display = wminfo.info.x11.display;
            window = wminfo.info.x11.window;
            return true;
        }

        public string GetClipboardText()
        {
            return SDL.SDL_GetClipboardText();
        }
        public void SetClipboardText(string text)
        {
            SDL.SDL_SetClipboardText(text);
        }
        IntPtr curArrow;
        IntPtr curMove;
        IntPtr curTextInput;
        IntPtr curResizeNS;
        IntPtr curResizeEW;
        IntPtr curResizeNESW;
        IntPtr curResizeNWSE;
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
        
        public long CurrentTick { get; private set; }
        
        public IntPtr GetGLProcAddress(string name)
        {
            return SDL.SDL_GL_GetProcAddress(name);
        }

        public void UnbindAll()
        {
            GLBind.VertexArray(RenderContext.Instance.NullVAO);
        }
        public void TrashGLState()
        {
            GLBind.Trash();
            RenderContext.Instance.Trash();
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
            if (ScreenshotSave != null)
            {
                GL.ReadBuffer(GL.GL_BACK);
                var colordata = new byte[width * height * 4];
                fixed (byte* ptr = colordata)
                {
                    GL.ReadPixels(0, 0, width, height, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, (IntPtr)ptr);
                }
                var c = RenderContext.ClearColor;
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                    {
                        int offset = (y * height * 4) + (x * 4);
                        colordata[offset + 3] = 0xFF;
                    }

                ScreenshotSave(_screenshotpath, width, height, colordata);
            }

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

        public void Yield()
        {
            if (Thread.CurrentThread.ManagedThreadId == mythread)
            {
                Action work;
                while (actions.TryDequeue(out work))
                    work();
            }
            Thread.Sleep(0);
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
        public event Action WillClose;
        
        public static HeadlessContext CreateHeadless()
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
                throw new Exception("SDL_Init failed");
            var win = SDL.SDL_CreateWindow("Headless Librelancer",
                SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED,
                128, 128,
                SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL);
            if (win == IntPtr.Zero)
                throw new Exception("Failed to create hidden SDL window");
            var ctx = CreateGLContext(win);
            if (ctx == IntPtr.Zero)
                throw new Exception("Failed to create OpenGL context");
            return new HeadlessContext()
            {
                RenderContext = new RenderContext(),
                UiThreadId = Thread.CurrentThread.ManagedThreadId
            };
        }

        static bool CreateContextCore(IntPtr sdlWin, out IntPtr ctx)
        {
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
            ctx = SDL.SDL_GL_CreateContext(sdlWin);
            if (ctx == IntPtr.Zero) return false;
            if (!GL.CheckStringSDL()) {
                SDL.SDL_GL_DeleteContext(ctx);
                ctx = IntPtr.Zero;
            }
            return true;
        }
        static bool CreateContextES(IntPtr sdlWin, out IntPtr ctx)
        {
            //mesa on raspberry pi OS won't give you a 3.1 context if you request it
            //but it will give you 3.1 if you request 3.0  ¯\_(ツ)_/¯
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES);
            ctx = SDL.SDL_GL_CreateContext(sdlWin);
            if (ctx == IntPtr.Zero) return false;
            if (!GL.CheckStringSDL(true)) {
                SDL.SDL_GL_DeleteContext(ctx);
                ctx = IntPtr.Zero;
            }
            GL.GLES = true;
            return true;
        }
        
        [DllImport("user32.dll", SetLastError=true)]
        static extern bool SetProcessDPIAware();
        
        private TimeSpan accumulatedTime;
        private TimeSpan lastTime;
        TimeSpan TimeStep = TimeSpan.FromTicks(166667);

        TimeSpan Accumulate(Stopwatch sw)
        {
            var current = sw.Elapsed;
            var diff = (current - lastTime);
            accumulatedTime += diff;
            lastTime = current;
            return diff;
        }

        protected virtual bool UseSplash => false;
        
        protected virtual Texture2D GetSplash()
        {
            return null;
        }

        static IntPtr CreateGLContext(IntPtr sdlWin)
        {
            IntPtr glcontext = IntPtr.Zero;
            if (Environment.GetEnvironmentVariable("LIBRELANCER_RENDERER") == "GLES" ||
                !CreateContextCore(sdlWin, out glcontext))
            {
                if (!CreateContextES(sdlWin, out glcontext))
                {
                    return IntPtr.Zero;
                }
            }
            GL.LoadSDL();
            return glcontext;
        }
        
        public void Run()
        {
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

            wakeEvent = SDL.SDL_RegisterEvents(1);
            SDL.SDL_SetHint(SDL.SDL_HINT_IME_INTERNAL_EDITING, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR, "0");
            //Set GL states
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            //Create Window

            var hiddenFlag = UseSplash ? SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN :  0;
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
            //Cursors
            curArrow = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
            curMove = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_CROSSHAIR);
            curTextInput = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM);
            curResizeNS = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENS);
            curResizeEW = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEWE);
            curResizeNESW = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENESW);
            curResizeNWSE = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENWSE);
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
            windowptr = sdlWin;
            var glContext = CreateGLContext(sdlWin);
            if (glContext == IntPtr.Zero)
            {
                Dialogs.CrashWindow.Run("Librelancer", "Failed to create OpenGL context",
                    "Your driver or gpu does not support at least OpenGL 3.2 or OpenGL ES 3.1\n" + SDL.SDL_GetError() ?? "");
            }
            Renderer = string.Format("{0} ({1})", GL.GetString(GL.GL_VERSION), GL.GetString(GL.GL_RENDERER));
            FLLog.Info("GL", $"Renderer: {GL.GetString(GL.GL_RENDERER)}");
            SetVSync(true);
            GL.ErrorChecking = true;
            //Init game state
            RenderContext = new RenderContext();
            FLLog.Info("GL", $"Max Anisotropy: {RenderContext.MaxAnisotropy}");
            FLLog.Info("GL", $"Max AA: {RenderContext.MaxSamples}");
            SDL.SDL_GetWindowSize(sdlWin, out int windowWidth, out int windowHeight);
            SDL.SDL_GL_GetDrawableSize(sdlWin, out  width, out height);
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
            if (UseSplash && (splashTexture = GetSplash()) != null)
            {
                var win2 = SDL.SDL_CreateWindow(
                    "Librelancer",
                    SDL.SDL_WINDOWPOS_CENTERED,
                    SDL.SDL_WINDOWPOS_CENTERED,
                    750, 250, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
                              SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
                              SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS |
                              SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
                SDL.SDL_GL_MakeCurrent(win2, glContext);
                RenderContext.ClearColor = Color4.Black;
                SDL.SDL_GL_GetDrawableSize(win2, out  var rw, out  var rh);
                RenderContext.ReplaceViewport(0,0, rw, rh);
                RenderContext.ClearAll();
                RenderContext.Renderer2D.DrawImageStretched(splashTexture, new Rectangle(0,0,rw,rh), Color4.White, true);
                RenderContext.EndFrame();
                SDL.SDL_GL_SwapWindow(win2);
                Load();
                splashTexture.Dispose();
                SDL.SDL_GL_MakeCurrent(sdlWin, glContext);
                SDL.SDL_DestroyWindow(win2);
            }
            else
            {
                Load();
            }
            SDL.SDL_ShowWindow(sdlWin);
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
                //Window State
                var winFlags = (SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(sdlWin);
                Focused = (winFlags & SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS) ==
                          SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS ||
                          (winFlags & SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) ==
                          SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS;
                EventsThisFrame = false;
                //Get Size
                SDL.SDL_GetWindowSize(sdlWin, out  windowWidth, out  windowHeight);
                SDL.SDL_GL_GetDrawableSize(sdlWin, out  width, out  height);
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
                                if (WillClose != null) WillClose();
                                running = false; //TODO: Raise Event
                                break;
                            }
                        case SDL.SDL_EventType.SDL_MOUSEMOTION:
                            {
                                Mouse.X = (int) (scaleW * e.motion.x);
                                Mouse.Y = (int) (scaleH * e.motion.y);
                                Mouse.OnMouseMove();
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
                                Mouse.Wheel += e.wheel.y;
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
                        case SDL.SDL_EventType.SDL_KEYMAPCHANGED:
                            KeysExtensions.ResetKeyNames();
                            break;
                        case SDL.SDL_EventType.SDL_WINDOWEVENT:
                            if (e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                            {
                                OnResize();
                            }
                            break;
                        case SDL.SDL_EventType.SDL_DROPFILE:
                            {
                                var file = UnsafeHelpers.PtrToStringUTF8(e.drop.file);
                                OnDrop(file);
                                SDL.SDL_free(e.drop.file);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_CLIPBOARDUPDATE:
                            {
                                OnClipboardUpdate();
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
                Accumulate(timer);
                while (accumulatedTime >= TimeStep)
                {
                    CurrentTick++;
                    Update(TimeStep.TotalSeconds);
                    accumulatedTime -= TimeStep;
                }

                if (!running)
                    break;
                Draw(elapsed);
                RenderContext.EndFrame();
                //Frame time before, FPS after
                var tk = timer.Elapsed.TotalSeconds - totalTime;
                frameTime = CalcAverageTime(tk);
                if (_screenshot)
                {
                    TakeScreenshot();
                    _screenshot = false;
                }
                GLSwap.SwapWindow(sdlWin, isVsync, fullscreen);
                if (GL.FrameHadErrors()) //If there was a GL error, track it down.
                    GL.ErrorChecking = true;
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
            Cleanup();
            SDL.SDL_Quit();
        }

        protected virtual void OnClipboardUpdate()
        {
        }
        
        protected virtual void OnResize()
        {
        }
        protected virtual void OnDrop(string file)
        {
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
            Cleanup();
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
        protected virtual void Load()
        {

        }
        protected virtual void Update(double elapsed)
        {

        }
        protected virtual void Draw(double elapsed)
        {

        }
        protected virtual void Cleanup()
        {

        }
    }
}

