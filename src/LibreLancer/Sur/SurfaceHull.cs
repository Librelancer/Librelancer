// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using Castle.DynamicProxy.Contributors;

namespace LibreLancer.Sur
{
	public class SurfaceHull
    {
        public uint HullId;
        public byte Type;
        public ushort Unknown;
        public List<SurfaceFace> Faces = new List<SurfaceFace>();

        public static SurfaceHull Read(BinaryReader reader)
        {
            var h = new SurfaceHull(); 
            h.HullId = reader.ReadUInt32 ();
            //24-bit unique refs count (we don't use)
            //+ type
            h.Type = (byte)(reader.ReadUInt32() & 0xFF);
            var faceCount = reader.ReadInt16 ();
            h.Unknown = reader.ReadUInt16();
            int longCount = 0;
            for (int i = 0; i < faceCount; i++) {
                h.Faces.Add(SurfaceFace.Read(reader, ref longCount));
                longCount++;
            }
            return h;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(HullId);
            var refVerts = ((12 + Faces.Count * 6) / 4);
            writer.Write(refVerts << 8 | Type);
            writer.Write((ushort)Faces.Count);
            writer.Write(Unknown);
            int edgeCount = 0;
            foreach (var f in Faces) {
                f.Write(writer, ref edgeCount);
            }
        }

        public override string ToString() => Type == 5 ? $"Wrap Hull" : $"Hull ({Type} ID: 0x{HullId:X})";
    }
}

