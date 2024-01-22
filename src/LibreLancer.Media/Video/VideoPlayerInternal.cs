// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics;

namespace LibreLancer.Media
{
	abstract class VideoPlayerInternal : IDisposable
	{
		public bool Playing = false;
        public abstract bool Init(RenderContext context);
		public abstract void PlayFile(string filename);
		public abstract void Draw();
		public abstract void Dispose();
		public abstract Texture2D GetTexture();
	}
}

