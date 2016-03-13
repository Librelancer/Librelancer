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
			int indexElementCount = primitiveType.GetArrayLength (primitiveCount);
			GLBind.VertexBuffer (VBO);
			GLBind.VertexArray (VAO);
			GL.DrawElements (primitiveType.GLType (),
				indexElementCount,
				DrawElementsType.UnsignedShort,
				IntPtr.Zero
			);
			TotalDrawcalls++;
		}

        public void SetElementBuffer(ElementBuffer elems)
        {
			GLBind.VertexBuffer(VBO);
			GLBind.VertexArray (VAO);
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, elems.Handle);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(VBO);
			GL.DeleteVertexArray (VAO);
        }
    }
}
