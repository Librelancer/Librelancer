// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public struct Point
	{
		public static readonly Point Zero = new Point(0, 0);

		public int X;
		public int Y;
		public Point(int x, int y)
		{
			X = x;
			Y = y;
		}

		public static bool operator ==(Point a, Point b)
		{
			return a.X == b.X && a.Y == b.Y;
		}
		public static bool operator !=(Point a, Point b)
		{
			return a.X != b.X || a.Y != b.Y;
		}
		public override int GetHashCode()
		{
			unchecked
			{
				return (X * 31) + Y;
			}
		}
		public override bool Equals(object obj)
		{
			if (obj is Point)
				return (Point)obj == this;
			return false;
		}
	}
}

