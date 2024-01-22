// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics
{
	public class RenderTarget2D : RenderTarget
	{
		public DepthBuffer DepthBuffer { get; private set; }
		public Texture2D Texture { get; private set; }
        public int Width => Texture.Width;
        public int Height => Texture.Height;

        internal IRenderTarget2D Backing;


		public RenderTarget2D (RenderContext context, int width, int height)
        {
            Texture = new Texture2D(context, width, height);
            DepthBuffer = new DepthBuffer(context, width, height);
            Backing = context.Backend.CreateRenderTarget2D(Texture.Backing, DepthBuffer.Backing);
            Target = Backing;
		}

        public void BlitToScreen() => Backing.BlitToScreen();

		public override void Dispose ()
		{
			Dispose(false);
        }

        public void Dispose(bool keepTexture)
        {
            Backing.Dispose();
            DepthBuffer.Dispose();
            if(!keepTexture)
                Texture.Dispose();
        }
	}
}

