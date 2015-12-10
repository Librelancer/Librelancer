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
        public int VertexCount { get; private set; }
        public IVertexType VertexType;
        public int VBO;
        public int VAO;
        public bool HasElements = false;
        Type type;
		int currentOffset = 0;

        public VertexBuffer(Type type, int length)
        {
            VBO = GL.GenBuffer();
            this.type = type;
            try
            {
                VertexType = (IVertexType)Activator.CreateInstance(type);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("{0} is not a valid IVertexType", type.FullName));
            }
            VAO = GL.GenVertexArray();
            //GL.BindVertexArray(VAO);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			GLBind.VertexArrayPair(VBO, VAO);
            GL.BufferData(BufferTarget.ArrayBuffer, length * VertexType.VertexSize(), IntPtr.Zero, BufferUsageHint.StaticDraw);
            VertexType.SetVertexPointers(0);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            //GL.BindVertexArray(0);
			VertexCount = length;
        }

        public void SetData<T>(T[] data) where T : struct
        {
            if (typeof(T) != type)
                throw new Exception("Data must be of type " + type.FullName);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * VertexType.VertexSize(), data, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

		public void Draw(PrimitiveTypes primitiveType, int baseVertex, int startIndex, int primitiveCount)
		{
			int indexElementCount = primitiveType.GetArrayLength (primitiveCount);
			int vertexOffset = VertexType.VertexSize () * baseVertex;
			//GL.BindVertexArray(VAO);
			//GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			GLBind.VertexArrayPair(VBO, VAO);
			if (currentOffset != vertexOffset) {
				VertexType.SetVertexPointers (vertexOffset);
				currentOffset = vertexOffset;
			}
			GL.DrawElements (primitiveType.GLType (),
				indexElementCount,
				DrawElementsType.UnsignedShort,
				startIndex * 2);
			//GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			//GL.BindVertexArray(0);
		}

        public void SetElementBuffer(ElementBuffer elems)
        {
            //GL.BindVertexArray(VAO);
			GLBind.VertexArrayPair(VBO, VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elems.Handle);
            //GL.BindVertexArray(0);
            HasElements = true;
        }

        public void Dispose()
        {
            GL.DeleteBuffer(VBO);
            GL.DeleteVertexArray(VAO);
        }
    }
}
