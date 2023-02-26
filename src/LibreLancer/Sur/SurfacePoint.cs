// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.IO;

namespace LibreLancer.Sur
{
	public struct SurfacePoint
	{
		public Vector3 Point;
		public uint Mesh;

        public static SurfacePoint Read(BinaryReader reader)
        {
            return new SurfacePoint()
            {
                Point = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Mesh = reader.ReadUInt32()
            };
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Point.X);
            writer.Write(Point.Y);
            writer.Write(Point.Z);
            writer.Write(Mesh);
        }
	}
}

