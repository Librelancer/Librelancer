using System;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Threading;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Backends.Null;
using LibreLancer.Platforms;

namespace LibreLancer;

class NullGame : IGame
{
    private ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
    public void QueueUIThread(Action work) => actions.Enqueue(work);

    public void WaitForEvent(int timeout)
    {
    }

    public void InterruptWait()
    {
    }

    public void BringToFront()
    {
    }

    public bool EventsThisFrame => true;
    public float DpiScale => 1;
    public int Width => 1024;
    public int Height => 768;
    public void SetWindowIcon(int width, int height, ReadOnlySpan<Bgra8> data)
    {
    }

    public ScreenshotSaveHandler OnScreenshotSave { get; set; }

    public bool RelativeMouseMode { get; set; }

    public bool Focused => true;
    public string Title { get; set; }
    public Point MinimumWindowSize { get; set; }
    public void SetVSync(bool vsync)
    {
    }

    public int MaxIterations = 0;

    private bool running = false;
    public void Run(Game loop)
    {
        var prevDrivers = Environment.GetEnvironmentVariable("ALSOFT_DRIVERS");
        Environment.SetEnvironmentVariable("ALSOFT_DRIVERS", "null");
        mythread = Thread.CurrentThread.ManagedThreadId;
        running = true;
        loop.OnLoad();
        TotalTime = 0;
        int iterations = 0;
        while (running)
        {
            while (actions.TryDequeue(out var a))
                a();
            iterations++;
            if (MaxIterations > 0 && iterations > MaxIterations)
                throw new TimeoutException("Exceeded max main loop iterations");
            loop.OnUpdate(1 / 60.0);
            loop.OnDraw(1 / 60.0);
            TotalTime += 1 / 60.0;
            OnTick?.Invoke();
        }
        Environment.SetEnvironmentVariable("ALSOFT_DRIVERS", prevDrivers);
    }

    public void Exit()
    {
        running = false;
    }

    public void Crashed()
    {
    }

    private int mythread;
    public bool IsUiThread() => Thread.CurrentThread.ManagedThreadId == mythread;


    public double TotalTime { get; set;  }
    public double TimerTick => TotalTime;
    public double RenderFrequency => 60.0;
    public double FrameTime => 1 / 60.0;
    public string Renderer => "NULL";
    public RenderContext RenderContext { get; set; } = new RenderContext(new NullRenderContext());
    public Mouse Mouse { get; set; } = new Mouse();
    public Keyboard Keyboard { get; set; } = new Keyboard();

    public Action OnTick;

    public void EnableTextInput()
    {
    }

    public void DisableTextInput()
    {
    }

    public void ToggleFullScreen()
    {
    }

    public void Screenshot(string filename)
    {
    }

    private object clipboard;

    public ClipboardContents ClipboardStatus()
    {
        if (clipboard is string)
            return ClipboardContents.Text;
        else if (clipboard is byte[])
            return ClipboardContents.Array;
        else
            return ClipboardContents.None;
    }

    public string GetClipboardText() => clipboard as string;

    public void SetClipboardText(string text) => clipboard = text;

    public byte[] GetClipboardArray() => clipboard as byte[];

    public void SetClipboardArray(byte[] array) => clipboard = array;

    public CursorKind CursorKind { get; set; }
}
