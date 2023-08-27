// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.IO;

namespace LibreLancer.Utf.Vms
{
    /// <summary>
    /// Repeated no_meshes times in segment - 12 bytes
    /// </summary>
    public class TMeshHeader
    {
		public uint MaterialCrc { get; private set; }

        public ushort StartVertex { get; private set; }
        public ushort EndVertex { get; private set; }
        public ushort NumRefVertices { get; private set; }
        public ushort Padding { get; private set; } //0x00CC

        public int TriangleStart { get; private set; }

        public TMeshHeader(BinaryReader reader, int triangleStartOffset)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            
            MaterialCrc = reader.ReadUInt32();
            StartVertex = reader.ReadUInt16();
            EndVertex = reader.ReadUInt16();
            NumRefVertices = reader.ReadUInt16();
            Padding = reader.ReadUInt16();

            TriangleStart = triangleStartOffset;
        }
    }
}
