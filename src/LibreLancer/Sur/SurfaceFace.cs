// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
namespace LibreLancer.Sur
{
	public struct SurfaceFace
    {
        private uint Data;

        public int Index
        {
            get => (int) ((Data >> 0) & 0xFFF);
            set => Data = (uint) ((Data & ~0xFFF) | (uint) (value & 0xFFF));
        }

        public bool Flag
        {
            get => (Data & 0x80000000) != 0;
            set {
                if (value)
                    Data |= 0x80000000;
                else
                    Data &= ~0x80000000;
            }
        }

        public int Opposite
        {
            get => (int) ((Data >> 12) & 0xFFF);
            set => Data = (uint) ((Data & ~0xFFF000) | (((uint) value & 0xFFF) << 12));
        }
        
        public Point3<int> Shared;
        public Point3<ushort> Points;
        public Point3<bool> Flags;

        static (ushort p, bool f, int s) ReadSide(BinaryReader reader, ref int longCount)
        {
            var point = reader.ReadUInt16();
            var x = reader.ReadUInt16();
            var flag = (x >> 15);
            var edgeOffset = longCount + ((x & 0x4000) != 0 ? (x & 0x3FFF) | ~0x3FFF : x & 0x3FFF);
            var shared = edgeOffset - edgeOffset / 4;
            longCount++;
            return (point, flag != 0, shared);
        }

        public static SurfaceFace Read(BinaryReader reader, ref int longCount)
        {
            var f = new SurfaceFace();

            f.Data = reader.ReadUInt32();

            (f.Points.A, f.Flags.A, f.Shared.A) = ReadSide(reader, ref longCount);
            (f.Points.B, f.Flags.B, f.Shared.B) = ReadSide(reader, ref longCount);
            (f.Points.C, f.Flags.C, f.Shared.C) = ReadSide(reader, ref longCount);
            return f;
        }

        void WriteSide(int p, bool f, int s, ref int edgeCount, BinaryWriter writer)
        {
            var shared = s - edgeCount + s / 3 - edgeCount / 3;
            shared &= 0x7FFF;
            if (f) shared |= 0x8000;
            writer.Write((ushort)p);
            writer.Write((ushort)shared);
            edgeCount++;
        }

        public void Write(BinaryWriter writer, ref int edgeCount)
        {
            writer.Write(Data);
            WriteSide(Points.A, Flags.A, Shared.A, ref edgeCount, writer);
            WriteSide(Points.B, Flags.B, Shared.B, ref edgeCount, writer);
            WriteSide(Points.C, Flags.C, Shared.C, ref edgeCount, writer);
        }
	}
}

