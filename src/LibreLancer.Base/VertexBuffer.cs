// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LibreLancer.Vertices;

namespace LibreLancer
{
    public class VertexBuffer : IDisposable
    {
		public static int TotalDrawcalls = 0;
        public static int TotalBuffers = 0;
        public int VertexCount { get; private set; }
        uint VBO;
		uint VAO;
		public bool HasElements = false;
		Type type;
		VertexDeclaration decl;
		IVertexType vertextype;
		public IVertexType VertexType {
			get {
				return vertextype;
			}
		}
		ElementBuffer _elements;
		public ElementBuffer Elements
		{
			get
			{
				return _elements;
			}
		}
		public VertexBuffer(Type type, int length, bool isStream = false)
        {
            TotalBuffers++;
            VBO = GL.GenBuffer();
			var usageHint = isStream ? GL.GL_STREAM_DRAW : GL.GL_STATIC_DRAW;
            this.type = type;
            try
            {
				vertextype = (IVertexType)Activator.CreateInstance (type);
				decl = vertextype.GetVertexDeclaration();
            }
            catch (Exception)
            {
                throw new Exception(string.Format("{0} is not a valid IVertexType", type.FullName));
            }
			GL.GenVertexArrays (1, out VAO);
            GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
			GL.BufferData (GL.GL_ARRAY_BUFFER, (IntPtr)(length * decl.Stride), IntPtr.Zero, usageHint);
			decl.SetPointers ();
			VertexCount = length;
        }

        public VertexBuffer(VertexDeclaration decl, int length, bool isStream = false)
        {
            this.decl = decl;
            VBO = GL.GenBuffer();
            var usageHint = isStream ? GL.GL_STREAM_DRAW : GL.GL_STATIC_DRAW;
            GL.GenVertexArrays(1, out VAO);
            GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            GL.BufferData(GL.GL_ARRAY_BUFFER, (IntPtr)(length * decl.Stride), IntPtr.Zero, usageHint);
            decl.SetPointers();
            VertexCount = length;
        }

		public void SetData<T>(T[] data, int? length = null, int? start = null) where T : struct
        {
            if (typeof(T) != type && typeof(T) != typeof(byte))
                throw new Exception("Data must be of type " + type.FullName);
			int len = length ?? data.Length;
            int s = start ?? 0;
			GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
			var handle = GCHandle.Alloc (data, GCHandleType.Pinned);
			GL.BufferSubData (GL.GL_ARRAY_BUFFER, (IntPtr)(s * decl.Stride), (IntPtr)(len * decl.Stride), handle.AddrOfPinnedObject());
			handle.Free ();
        }

		public void Draw(PrimitiveTypes primitiveType, int baseVertex, int startIndex, int primitiveCount)
		{
			RenderState.Instance.Apply ();
			int indexElementCount = primitiveType.GetArrayLength (primitiveCount);
			GLBind.VertexArray (VAO);
			GL.DrawElementsBaseVertex (primitiveType.GLType (),
				indexElementCount,
				GL.GL_UNSIGNED_SHORT,
				(IntPtr)(startIndex * 2),
				baseVertex);
			TotalDrawcalls++;
		}
        internal void Bind()
        {
            GLBind.VertexArray(VAO);
        }
		public void Draw(PrimitiveTypes primitiveType, int primitiveCount)
		{
			RenderState.Instance.Apply ();
			GLBind.VertexArray (VAO);
			if (HasElements) {
                GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, _elements.Handle);
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
			TotalDrawcalls++;
		}
		public void Draw(PrimitiveTypes primitiveType,int start, int primitiveCount)
		{
			RenderState.Instance.Apply();
			GLBind.VertexArray(VAO);
			if (HasElements)
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
			TotalDrawcalls++;
		}
        public void SetElementBuffer(ElementBuffer elems)
        {
			GLBind.VertexArray (VAO);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, elems.Handle);

			HasElements = true;
			_elements = elems;
            elems.VertexBuffer = this;
        }
		public void UnsetElementBuffer()
		{
			GLBind.VertexArray(VAO);
			GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, 0);
			HasElements = false;
			_elements = null;
            _elements.VertexBuffer = null;
		}
        public void Dispose()
        {
            TotalBuffers--;
            GL.DeleteBuffer(VBO);
			GL.DeleteVertexArray (VAO);
            GLBind.VertexArray(0);
        }
    }
}
