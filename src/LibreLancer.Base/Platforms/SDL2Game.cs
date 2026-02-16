// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using LibreLancer.Dialogs;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Platforms;

internal class SDL2Game : IGame
{
    private int width;
    private int height;
    private double totalTime;
    private bool allowScreensaver;
    private bool running = false;
    private string title = "LibreLancer";
    private IntPtr windowptr;
    private double renderFrequency;
    public Mouse Mouse { get; } = new();
    public Keyboard Keyboard { get; } = new();
    private ConcurrentQueue<Action> actions = new();
    private int mythread;
    private uint wakeEvent;

    public ScreenshotSaveHandler? OnScreenshotSave { get; set; }

    public RenderContext RenderContext { get; private set; } = null!;
    public string? Renderer
    {
        get; private set;
    }

    public SDL2Game(int w, int h, bool allowScreensaver)
    {
        FLLog.Warning("Platform", "SDL2 backend in use, expect bugs (please install SDL3)");
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
            {
                SDL2.SDL_SetRelativeMouseMode(value ? SDL2.SDL_bool.SDL_TRUE : SDL2.SDL_bool.SDL_FALSE);
            }

            _relativeMouseMode = value;
        }
    }

    public float DpiScale { get; set; } = 1;
    public int Width => width;

    public unsafe void SetWindowIcon(int width, int height, ReadOnlySpan<Bgra8> data)
    {
        IntPtr surface;
        fixed (Bgra8* ptr = &data.GetPinnableReference())
        {
            surface = SDL2.SDL_CreateRGBSurfaceFrom(
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
        SDL2.SDL_SetWindowIcon(windowptr, surface);
        SDL2.SDL_FreeSurface(surface);
    }

    public ClipboardContents ClipboardStatus()
    {
        if (SDL2.SDL_HasClipboardText() != SDL2.SDL_bool.SDL_TRUE)
        {
            return ClipboardContents.None;
        }

        var text = SDL2.SDL_GetClipboardText();
        return L85.IsL85String(text) ? ClipboardContents.Array : ClipboardContents.Text;
    }

    public string? GetClipboardText()
    {
        var text = SDL2.SDL_GetClipboardText();
        return L85.IsL85String(text) ? "" : text;
    }

    public void SetClipboardText(string? text)
    {
        SDL2.SDL_SetClipboardText(text);
    }

    public byte[]? GetClipboardArray()
    {
        var text = SDL2.SDL_GetClipboardText();
        return L85.IsL85String(text) ? L85.FromL85String(text) : [];
    }

    public void SetClipboardArray(byte[] array)
    {
        SDL2.SDL_SetClipboardText(L85.ToL85String(array));
    }

    private IntPtr curArrow;
    private IntPtr curMove;
    private IntPtr curTextInput;
    private IntPtr curResizeNS;
    private IntPtr curResizeEW;
    private IntPtr curResizeNESW;
    private IntPtr curResizeNWSE;
    private IntPtr curNotAllowed;
    private CursorKind cursorKind = CursorKind.Arrow;
    public CursorKind CursorKind
    {
        get => cursorKind;
        set
        {
            if (cursorKind == value) return;
            cursorKind = value;
            switch (cursorKind)
            {
                case CursorKind.Arrow:
                    SDL2.SDL_SetCursor(curArrow);
                    break;
                case CursorKind.Move:
                    SDL2.SDL_SetCursor(curMove);
                    break;
                case CursorKind.TextInput:
                    SDL2.SDL_SetCursor(curTextInput);
                    break;
                case CursorKind.ResizeNS:
                    SDL2.SDL_SetCursor(curResizeNS);
                    break;
                case CursorKind.ResizeEW:
                    SDL2.SDL_SetCursor(curResizeEW);
                    break;
                case CursorKind.ResizeNESW:
                    SDL2.SDL_SetCursor(curResizeNESW);
                    break;
                case CursorKind.ResizeNWSE:
                    SDL2.SDL_SetCursor(curResizeNWSE);
                    break;
                case CursorKind.NotAllowed:
                    SDL2.SDL_SetCursor(curNotAllowed);
                    break;
            }
            SDL2.SDL_ShowCursor(value == CursorKind.None ? 0 : 1);
        }
    }

    public int Height => height;
    public double TotalTime => totalTime;

    private Stopwatch? timer;
    public double TimerTick => timer!.ElapsedMilliseconds / 1000.0;

    public string Title
    {
        get => title;
        set
        {
            title = value;
            SDL2.SDL_SetWindowTitle(windowptr, title);
        }
    }

    public double RenderFrequency => renderFrequency;

    private double frameTime;
    public double FrameTime => frameTime;


    public void QueueUIThread(Action work)
    {
        actions.Enqueue(work);
        InterruptWait();
    }

    public bool IsUiThread() =>  Thread.CurrentThread.ManagedThreadId == mythread;

    private string? _screenShotPath;
    private bool _screenshot;
    public void Screenshot(string filename)
    {
        _screenShotPath = filename;
        _screenshot = true;
    }

    private unsafe void TakeScreenshot()
    {
        GL.ReadBuffer(GL.GL_BACK);
        var colorData = new Bgra8[width * height];
        fixed (Bgra8* ptr = colorData)
        {
            GL.ReadPixels(0, 0, width, height, GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, (IntPtr)ptr);
        }

        for (int i = 0; i < colorData.Length; i++)
        {
            colorData[i].A = 0xFF;
        }

        OnScreenshotSave?.Invoke(_screenShotPath, width, height, colorData);
    }

    private bool isVsync = false;

    public void SetVSync(bool vsync)
    {
        isVsync = vsync;
    }

    public bool IsFullScreen { get; set; }

    public void SetFullScreen(bool fullscreen)
    {
        if (!fullscreen)
            SDL2.SDL_SetWindowFullscreen(windowptr, 0);
        else
            SDL2.SDL_SetWindowFullscreen(windowptr, (int)SDL2.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
        IsFullScreen = fullscreen;
    }

    private Point minWindowSize = Point.Zero;

    public Point MinimumWindowSize
    {
        get => minWindowSize;
        set
        {
            minWindowSize = value;
            if (windowptr != IntPtr.Zero)
                SDL2.SDL_SetWindowMinimumSize(windowptr, value.X, value.Y);
        }
    }

    public void BringToFront()
    {
        if(windowptr != IntPtr.Zero)
            SDL2.SDL_RaiseWindow(windowptr);
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
        var ev = new SDL2.SDL_Event();
        ev.type = (SDL2.SDL_EventType) wakeEvent;
        SDL2.SDL_PushEvent(ref ev);
    }

    public void Yield()
    {
        if (mythread != Thread.CurrentThread.ManagedThreadId)
        {
            throw new InvalidOperationException();
        }

        RenderContext!.Backend.QueryFences();
        while (actions.TryDequeue(out Action? work))
            work?.Invoke();
    }

    public bool Focused { get; private set; }
    public bool EventsThisFrame { get; private set; }


    [DllImport("user32.dll", SetLastError=true)]
    private static extern bool SetProcessDPIAware();


    public void Run(Game loop)
    {
        SDL2.SDL_SetHint(SDL2.SDL_HINT_VIDEO_ALLOW_SCREENSAVER, allowScreensaver ? "1" : "0");
        //Try to set DPI Awareness on Win32
        if (Platform.RunningOS == OS.Windows)
        {
            try
            {
                SetProcessDPIAware();
            }
            catch
            {
                // ignored
            }
        }

        FLLog.Info("Engine", "Version: " + Platform.GetInformationalVersion<Game>());
        //TODO: This makes i5-7200U on mesa 18 faster, but this should probably be a configurable option
        bool setMesaThread = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("mesa_glthread"));
        if(setMesaThread) Environment.SetEnvironmentVariable("mesa_glthread", "true");
        if (SDL2.SDL_Init(SDL2.SDL_INIT_VIDEO) != 0)
        {
            FLLog.Error("SDL", "SDL_Init failed, exiting.");
            return;
        }
        KeysExtensions.FillKeyNamesSDL();

        wakeEvent = SDL2.SDL_RegisterEvents(1);
        SDL2.SDL_SetHint(SDL2.SDL_HINT_IME_INTERNAL_EDITING, "1");
        SDL2.SDL_SetHint(SDL2.SDL_HINT_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR, "0");

        //Set GL states
        SDL2.SDL_GL_SetAttribute(SDL2.SDL_GLattr.SDL_GL_RED_SIZE, 8);
        SDL2.SDL_GL_SetAttribute(SDL2.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
        SDL2.SDL_GL_SetAttribute(SDL2.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
        SDL2.SDL_GL_SetAttribute(SDL2.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);

        //Create Window
        var hiddenFlag = loop.Splash ? SDL2.SDL_WindowFlags.SDL_WINDOW_HIDDEN :  0;
        var flags = SDL2.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL2.SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                    SDL2.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI | hiddenFlag;

        if (IsFullScreen)
        {
            flags |= SDL2.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
        }

        var sdlWin = SDL2.SDL_CreateWindow(
            "LibreLancer",
            SDL2.SDL_WINDOWPOS_CENTERED,
            SDL2.SDL_WINDOWPOS_CENTERED,
            width,
            height,
            flags
        );

        Platform.Init(SDL2.SDL_GetCurrentVideoDriver());

        //Cursors
        curArrow = SDL2.SDL_CreateSystemCursor(SDL2.SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
        curMove = SDL2.SDL_CreateSystemCursor(SDL2.SDL_SystemCursor.SDL_SYSTEM_CURSOR_CROSSHAIR);
        curTextInput = SDL2.SDL_CreateSystemCursor(SDL2.SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM);
        curResizeNS = SDL2.SDL_CreateSystemCursor(SDL2.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENS);
        curResizeEW = SDL2.SDL_CreateSystemCursor(SDL2.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEWE);
        curResizeNESW = SDL2.SDL_CreateSystemCursor(SDL2.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENESW);
        curResizeNWSE = SDL2.SDL_CreateSystemCursor(SDL2.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENWSE);
        curNotAllowed = SDL2.SDL_CreateSystemCursor(SDL2.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NO);

        //Window sizing
        if (sdlWin == IntPtr.Zero)
        {
            CrashWindow.Run("Librelancer", "Failed to create SDL window",
                "SDL Error: " + (SDL2.SDL_GetError() ?? ""));
            return;
        }

        if (minWindowSize != Point.Zero)
        {
            SDL2.SDL_SetWindowMinimumSize(sdlWin, minWindowSize.X, minWindowSize.Y);
        }

        SDL2.SDL_EventState(SDL2.SDL_EventType.SDL_DROPFILE, SDL2.SDL_ENABLE);
        SDL2.SDL_EventState(SDL2.SDL_EventType.SDL_SYSWMEVENT, SDL2.SDL_ENABLE);
        windowptr = sdlWin;
        IRenderContext? renderBackend = GLRenderContext.Create(sdlWin);

        if (renderBackend == null)
        {
            CrashWindow.Run("Librelancer", "Failed to create OpenGL context",
                "Your driver or gpu does not support at least OpenGL 3.2 or OpenGL ES 3.1\n" + SDL2.SDL_GetError() ?? "");
            return;
        }

        Renderer = renderBackend.GetRenderer();
        FLLog.Info("Graphics", $"Renderer: {Renderer}");
        SetVSync(true);
        //Init game state
        RenderContext = new RenderContext(renderBackend);
        FLLog.Info("Graphics", $"Max Anisotropy: {RenderContext.MaxAnisotropy}");
        FLLog.Info("Graphics", $"Max AA: {RenderContext.MaxSamples}");
        SDL2.SDL_GetWindowSize(sdlWin, out int windowWidth, out int windowHeight);
        var drawable = RenderContext.Backend.GetDrawableSize(sdlWin);
        RenderContext.SetDrawableSize(drawable);
        width = drawable.X;
        height = drawable.Y;
        var scaleW = (float) width / windowWidth;
        var scaleH = (float) height / windowHeight;
        if (Platform.RunningOS != OS.Windows) DpiScale = scaleH;
        else
        {
            if (SDL2.SDL_GetDisplayDPI(0, out float ddpi, out _, out _) == 0)
            {
                DpiScale = ddpi / 96.0f;
            }
        }
        FLLog.Info("GL", $"Dpi Scale: {DpiScale:F4}");
        Texture2D? splashTexture;
        if (loop.Splash && (splashTexture = loop.GetSplashInternal()) != null)
        {
            var win2 = SDL2.SDL_CreateWindow(
                "Librelancer",
                SDL2.SDL_WINDOWPOS_CENTERED,
                SDL2.SDL_WINDOWPOS_CENTERED,
                750, 250, SDL2.SDL_WindowFlags.SDL_WINDOW_OPENGL |
                          SDL2.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
                          SDL2.SDL_WindowFlags.SDL_WINDOW_BORDERLESS |
                          SDL2.SDL_WindowFlags.SDL_WINDOW_SHOWN);
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
            SDL2.SDL_DestroyWindow(win2);
        }
        else
        {
            loop.OnLoad();
        }
        SDL2.SDL_ShowWindow(sdlWin);
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
        SDL2.SDL_Event e = new SDL2.SDL_Event();
        SDL2.SDL_StopTextInput();
        MouseButtons doRelease = 0;
        while (running)
        {
            events.Poll();
            //Window State
            var winFlags = (SDL2.SDL_WindowFlags)SDL2.SDL_GetWindowFlags(sdlWin);
            Focused = (winFlags & SDL2.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS) ==
                      SDL2.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS ||
                      (winFlags & SDL2.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) ==
                      SDL2.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS;
            EventsThisFrame = false;
            //Get Size
            SDL2.SDL_GetWindowSize(sdlWin, out  windowWidth, out  windowHeight);
            var dw = RenderContext.Backend.GetDrawableSize(sdlWin);
            RenderContext.SetDrawableSize(dw);
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
                if (SDL2.SDL_WaitEventTimeout(out e, waitTimeout) != 0)
                {
                    eventWaited = true;
                }
            }
            Mouse.Wheel = 0;
            //Pump message queue
            while (eventWaited || SDL2.SDL_PollEvent(out e) != 0)
            {
                eventWaited = false;
                EventsThisFrame = true;
                switch (e.type)
                {
                    case SDL2.SDL_EventType.SDL_QUIT:
                    {
                        if(loop.OnWillClose())
                            running = false;
                        break;
                    }
                    case SDL2.SDL_EventType.SDL_MOUSEMOTION:
                    {
                        var x = RelativeMouseMode ? e.motion.xrel : e.motion.x;
                        var y = RelativeMouseMode ? e.motion.yrel : e.motion.y;
                        Mouse.X = (int) (scaleW * e.motion.x);
                        Mouse.Y = (int) (scaleH * e.motion.y);
                        Mouse.OnMouseMove((int)(scaleW * x),(int)(scaleH * y));
                        break;
                    }
                    case SDL2.SDL_EventType.SDL_MOUSEBUTTONDOWN:
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
                    case SDL2.SDL_EventType.SDL_MOUSEBUTTONUP:
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
                    case SDL2.SDL_EventType.SDL_MOUSEWHEEL:
                    {
                        Mouse.OnMouseWheel(e.wheel.x, e.wheel.y);
                        break;
                    }
                    case SDL2.SDL_EventType.SDL_TEXTINPUT:
                    {
                        Keyboard.OnTextInput(GetEventText(ref e));
                        break;
                    }
                    case SDL2.SDL_EventType.SDL_KEYDOWN:
                    {
                        Keyboard.OnKeyDown((Keys)e.key.keysym.scancode, (KeyModifiers)e.key.keysym.mod, e.key.repeat != 0);
                        break;
                    }
                    case SDL2.SDL_EventType.SDL_KEYUP:
                    {
                        Keyboard.OnKeyUp((Keys)e.key.keysym.scancode, (KeyModifiers)e.key.keysym.mod);
                        break;
                    }
                    case SDL2.SDL_EventType.SDL_SYSWMEVENT:
                    {
                        if (Platform.RunningOS == OS.Windows)
                        {
                            unsafe
                            {
                                SDL2.SDL_SysWMmsg_WINDOWS* ev = (SDL2.SDL_SysWMmsg_WINDOWS*) e.syswm.msg;
                                events.WndProc(ev->msg, ev->wParam);
                            }
                        }
                        break;
                    }
                    case SDL2.SDL_EventType.SDL_KEYMAPCHANGED:
                        KeysExtensions.ResetKeyNames();
                        break;
                    case SDL2.SDL_EventType.SDL_WINDOWEVENT:
                        if (e.window.windowEvent == SDL2.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                        {
                            loop.SignalResize();
                        }
                        break;
                    case SDL2.SDL_EventType.SDL_DROPFILE:
                    {
                        var file = UnsafeHelpers.PtrToStringUTF8(e.drop.file);
                        loop.SignalDrop(file);
                        SDL2.SDL_free(e.drop.file);
                        break;
                    }
                    case SDL2.SDL_EventType.SDL_CLIPBOARDUPDATE:
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
            while (actions.TryDequeue(out Action? work))
            {
                work?.Invoke();
            }

            RenderContext.Backend.QueryFences();
            totalTime = timer.Elapsed.TotalSeconds;
            loop.OnUpdate(elapsed);
            if (!running)
            {
                break;
            }

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
            RenderContext.Backend.SwapWindow(sdlWin, isVsync, IsFullScreen);

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
        SDL2.SDL_Quit();
    }



    //TODO: Terrible Hack
    public void Crashed()
    {
        SDL2.SDL_Quit();
    }

    private const int FPS_MAXSAMPLES = 50;
    private int tickindex = 0;
    private double ticksum = 0;
    private double[] ticklist = new double[FPS_MAXSAMPLES];

    private double CalcAverageTick(double newtick)
    {
        ticksum -= ticklist[tickindex];
        ticksum += newtick;
        ticklist[tickindex] = newtick;
        if (++tickindex == FPS_MAXSAMPLES)
            tickindex = 0;
        return ((double)ticksum / FPS_MAXSAMPLES);
    }

    private int timeindex = 0;
    private double timesum = 0;
    private double[] timelist = new double[FPS_MAXSAMPLES];

    private double CalcAverageTime(double newtick)
    {
        timesum -= timelist[timeindex];
        timesum += newtick;
        timelist[timeindex] = newtick;
        if (++timeindex == FPS_MAXSAMPLES)
            timeindex = 0;
        return ((double)timesum / FPS_MAXSAMPLES);
    }

    //Convert from SDL2 button to saner button
    private MouseButtons GetMouseButton(byte b)
    {
        if (b == SDL2.SDL_BUTTON_LEFT)
            return MouseButtons.Left;
        if (b == SDL2.SDL_BUTTON_MIDDLE)
            return MouseButtons.Middle;
        if (b == SDL2.SDL_BUTTON_RIGHT)
            return MouseButtons.Right;
        if (b == SDL2.SDL_BUTTON_X1)
            return MouseButtons.X1;
        if (b == SDL2.SDL_BUTTON_X2)
            return MouseButtons.X2;
        throw new Exception("SDL2 gave undefined mouse button"); //should never happen
    }

    private unsafe string GetEventText(ref SDL2.SDL_Event e)
    {
        byte[] rawBytes = new byte[SDL2.SDL_TEXTINPUTEVENT_TEXT_SIZE];
        fixed (byte* txtPtr = e.text.text)
        {
            Marshal.Copy((IntPtr)txtPtr, rawBytes, 0, SDL2.SDL_TEXTINPUTEVENT_TEXT_SIZE);
        }
        int nullIndex = Array.IndexOf(rawBytes, (byte)0);
        string text = Encoding.UTF8.GetString(rawBytes, 0, nullIndex);
        return text;
    }

    private bool textInputEnabled = false;
    public void EnableTextInput()
    {
        if (!textInputEnabled)
        {
            SDL2.SDL_StartTextInput();
            textInputEnabled = true;
        }
    }
    public void DisableTextInput()
    {
        if (textInputEnabled)
        {
            SDL2.SDL_StopTextInput();
            textInputEnabled = false;
        }
    }

    public unsafe void SetTextInputRect(Rectangle? rect)
    {
        if (rect == null)
            SDL2.SDL_SetTextInputRect(null);
        else
        {
            var sr = new SDL2.SDL_Rect()
            {
                x = rect.Value.X,
                y = rect.Value.Y,
                w = rect.Value.Width,
                h = rect.Value.Height
            };
            SDL2.SDL_SetTextInputRect(&sr);
        }
    }

    public void Exit()
    {
        running = false;
    }
}
