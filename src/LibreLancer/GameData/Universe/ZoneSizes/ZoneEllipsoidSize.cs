using System;

namespace LibreLancer.GameData.Universe
{
	public class ZoneEllipsoidSize : ZoneSize
	{
		public float Width;
		public float Height;
		public float Length;

		public ZoneEllipsoidSize (float w, float h, float l)
		{
			Width = w;
			Height = h;
			Length = l;
		}
	}
}

