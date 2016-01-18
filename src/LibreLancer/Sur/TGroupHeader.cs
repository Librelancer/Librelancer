using System;
using System.IO;
namespace LibreLancer.Sur
{
	public struct TGroupHeader
	{
		public const int SIZE = 16;
		public uint MeshID;
		public uint RefVertsCount;
		public short TriangleCount;
		public uint Type;
		public uint VertexArrayOffset;

		public TGroupHeader (BinaryReader reader)
		{
			VertexArrayOffset = reader.ReadUInt32 ();
			MeshID = reader.ReadUInt32 ();
			Type = reader.ReadByte ();
			RefVertsCount = reader.ReadUInt24 ();
			TriangleCount = reader.ReadInt16 ();
			//FL-OS Comment: padding
			reader.BaseStream.Seek (2, SeekOrigin.Current);
		}
	}
}

