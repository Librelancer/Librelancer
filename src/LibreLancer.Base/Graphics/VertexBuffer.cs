// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Graphics
{
    public class VertexBuffer : IDisposable
    {
		public static int TotalDrawcalls = 0;
        public static int TotalBuffers = 0;

        private IVertexBuffer impl;

        public int VertexCount => impl.VertexCount;
        public bool HasElements => _elements != null;

        public IVertexType VertexType => impl.VertexType;
		ElementBuffer _elements;
		public ElementBuffer Elements
		{
			get
			{
				return _elements;
			}
		}


		public VertexBuffer(RenderContext context, Type type, int length, bool isStream = false)
        {
            TotalBuffers++;
            impl = context.Backend.CreateVertexBuffer(type, length, isStream);
        }

        public VertexBuffer(RenderContext context, IVertexType type, int length, bool isStream = false)
        {
            TotalBuffers++;
            impl = context.Backend.CreateVertexBuffer(type, length, isStream);
        }

        public void SetData<T>(ReadOnlySpan<T> data, int offset = 0) where T : unmanaged
            => impl.SetData(data, offset);

        public void Expand(int newSize)
            => impl.Expand(newSize);

        public void Draw(PrimitiveTypes primitiveType, int baseVertex, int startIndex, int primitiveCount)
            => impl.Draw(primitiveType, baseVertex, startIndex, primitiveCount);

        public unsafe void DrawImmediateElements(PrimitiveTypes primitiveTypes, int baseVertex,
            ReadOnlySpan<ushort> elements)
            => impl.DrawImmediateElements(primitiveTypes, baseVertex, elements);

        public IntPtr BeginStreaming() => impl.BeginStreaming();

        //Count is for if emulation is required
        public void EndStreaming(int count) => impl.EndStreaming(count);

        public void Draw(PrimitiveTypes primitiveType, int primitiveCount)
        {
            TotalDrawcalls++;
            impl.Draw(primitiveType, primitiveCount);
        }

        internal void DrawNoApply(PrimitiveTypes primitiveType, int primitiveCount)
        {
            TotalDrawcalls++;
            impl.DrawNoApply(primitiveType, primitiveCount);
        }


        public void Draw(PrimitiveTypes primitiveType, int start, int primitiveCount)
        {
            TotalDrawcalls++;
            impl.Draw(primitiveType, start, primitiveCount);
        }

        public void SetElementBuffer(ElementBuffer elems)
        {
            _elements = elems;
            impl.SetElementBuffer(elems.Backend);
        }

		public void UnsetElementBuffer()
		{
			impl.UnsetElementBuffer();
			_elements = null;
		}

        public void Dispose()
        {
            TotalBuffers--;
            impl.Dispose();
        }
    }
}
