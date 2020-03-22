// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
	public class Cursor
	{
		public string Nickname;
		public string Texture;
		public Rectangle Dimensions;
		public float Spin;
		public float Scale;
		public Color4 Color = Color4.White;
		public Vector2 Hotspot = Vector2.Zero;
		public ResourceManager Resources;

		public void Draw(Renderer2D renderer, Mouse m)
		{
            var pos = new Vector2(m.X, m.Y) - (Hotspot * Scale);
			var dst = new Rectangle(
				(int)pos.X, (int)pos.Y,
				(int)(Dimensions.Width * Scale), (int)(Dimensions.Height * Scale)
			);
			renderer.Draw(
				(Texture2D)Resources.FindTexture(Texture),
				Dimensions,
				dst,
				Color,
				BlendMode.Additive
			);
		}
	}
}
