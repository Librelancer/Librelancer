// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics;

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

		public void Draw(Renderer2D renderer, Mouse m, double globalTime)
		{
            //var pos = new Vector2(m.X, m.Y) - (Hotspot * Scale);
			var dst = new Rectangle(
				(int)m.X, (int)m.Y,
				(int)(Dimensions.Width * Scale), (int)(Dimensions.Height * Scale)
			);
            var angle = MathHelper.WrapF((float)globalTime * Spin, -MathF.PI, MathF.PI);
            var hp = new Vector2((int) (Hotspot.X * Scale), (int) (Hotspot.Y * Scale));
            renderer.DrawRotated(
				(Texture2D)Resources.FindTexture(Texture),
				Dimensions,
				dst,
                hp,
				Color,
				BlendMode.Additive,
                angle
			);
		}
	}
}
