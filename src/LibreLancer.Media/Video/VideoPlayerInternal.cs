// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Media
{
	abstract class VideoPlayerInternal : IDisposable
	{
		public bool Playing = false;
		public abstract bool Init();
		public abstract void PlayFile(string filename);
		public abstract void Draw(RenderState rstate);
		public abstract void Dispose();
		public abstract Texture2D GetTexture();
	}
}

