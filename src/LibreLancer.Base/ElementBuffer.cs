// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreLancer
{
    public unsafe class ElementBuffer : IDisposable
    {
        public int IndexCount { get; private set;  }
        public uint Handle;
        internal VertexBuffer VertexBuffer;
		bool isDynamic;
		public ElementBuffer(int count, bool isDynamic = false)
        {
			this.isDynamic = isDynamic;
            IndexCount = count;
            Handle = GL.GenBuffer();
            GLBind.VertexArray(RenderState.Instance.NullVAO);
			GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, Handle);
			GL.BufferData(GL.GL_ELEMENT_ARRAY_BUFFER, new IntPtr(count * 2), IntPtr.Zero, isDynamic ? GL.GL_DYNAMIC_DRAW : GL.GL_STATIC_DRAW);

		}
        public void SetData(short[] data)
        {
            GLBind.VertexArray(RenderState.Instance.NullVAO);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, Handle);			
            fixed(short* ptr = data) {
				GL.BufferData (GL.GL_ELEMENT_ARRAY_BUFFER, new IntPtr (data.Length * 2), (IntPtr)ptr, isDynamic ? GL.GL_DYNAMIC_DRAW : GL.GL_STATIC_DRAW);
			}
        }
        public void SetData(ushort[] data)
        {
			SetData(data, data.Length);
        }
        public void SetData(ushort[] data, int count, int start = 0)
		{
            GLBind.VertexArray(RenderState.Instance.NullVAO);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, Handle);
			fixed (ushort* ptr = data) {;
				GL.BufferSubData(GL.GL_ELEMENT_ARRAY_BUFFER, new IntPtr(start * 2), new IntPtr(count * 2), (IntPtr)ptr);
			}
		}
        public void Dispose()
        {
            GL.DeleteBuffer(Handle);
        }
    }
}
