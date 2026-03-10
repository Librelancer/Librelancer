// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Utf.Dfm
{
	public class Bone
	{
		public Matrix4x4 BoneToRoot { get; private set; }
		public byte LodBits { get; private set; }
		public List<HardpointDefinition> Hardpoints { get; private set; }

        public string Name;

        public Vector3 Min;
        public Vector3 Max;

		public Bone(string name, IntermediateNode node)
        {
            Name = name;
			Hardpoints = [];

			foreach (Node subNode in node)
			{
				switch (subNode.Name.ToLowerInvariant())
				{
				case "bone to root":
                        BoneToRoot = ((subNode as LeafNode)!).MatrixData4x3!.Value;
					break;
				case "lod bits":
                    LodBits = ((subNode as LeafNode)!).DataSegment.AtIndex(0);
					break;
				case "hardpoints":
					IntermediateNode hardpointsNode = (subNode as IntermediateNode)!;
					foreach (var hardpointTypeNode in hardpointsNode.OfType<IntermediateNode>())
					{
						switch (hardpointTypeNode.Name.ToLowerInvariant())
						{
						case "fixed":
							foreach (var fixedNode in hardpointTypeNode.OfType<IntermediateNode>())
								Hardpoints.Add(new FixedHardpointDefinition(fixedNode));
							break;
						case "revolute":
							foreach (var revoluteNode in hardpointTypeNode.OfType<IntermediateNode>())
								Hardpoints.Add(new RevoluteHardpointDefinition(revoluteNode));
							break;
						default: throw new Exception("Invalid node in " + hardpointsNode.Name + ": " + hardpointTypeNode.Name);
						}
					}
					break;
				default: throw new Exception("Invalid node in " + node.Name + ": " + subNode.Name);
				}
			}
		}
    }
}
