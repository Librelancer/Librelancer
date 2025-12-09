// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public struct RectangleF
	{
		public float X;
		public float Y;
		public float Width;
		public float Height;

		public RectangleF(float x, float y, float w, float h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
		}

        public bool Contains(float x, float y)
        {
            return (
                x >= X &&
                x <= (X + Width) &&
                y >= Y &&
                y <= (Y + Height)
            );
        }

        public bool Intersects(RectangleF other)
        {
            return (other.X < (X + Width) &&
                    X < (other.X + other.Width) &&
                    other.Y < (Y + Height) &&
                    Y < (other.Y + other.Height));
        }

        public static explicit operator Rectangle(RectangleF src) => new Rectangle((int)src.X, (int)src.Y, (int)src.Width, (int)src.Height);
    }
}

