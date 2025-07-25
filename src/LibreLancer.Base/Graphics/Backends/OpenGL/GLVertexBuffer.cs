// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    class GLVertexBuffer : IVertexBuffer
    {
        private bool isDisposed = false;
        public int VertexCount { get; private set; }
        uint VBO;
		uint VAO;
        bool streaming;
		Type type;

		VertexDeclaration decl;
		IVertexType vertextype;

        private GLElementBuffer glElements;

        public IVertexType VertexType => vertextype;

		public GLVertexBuffer(Type type, int length, bool isStream = false)
        {
            this.type = type;
            try
            {
				vertextype = (IVertexType)Activator.CreateInstance (type);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("{0} is not a valid IVertexType", type.FullName));
            }
            Create(length, isStream);
        }

        public GLVertexBuffer(IVertexType type, int length, bool isStream = false)
        {
            this.type = type.GetType();
            vertextype = type;
            Create(length, isStream);
        }

        void Create(int length, bool isStream)
        {
            decl = vertextype.GetVertexDeclaration();
            VBO = GL.GenBuffer();
            var usageHint = isStream ? GL.GL_STREAM_DRAW : GL.GL_STATIC_DRAW;
            streaming = isStream;
            GL.GenVertexArrays (1, out VAO);
            GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            GL.BufferData (GL.GL_ARRAY_BUFFER, (IntPtr)(length * decl.Stride), IntPtr.Zero, usageHint);
            if(isStream)
                buffer = UnsafeHelpers.Allocate(length * decl.Stride);
            SetPointers (0);
            VertexCount = length;
        }

		public unsafe void SetData<T>(ReadOnlySpan<T> data, int offset = 0) where T : unmanaged
        {
            if (typeof(T) != type && typeof(T) != typeof(byte))
                throw new Exception("Data must be of type " + type.FullName);
			GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            int sz = Unsafe.SizeOf<T>() * data.Length;
            fixed (T* ptr = &data.GetPinnableReference())
            {
                GL.BufferSubData (GL.GL_ARRAY_BUFFER, (IntPtr)(offset * decl.Stride), (IntPtr)(sz), (IntPtr)ptr);
            }
        }

        public void Expand(int newSize)
        {
            if (newSize < VertexCount)
                throw new InvalidOperationException();
            var newHandle = GL.GenBuffer();
            GL.BindBuffer(GL.GL_COPY_READ_BUFFER, VBO);
            GL.BindBuffer(GL.GL_COPY_WRITE_BUFFER, newHandle);
            GL.BufferData(GL.GL_COPY_WRITE_BUFFER, new IntPtr(newSize * decl.Stride), IntPtr.Zero, streaming ? GL.GL_STREAM_DRAW : GL.GL_STATIC_DRAW);
            GL.CopyBufferSubData(GL.GL_COPY_READ_BUFFER, GL.GL_COPY_WRITE_BUFFER, IntPtr.Zero, IntPtr.Zero, (IntPtr)(VertexCount * decl.Stride));
            GL.DeleteBuffer(VBO);
            VBO = newHandle;
            GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            SetPointers(0);
            VertexCount = newSize;
        }

		public void Draw(PrimitiveTypes primitiveType, int baseVertex, int startIndex, int primitiveCount)
		{
            if (primitiveCount == 0) throw new InvalidOperationException("primitiveCount can't be 0");
            if (glElements == null)
                throw new InvalidOperationException("Cannot use drawElementsBaseVertex without element buffer");
            EnsureBaseVertex(baseVertex);
            RenderContext.Instance.Apply ();
			int indexElementCount = primitiveType.GetArrayLength (primitiveCount);
			GLBind.VertexArray (VAO);
            if (GLExtensions.BaseVertex)
            {
                GL.DrawElementsBaseVertex (primitiveType.GLType (),
                    indexElementCount,
                    GL.GL_UNSIGNED_SHORT,
                    (IntPtr)(startIndex * 2),
                    baseVertex);
            }
            else
            {
                GL.DrawElements(primitiveType.GLType(),
                    indexElementCount,
                    GL.GL_UNSIGNED_SHORT,
                    (IntPtr)(startIndex * 2));
            }

		}

        public unsafe void DrawImmediateElements(PrimitiveTypes primitiveTypes, int baseVertex, ReadOnlySpan<ushort> elements)
        {
            if (elements.Length == 0) throw new InvalidOperationException("elements length can't be 0");
            EnsureBaseVertex(baseVertex);
            RenderContext.Instance.Apply();
            GLBind.VertexArray(VAO);
            var eb = GL.GenBuffer();
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, eb);
            fixed (ushort* ptr = &elements.GetPinnableReference())
                GL.BufferData(GL.GL_ELEMENT_ARRAY_BUFFER, (IntPtr)(elements.Length * 2), (IntPtr)ptr, GL.GL_STREAM_DRAW);
            if (GLExtensions.BaseVertex)
            {
                GL.DrawElementsBaseVertex(primitiveTypes.GLType(),elements.Length, GL.GL_UNSIGNED_SHORT, IntPtr.Zero, baseVertex);
            }
            else
            {
                GL.DrawElements(primitiveTypes.GLType(),
                    elements.Length,
                    GL.GL_UNSIGNED_SHORT,
                    0);
            }
            GL.DeleteBuffer(eb);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, glElements?.Handle ?? 0);
        }

        const int STREAM_FLAGS = GL.GL_MAP_WRITE_BIT | GL.GL_MAP_INVALIDATE_BUFFER_BIT;
        private NativeBuffer buffer;
        public IntPtr BeginStreaming()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(VertexBuffer));
            if (!streaming) throw new InvalidOperationException("not streaming buffer");
            return (IntPtr)buffer;
        }

        //Count is for if emulation is required
        public void EndStreaming(int count)
        {
            if (!streaming) throw new InvalidOperationException("not streaming buffer");
            if (count == 0) return;
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            GL.BufferData(GL.GL_ARRAY_BUFFER, (IntPtr)(VertexCount * decl.Stride), IntPtr.Zero, GL.GL_STREAM_DRAW);
            GL.BufferSubData(GL.GL_ARRAY_BUFFER, IntPtr.Zero, (IntPtr) (count * decl.Stride), (IntPtr)buffer);
        }

		public void Draw(PrimitiveTypes primitiveType, int primitiveCount)
		{
            if (isDisposed) throw new ObjectDisposedException(nameof(VertexBuffer));
            RenderContext.Instance.Apply ();
			DrawNoApply(primitiveType, primitiveCount);
		}


        void EnsureBaseVertex(int baseVertex)
        {
            if (GLExtensions.BaseVertex)
                return;
            if (baseVertex != _activeBaseVertex)
            {
                SetPointers(baseVertex);
            }
        }

        public void DrawNoApply(PrimitiveTypes primitiveType, int primitiveCount)
        {
            EnsureBaseVertex(0);
            GLBind.VertexArray (VAO);
            if (glElements != null) {
                GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, glElements.Handle);
                int indexElementCount = primitiveType.GetArrayLength (primitiveCount);
                GL.DrawElements (primitiveType.GLType (),
                    indexElementCount,
                    GL.GL_UNSIGNED_SHORT,
                    IntPtr.Zero
                );
            } else {
                int indexElementCount = primitiveType.GetArrayLength(primitiveCount);
                GL.DrawArrays (primitiveType.GLType (),
                    0,
                    indexElementCount
                );
            }
        }
		public void Draw(PrimitiveTypes primitiveType,int start, int primitiveCount)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(VertexBuffer));
			RenderContext.Instance.Apply();
            EnsureBaseVertex(0);
			GLBind.VertexArray(VAO);
			if (glElements != null)
			{
				int indexElementCount = primitiveType.GetArrayLength(primitiveCount);
				GL.DrawElements(primitiveType.GLType(),
					indexElementCount,
					GL.GL_UNSIGNED_SHORT,
				    (IntPtr)(2 * start)
				);
			}
			else
			{
				int indexElementCount = primitiveType.GetArrayLength(primitiveCount);
				GL.DrawArrays(primitiveType.GLType(),
					start,
					indexElementCount
				);
			}
		}

        private int _activeBaseVertex = 0;


        void SetPointers(int baseVertex)
        {
            _activeBaseVertex = baseVertex;
            IntPtr o = _activeBaseVertex * decl.Stride;
            GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            foreach (var e in decl.Elements) {
                GL.EnableVertexAttribArray (e.Slot);
                if(e.Integer)
                    GL.VertexAttribIPointer ((uint)e.Slot, e.Elements, (int)e.Type, decl.Stride, o + (IntPtr)(e.Offset));
                else
                    GL.VertexAttribPointer ((uint)e.Slot, e.Elements, (int)e.Type, e.Normalized, decl.Stride, o + (IntPtr)(e.Offset));
            }
        }

        public void SetElementBuffer(IElementBuffer elems)
        {
            glElements = (GLElementBuffer)elems;
			GLBind.VertexArray (VAO);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, glElements.Handle);
            glElements.VertexBuffers.Add(this);
        }

        internal void RefreshElementBuffer()
        {
            GLBind.VertexArray (VAO);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, glElements.Handle);
        }

		public void UnsetElementBuffer()
		{
			GLBind.VertexArray(VAO);
			GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, 0);
            glElements.VertexBuffers.Remove(this);
			glElements = null;
		}

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            buffer?.Dispose();
            GL.DeleteBuffer(VBO);
			GL.DeleteVertexArray (VAO);
            GLBind.VertexArray(0);
        }
    }
}
