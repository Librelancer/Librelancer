// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
namespace LibreLancer.Sur
{
	public class SurfaceFace
    {
        public int Index;
        public int Flag;
        public int Opposite;
        private int Unknown;
        public Point3 Points;
        public Point3 Shared;
        public Point3 Flags;

        static (int p, int f, int s) ReadSide(BinaryReader reader, ref int longCount)
        {
            var point = reader.ReadUInt16();
            var x = reader.ReadUInt16();
            var flag = (x >> 15);
            var edgeOffset = longCount + ((x & 0x4000) != 0 ? (x & 0x3FFF) | ~0x3FFF : x & 0x3FFF);
            var shared = edgeOffset - edgeOffset / 4;
            longCount++;
            return (point, flag, shared);
        }

        public static SurfaceFace Read(BinaryReader reader, ref int longCount)
        {
            var f = new SurfaceFace();
            
			uint arg = reader.ReadUInt32 ();
            f.Index = (int) ((arg >> 0) & 0xFFF);
            f.Opposite = (int) ((arg >> 12) & 0xFFF);
            f.Unknown = (int) ((arg >> 24) & 0x7F);
            f.Flag = (int) (arg >> 31);

            (f.Points.A, f.Flags.A, f.Shared.A) = ReadSide(reader, ref longCount);
            (f.Points.B, f.Flags.B, f.Shared.B) = ReadSide(reader, ref longCount);
            (f.Points.C, f.Flags.C, f.Shared.C) = ReadSide(reader, ref longCount);
            return f;
        }

        void WriteSide(int p, int f, int s, ref int edgeCount, BinaryWriter writer)
        {
            var shared = s - edgeCount + s / 3 - edgeCount / 3;
            shared &= 0x7FFF;
            if (f != 0) shared |= 0x8000;
            writer.Write((ushort)p);
            writer.Write((ushort)shared);
            edgeCount++;
        }

        public void Write(BinaryWriter writer, ref int edgeCount)
        {
            var arg = (uint) (Index & 0xFFF |
                              (Opposite & 0xFFF) << 12 |
                              (Unknown & 0x7F) << 24 |
                              Flag << 31);
            writer.Write(arg);
            WriteSide(Points.A, Flags.A, Shared.A, ref edgeCount, writer);
            WriteSide(Points.B, Flags.B, Shared.B, ref edgeCount, writer);
            WriteSide(Points.C, Flags.C, Shared.C, ref edgeCount, writer);
        }
	}
}

