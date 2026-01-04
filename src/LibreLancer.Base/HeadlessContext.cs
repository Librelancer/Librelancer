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

    private ConcurrentQueue<Action?> queue = new();

    public RenderContext RenderContext { get; internal set; } = null!;

    public void QueueUIThread(Action work)
    {
        if (!IsUiThread())
            queue.Enqueue(work);
        else
            work.Invoke();
    }

    public void RunUIEvents()
    {
        if (!IsUiThread()) throw new InvalidOperationException();
        while (queue.Count > 0)
        {
            if (queue.TryDequeue(out var a))
                a?.Invoke();
        }
    }

    private HeadlessContext()
    {
    }

    public static HeadlessContext Create()
    {
        IntPtr win;
        if (SDL3.Supported)
        {
            if (!SDL3.SDL_Init(SDL3.SDL_InitFlags.SDL_INIT_VIDEO))
            {
                throw new Exception("SDL_Init failed");
            }

            win = SDL3.SDL_CreateWindow("Headless Librelancer", 128, 128,
                SDL3.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL3.SDL_WindowFlags.SDL_WINDOW_OPENGL);
        }
        else
        {
            if (SDL2.SDL_Init(SDL2.SDL_INIT_VIDEO) != 0)
            {
                throw new Exception("SDL_Init failed");
            }

            win = SDL2.SDL_CreateWindow("Headless Librelancer",
                SDL2.SDL_WINDOWPOS_UNDEFINED, SDL2.SDL_WINDOWPOS_UNDEFINED,
                128, 128,
                SDL2.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL2.SDL_WindowFlags.SDL_WINDOW_OPENGL);
        }

        if (win == IntPtr.Zero)
        {
            throw new Exception("Failed to create hidden SDL window");
        }

        var ctx = GLRenderContext.Create(win);

        return ctx switch
        {
            null => throw new Exception("Failed to create OpenGL context"),
            _ => new HeadlessContext()
            {
                RenderContext = new RenderContext(ctx),
                UiThreadId = Thread.CurrentThread.ManagedThreadId
            }
        };
    }
}
