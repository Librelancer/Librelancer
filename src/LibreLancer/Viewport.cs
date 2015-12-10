using System;

namespace LibreLancer
{
	public struct Viewport
	{
		public int X;
		public int Y;
		public int Width;
		public int Height;
		public Viewport(int x, int y, int width, int height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}
		public float AspectRatio
		{
			get {
				return (float)Width / (float)Height;
			}
		}
	}
}

