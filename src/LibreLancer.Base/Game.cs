// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;
namespace LibreLancer
{
    public delegate void ScreenshotSaveHandler(string filename, int width, int height, byte[] data);
    public class Game : IUIThread
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
        public ScreenshotSaveHandler ScreenshotSave;
        public RenderState RenderState;
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
        public IntPtr GetGLProcAddress(string name)
        {
            return SDL.SDL_GL_GetProcAddress(name);
        }

        public void UnbindAll()
        {
            GLBind.VertexArray(RenderState.Instance.NullVAO);
        }
        public void TrashGLState()
        {
            GLBind.Trash();
            RenderState.Instance.Trash();
        }

        public void QueueUIThread(Action work)
        {
            actions.Enqueue(work);
        }
        public void EnsureUIThread(Action work)
        {
            if (Thread.CurrentThread.ManagedThreadId == mythread)
                work();
            else
            {
                bool done = false;
                actions.Enqueue(() =>
                {
                    work();
                    done = true;
                });
                while (!done)
                    Thread.Sleep(1);
            }
        }

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
                var c = RenderState.ClearColor;
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                    {
                        int offset = (y * height * 4) + (x * 4);
                        colordata[offset + 3] = 0xFF;
                    }

                ScreenshotSave(_screenshotpath, width, height, colordata);
            }

        }

        public void SetVSync(bool vsync)
        {
            if (vsync)
            {
                if (SDL.SDL_GL_SetSwapInterval(-1) < 0)
                    SDL.SDL_GL_SetSwapInterval(1);
            }
            else
            {
                SDL.SDL_GL_SetSwapInterval(0);
            }
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

        public event Action WillClose;
        public void Run()
        {
            FLLog.Info("Engine", "Version: " + Platform.GetInformationalVersion<Game>());
            //TODO: This makes i5-7200U on mesa 18 faster, but this should probably be a configurable option
            Environment.SetEnvironmentVariable("mesa_glthread", "true");
            SSEMath.Load();
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
            {
                FLLog.Error("SDL", "SDL_Init failed, exiting.");
                return;
            }
            SDL.SDL_SetHint(SDL.SDL_HINT_IME_INTERNAL_EDITING, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR, "0");
            //Set GL states
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            //Create Window
            var flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
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
            if (minWindowSize != Point.Zero)
            {
                SDL.SDL_SetWindowMinimumSize(sdlWin, minWindowSize.X, minWindowSize.Y);
            }
            if (sdlWin == IntPtr.Zero)
            {
                FLLog.Error("SDL", "Failed to create window, exiting.");
                return;
            }
            SDL.SDL_EventState(SDL.SDL_EventType.SDL_DROPFILE, SDL.SDL_ENABLE);
            windowptr = sdlWin;
            var glcontext = SDL.SDL_GL_CreateContext(sdlWin);
            if (glcontext == IntPtr.Zero || !GL.CheckStringSDL())
            {
                SDL.SDL_GL_DeleteContext(glcontext);
                if (Platform.RunningOS == OS.Windows)
                    SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "Librelancer", "Failed to create OpenGL context, exiting.", IntPtr.Zero);
                FLLog.Error("OpenGL", "Failed to create OpenGL context, exiting.");
                return;
            }
            else
            {
                GL.LoadSDL();
                Renderer = string.Format("{0} ({1})", GL.GetString(GL.GL_VERSION), GL.GetString(GL.GL_RENDERER));
            }
            SetVSync(true);
            //Init game state
            RenderState = new RenderState();
            Load();
            //Start game
            running = true;
            timer = new Stopwatch();
            timer.Start();
            double last = 0;
            double elapsed = 0;
            SDL.SDL_Event e;
            SDL.SDL_StopTextInput();
            while (running)
            {
                //Pump message queue
                while (SDL.SDL_PollEvent(out e) != 0)
                {
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
                                Mouse.X = e.motion.x;
                                Mouse.Y = e.motion.y;
                                Mouse.OnMouseMove();
                                break;
                            }
                        case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                            {
                                Mouse.X = e.button.x;
                                Mouse.Y = e.button.y;
                                var btn = GetMouseButton(e.button.button);
                                Mouse.Buttons |= btn;
                                Mouse.OnMouseDown(btn);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                            {
                                Mouse.X = e.button.x;
                                Mouse.Y = e.button.y;
                                var btn = GetMouseButton(e.button.button);
                                Mouse.Buttons &= ~btn;
                                Mouse.OnMouseUp(btn);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                            {
                                Mouse.OnMouseWheel(e.wheel.y);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_TEXTINPUT:
                            {
                                Keyboard.OnTextInput(GetEventText(ref e));
                                break;
                            }
                        case SDL.SDL_EventType.SDL_KEYDOWN:
                            {
                                Keyboard.OnKeyDown((Keys)e.key.keysym.sym, (KeyModifiers)e.key.keysym.mod, e.key.repeat != 0);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_KEYUP:
                            {
                                Keyboard.OnKeyUp((Keys)e.key.keysym.sym, (KeyModifiers)e.key.keysym.mod);
                                break;
                            }
                        case SDL.SDL_EventType.SDL_WINDOWEVENT:
                            if (e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                            {
                                SDL.SDL_GetWindowSize(windowptr, out width, out height);
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
                    }
                }

                //Do game things
                if (!running)
                    break;
                Action work;
                while (actions.TryDequeue(out work))
                    work();
                totalTime = timer.Elapsed.TotalSeconds;
                Update(elapsed);
                if (!running)
                    break;
                Draw(elapsed);
                //Frame time before, FPS after
                var tk = timer.Elapsed.TotalSeconds - totalTime;
                frameTime = CalcAverageTime(tk);
                if (_screenshot)
                {
                    TakeScreenshot();
                    _screenshot = false;
                }
                SDL.SDL_GL_SwapWindow(sdlWin);
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
            SDL.SDL_StartTextInput();
            textInputEnabled = true;
        }
        public void DisableTextInput()
        {
            SDL.SDL_StopTextInput();
            textInputEnabled = false;
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

