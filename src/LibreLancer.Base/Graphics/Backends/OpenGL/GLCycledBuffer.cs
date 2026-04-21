using System;

namespace LibreLancer.Graphics.Backends.OpenGL;

abstract class GLCycledBuffer(GLRenderContext ctx) : IDisposable
{
    private bool allAllocated = false;
    internal SyncPoint?[] Fences = new SyncPoint?[3];
    internal uint[] IDs = [0, 0, 0];
    internal int ActiveIdx = 0;

    protected abstract uint GenerateBuffer();

    protected void AllocateBufferIndex(int idx)
    {
        IDs[idx] = GenerateBuffer();
        allAllocated = true;
        for (int i = 0; i < IDs.Length; i++)
        {
            if (IDs[i] == 0)
            {
                allAllocated = false;
                break;
            }
        }
    }

    protected int GetNextBuffer()
    {
        for (int i = 0; i < 3; i++)
        {
            int idx = (ActiveIdx + i) % 3;
            if (IDs[idx] == 0)
                continue;
            if (Fences[idx] == null)
            {
                return idx;
            }
            if (Fences[idx]!.Passed())
            {
                Fences[idx] = null;
                return idx;
            }
        }

        if (allAllocated)
        {
            int idx = (ActiveIdx + 1);
            Fences[idx]!.Wait();
            Fences[idx] = null;
            return idx;
        }

        for (int i = 0; i < IDs.Length; i++)
        {
            if (IDs[i] == 0)
            {
                AllocateBufferIndex(i);
                return i;
            }
        }

        throw new InvalidOperationException("Unreachable");
    }

    public void Dispose()
    {
        for (int i = 0; i < ctx.BoundBuffers.Length; i++)
        {
            if (ctx.BoundBuffers[i] == this)
            {
                ctx.BoundBuffers[i] = null;
            }
        }

        for (int i = 0; i < IDs.Length; i++)
        {
            if (IDs[i] != 0)
                GL.DeleteBuffer(IDs[i]);
        }
    }
}
