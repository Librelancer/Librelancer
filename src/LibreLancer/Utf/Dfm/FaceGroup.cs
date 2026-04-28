// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Render;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Utf.Dfm
{
	public class FaceGroup
	{
        public string? MaterialName { get; private set; }

        public int StartIndex;
		public ushort[]? TriangleStripIndices { get; private set; }
        public ushort[]? FaceIndices { get; private set; }


        public FaceGroup(IntermediateNode root)
		{
			foreach (var node in root.Children.OfType<LeafNode>())
			{
				switch (node.Name.ToLowerInvariant())
				{
				case "material_name": MaterialName = node.StringData;
					break;
				case "tristrip_indices": TriangleStripIndices = node.UInt16ArrayData;
					break;
                case "face_indices": FaceIndices = node.UInt16ArrayData;
                    break;
				}
			}
		}
    }
}
