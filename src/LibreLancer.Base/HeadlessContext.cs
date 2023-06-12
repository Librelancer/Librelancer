using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

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
}