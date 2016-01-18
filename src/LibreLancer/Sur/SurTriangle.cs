using System;
using System.IO;
namespace LibreLancer.Sur
{
	//TODO: SurTriangle - What does this stuff do?
	public class SurTriangle
	{
		public const int SIZE = 16;
		public Side[] Vertices = new Side[3];
		public uint TriNumber;
		public uint Flag;
		public uint TriOp;
		public uint Unknown; //FL-OS Comment: tested for zero (which they all are), but not used

		public SurTriangle (BinaryReader reader)
		{
			uint arg = reader.ReadUInt32 ();
			TriNumber = (arg >> 0) & 0xFFF;
			TriOp = (arg >> 12) & 0xFFF;
			Unknown = (arg >> 24) & 0x7F;
			Flag = arg >> 31;

			Vertices [0] = new Side (reader);
			Vertices [1] = new Side (reader);
			Vertices [2] = new Side (reader);
		}
	}
}

