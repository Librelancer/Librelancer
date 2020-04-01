// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;
namespace LibreLancer.Utf.Vms
{
    /// <summary>
    /// Repeated no_meshes times in segment - 12 bytes
    /// </summary>
    public class TMeshHeader
    {
        private ILibFile materialLibrary;
        //private static NullMaterial nullMaterial;

        /// <summary>
        /// CRC of texture name for mesh
        /// </summary>
        private uint MaterialId;
        private Material material;
		Material defaultMaterial;
        public Material Material
        {
            get
            {
				if (material != null && !material.Loaded) material = null;
                if (material == null) material = materialLibrary.FindMaterial(MaterialId);
                return material;
            }
        }

		public uint MaterialCrc
		{
			get
			{
				return MaterialId;
			}
		}

        public ushort StartVertex { get; private set; }
        public ushort EndVertex { get; private set; }
        public ushort NumRefVertices { get; private set; }
        public ushort Padding { get; private set; } //0x00CC

        public int TriangleStart { get; private set; }

        private int numVertices;
        private int primitiveCount;

        public TMeshHeader(BinaryReader reader, int triangleStartOffset, ILibFile materialLibrary)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (materialLibrary == null) throw new ArgumentNullException("materialLibrary");

            this.materialLibrary = materialLibrary;

            MaterialId = reader.ReadUInt32();
            StartVertex = reader.ReadUInt16();
            EndVertex = reader.ReadUInt16();
            NumRefVertices = reader.ReadUInt16();
            Padding = reader.ReadUInt16();

            TriangleStart = triangleStartOffset;

            numVertices = EndVertex - StartVertex + 1;
            primitiveCount = NumRefVertices / 3;
        }
    }
}
