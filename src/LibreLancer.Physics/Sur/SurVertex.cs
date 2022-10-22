// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.IO;

namespace LibreLancer.Physics.Sur
{
	public struct SurVertex
	{
		public const int SIZE = sizeof(float) * 3 + sizeof(uint);
		public Vector3 Point;
		public uint Mesh;

		public SurVertex (BinaryReader reader)
		{
			Point = new Vector3 (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ());
			Mesh = reader.ReadUInt32 ();
		}
	}
}

