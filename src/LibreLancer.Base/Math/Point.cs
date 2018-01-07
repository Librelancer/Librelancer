/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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

