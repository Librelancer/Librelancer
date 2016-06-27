/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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

		public VertexBuffer(Type type, int length, bool isStream = false)
        {
            VBO = GL.GenBuffer();
			var usageHint = isStream ? GL.GL_STREAM_DRAW : GL.GL_DYNAMIC_DRAW;
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
			GLBind.VertexBuffer(VBO);
			GL.BufferData (GL.GL_ARRAY_BUFFER, (IntPtr)(length * decl.Stride), IntPtr.Zero, usageHint);
			decl.SetPointers ();
			VertexCount = length;
        }

		public void SetData<T>(T[] data, int? length = null) where T : struct
        {
            if (typeof(T) != type)
                throw new Exception("Data must be of type " + type.FullName);
			int len = length ?? data.Length;
			GLBind.VertexBuffer (VBO);
			var handle = GCHandle.Alloc (data, GCHandleType.Pinned);
			GL.BufferSubData (GL.GL_ARRAY_BUFFER, IntPtr.Zero, (IntPtr)(len * decl.Stride), handle.AddrOfPinnedObject());
			handle.Free ();
        }

		public void Draw(PrimitiveTypes primitiveType, int baseVertex, int startIndex, int primitiveCount)
		{
			RenderState.Instance.Apply ();
			int indexElementCount = primitiveType.GetArrayLength (primitiveCount);
			GLBind.VertexBuffer(VBO);
			GLBind.VertexArray (VAO);
			GL.DrawElementsBaseVertex (primitiveType.GLType (),
				indexElementCount,
				GL.GL_UNSIGNED_SHORT,
				(IntPtr)(startIndex * 2),
				baseVertex);
			TotalDrawcalls++;
		}
		public void Draw(PrimitiveTypes primitiveType, int primitiveCount)
		{
			RenderState.Instance.Apply ();
			GLBind.VertexBuffer (VBO);
			GLBind.VertexArray (VAO);
			if (HasElements) {
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
			GLBind.VertexBuffer(VBO);
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
			GLBind.VertexBuffer(VBO);
			GLBind.VertexArray (VAO);
			GL.BindBuffer (GL.GL_ARRAY_BUFFER, elems.Handle);
			HasElements = true;
        }

        public void Dispose()
        {
            GL.DeleteBuffer(VBO);
			GL.DeleteVertexArray (VAO);
        }
    }
}
