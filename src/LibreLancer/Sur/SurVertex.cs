using System;
using System.IO;
using Jitter.LinearMath;
namespace LibreLancer.Sur
{
	public struct SurVertex
	{
		public JVector Point;
		public uint Mesh;

		public SurVertex (BinaryReader reader)
		{
			Point = new JVector (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ());
			Mesh = reader.ReadUInt32 ();
		}
	}
}

