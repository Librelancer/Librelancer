using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer;

public class HeadlessContext : IGLWindow, IUIThread
{
    internal int UiThreadId;
    public bool IsUiThread() => UiThreadId == Thread.CurrentThread.ManagedThreadId;

    private ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

    public RenderContext RenderContext { get; internal set; }

    public void QueueUIThread(Action work)
    {
        if (IsUiThread()) work();
        else queue.Enqueue(work);
    }

    public void RunUIEvents()
    {
        if (!IsUiThread()) throw new InvalidOperationException();
        while (queue.Count > 0)
        {
            if (queue.TryDequeue(out var a))
                a();
        }
    }

    private HeadlessContext()
    {
    }

    public static HeadlessContext Create()
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
            throw new Exception("SDL_Init failed");
        var win = SDL.SDL_CreateWindow("Headless Librelancer",
            SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED,
            128, 128,
            SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL);
        if (win == IntPtr.Zero)
            throw new Exception("Failed to create hidden SDL window");
        var ctx = GLRenderContext.Create(win);
        if (ctx == null)
            throw new Exception("Failed to create OpenGL context");
        return new HeadlessContext()
        {
            RenderContext = new RenderContext(ctx),
            UiThreadId = Thread.CurrentThread.ManagedThreadId
        };
    }
}
