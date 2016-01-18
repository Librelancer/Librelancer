using System;
using System.IO;

namespace LibreLancer.Sur
{
	//TODO: Sur - ???
	public struct Side
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

