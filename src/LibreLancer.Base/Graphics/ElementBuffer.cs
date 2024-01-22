// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics
{
    public unsafe class ElementBuffer : IDisposable
    {
        internal IElementBuffer Backend;
        public int IndexCount => Backend.IndexCount;
		public ElementBuffer(RenderContext context, int count, bool isDynamic = false)
        {
            Backend = context.Backend.CreateElementBuffer(count, isDynamic);
		}
        public void SetData(short[] data) => Backend.SetData(data);
        public void SetData(ushort[] data) => Backend.SetData(data);
        public void SetData(ushort[] data, int count, int start = 0) => Backend.SetData(data, count, start);

        public void Expand(int newSize) => Backend.Expand(newSize);
        public void Dispose() => Backend.Dispose();
    }
}
