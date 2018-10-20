// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;

namespace LibreLancer.Physics.Sur
{
	//TODO: Sur - ???
	struct Side
	{
		public bool Flag;
		public ushort Offset;
		public ushort Vertex;
		public Side(BinaryReader reader)
		{
			Vertex = reader.ReadUInt16();
			var arg = reader.ReadUInt16();
			Offset = (ushort)(arg & 0x7FFF);
			Flag = ((arg >> 15) & 1) == 1;
		}
	}
}
	