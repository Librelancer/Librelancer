using System;

namespace LibreLancer.Graphics.Backends.OpenGL;

class SyncPoint
{
	IntPtr handle = 0;
	bool signalled = false;

	public void Set()
	{
		handle = GL.FenceSync(GL.GL_SYNC_GPU_COMMANDS_COMPLETE, 0);
	}

	public bool Passed()
	{
		if(signalled)
		{
			return true;
		}
		if(handle == IntPtr.Zero)
		{
			return false;
		}
		uint r = GL.ClientWaitSync(handle, 0, 0);
        if(r == GL.GL_CONDITION_SATISFIED || r == GL.GL_ALREADY_SIGNALED)
        {
			GL.DeleteSync(handle);
			signalled = true;
		}
		return signalled;
	}

	public void Wait()
	{
		if(signalled || handle == IntPtr.Zero)
			return;
		uint r;
        do
        {
            r = GL.ClientWaitSync(handle, 0, 1000);
        } while (r != GL.GL_CONDITION_SATISFIED && r != GL.GL_ALREADY_SIGNALED);
        signalled = true;
        GL.DeleteSync(handle);
        handle = IntPtr.Zero;
	}
}
