using System;
using OpenTK;
namespace LibreLancer.GameData
{
	public class ZoneEllipsoid : ZoneShape
	{
		public Vector3 Size;
		public ZoneEllipsoid (float x, float y, float z)
		{
			Size = new Vector3 (x, y, z);
		}
	}
}

