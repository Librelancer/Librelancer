// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics
{
	public class MultisampleTarget : RenderTarget
	{
        public int Width => impl.Width;
        public int Height => impl.Height;

        private IMultisampleTarget impl;

		public MultisampleTarget(RenderContext renderContext, int width, int height, int samples)
        {
            impl = renderContext.Backend.CreateMultisampleTarget(width, height, samples);
            Target = impl;
        }

        public void BlitToScreen() => impl.BlitToScreen();

        public void BlitToRenderTarget(RenderTarget2D rTarget) =>
            impl.BlitToRenderTarget(rTarget.Backing);

        public override void Dispose() => impl.Dispose();
    }
}
