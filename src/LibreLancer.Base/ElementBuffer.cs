using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
    public class ElementBuffer : IDisposable
    {
        public int IndexCount { get; private set;  }
        public int Handle;
        public ElementBuffer(int count)
        {
            IndexCount = count;
            Handle = GL.GenBuffer();
        }
        public void SetData(short[] data)
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Handle);
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(data.Length * 2), data, BufferUsageHint.StaticDraw);
        }
        public void SetData(ushort[] data)
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Handle);
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(data.Length * 2), data, BufferUsageHint.StaticDraw);
        }
        public void Dispose()
        {
            GL.DeleteBuffer(Handle);
        }
    }
}
