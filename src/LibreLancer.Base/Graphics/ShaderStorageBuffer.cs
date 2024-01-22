// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics
{
	public class ShaderStorageBuffer : IDisposable
    {
        private IShaderStorageBuffer impl;
        public int Size => impl.Size;
		public ShaderStorageBuffer(RenderContext context, int size)
        {
            impl = context.Backend.CreateShaderStorageBuffer(size);
        }

        public SSBOHandle Map(bool read = false) => impl.Map(read);

        public void BindIndex(uint index) => impl.BindIndex(index);
        public void Dispose() => impl.Dispose();
    }
	public struct SSBOHandle : IDisposable
	{
		IShaderStorageBuffer parent;
		public IntPtr Handle;
		internal SSBOHandle(IShaderStorageBuffer p, IntPtr h)
		{
			parent = p;
			Handle = h;
		}
		public void Dispose()
		{
			parent.Unmap();
		}
	}
}
