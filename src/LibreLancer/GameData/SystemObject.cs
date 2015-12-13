using System;
using OpenTK;
namespace LibreLancer.GameData
{
	public class SystemObject
	{
		public Archetype Archetype;
		public Vector3 Position = Vector3.Zero;
		public Matrix4? Rotation;

		public SystemObject ()
		{
		}
	}
}

