using System;

namespace LibreLancer
{
    public struct Rectangle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
		public Rectangle(int x, int y, int w, int h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
		}
		public bool Contains(int x, int y)
		{
			return (
			    x >= X &&
			    x <= (X + Width) &&
			    y >= Y &&
			    y <= (Y + Height)
			);
		}
		public bool Contains(Point pt)
		{
			return Contains (pt.X, pt.Y);
		}
    }
}

