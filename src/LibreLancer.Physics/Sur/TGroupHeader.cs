// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;

namespace LibreLancer.Physics.Sur
{
	class TGroupHeader
	{
		public const int SIZE = 16;
		public long HeaderOffset;
		public uint MeshID;
		public uint RefVertsCount;
		public short TriangleCount;
		public uint Type;
		public uint VertexArrayOffset;
		public List<SurTriangle> Triangles;
		public TGroupHeader (BinaryReader reader)
		{
			HeaderOffset = reader.BaseStream.Position;
			VertexArrayOffset = reader.ReadUInt32 ();
			MeshID = reader.ReadUInt32 ();
			Type = reader.ReadByte ();
			RefVertsCount = reader.ReadUInt24 ();
			TriangleCount = reader.ReadInt16 ();
			//FL-OS Comment: padding
			reader.BaseStream.Seek (2, SeekOrigin.Current);
			Triangles = new List<SurTriangle>();
		}
	}
}

