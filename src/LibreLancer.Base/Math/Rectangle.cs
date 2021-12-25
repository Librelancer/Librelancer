// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;

namespace LibreLancer
{
    [StructLayout(LayoutKind.Sequential)]
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

		public bool Intersects(Rectangle other)
		{
			return (other.X < (X + Width) &&
					X < (other.X + other.Width) &&
					other.Y < (Y + Height) &&
					Y < (other.Y + other.Height));
		}

		public override bool Equals(object obj)
		{
			if (obj is Rectangle)
			{
				return ((Rectangle)obj) == this;
			}
			return false;
		}
		public static bool operator ==(Rectangle a, Rectangle b)
		{
			return a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
		}
		public static bool operator !=(Rectangle a, Rectangle b)
		{
			return a.X != b.X || a.Y != b.Y || a.Width != b.Width || a.Height != b.Height;
		}

        public override int GetHashCode()
        {
            unchecked
            {
                int hc = X;
                hc = hc * 314159 + Y;
                hc = hc * 314159 + Width;
                hc = hc * 314159 + Height;
                return hc;
            }
        }

        public float AspectRatio => (float) Width / (float) Height;
    }
}

