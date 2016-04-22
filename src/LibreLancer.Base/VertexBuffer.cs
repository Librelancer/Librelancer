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
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibreLancer.Vertices;

namespace LibreLancer
{
    public class VertexBuffer : IDisposable
    {
		public static int TotalDrawcalls = 0;
        public int VertexCount { get; private set; }
        public IVertexType VertexType;
        int VBO;
		int VAO;
		public bool HasElements = false;
		Type type;
		public VertexBuffer(Type type, int length, bool isStream = false)
        {
            VBO = GL.GenBuffer();
			var usageHint = isStream ? BufferUsageHint.StreamDraw : BufferUsageHint.DynamicDraw;
            this.type = type;
            try
            {
                VertexType = (IVertexType)Activator.CreateInstance(type);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("{0} is not a valid IVertexType", type.FullName));
            }
			VAO = GL.GenVertexArray ();
            GLBind.VertexArray(VAO);
			GLBind.VertexBuffer(VBO);
			GL.BufferData (BufferTarget.ArrayBuffer, length * VertexType.VertexSize (), IntPtr.Zero, usageHint);
			VertexType.SetVertexPointers (0);
			VertexCount = length;
        }

		public void SetData<T>(T[] data, int? length = null) where T : struct
        {
            if (typeof(T) != type)
                throw new Exception("Data must be of type " + type.FullName);
			int len = length ?? data.Length;
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			GL.BufferSubData (BufferTarget.ArrayBuffer, IntPtr.Zero, len * VertexType.VertexSize (), data);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

		public void Draw(PrimitiveTypes primitiveType, int baseVertex, int startIndex, int primitiveCount)
		{
			RenderState.Instance.Apply ();
			int indexElementCount = primitiveType.GetArrayLength (primitiveCount);
			GLBind.VertexBuffer(VBO);
			GLBind.VertexArray (VAO);
			GL.DrawElementsBaseVertex (primitiveType.GLType (),
				indexElementCount,
				DrawElementsType.UnsignedShort,
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
					DrawElementsType.UnsignedShort,
					IntPtr.Zero
				);
			} else {
				GL.DrawArrays (primitiveType.GLType (),
					0,
					primitiveCount
				);
			}
			TotalDrawcalls++;
		}

        public void SetElementBuffer(ElementBuffer elems)
        {
			GLBind.VertexBuffer(VBO);
			GLBind.VertexArray (VAO);
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, elems.Handle);
			HasElements = true;
        }

        public void Dispose()
        {
            GL.DeleteBuffer(VBO);
			GL.DeleteVertexArray (VAO);
        }
    }
}
