// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
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
			Hardpoints = new List<HardpointDefinition>();

			foreach (Node subNode in node)
			{
				switch (subNode.Name.ToLowerInvariant())
				{
				case "bone to root":
                        BoneToRoot = (subNode as LeafNode).MatrixData4x3.Value;
					break;
				case "lod bits":
                    LodBits = (subNode as LeafNode).DataSegment.AtIndex(0);
					break;
				case "hardpoints":
					IntermediateNode hardpointsNode = subNode as IntermediateNode;
					foreach (IntermediateNode hardpointTypeNode in hardpointsNode)
					{
						switch (hardpointTypeNode.Name.ToLowerInvariant())
						{
						case "fixed":
							foreach (IntermediateNode fixedNode in hardpointTypeNode)
								Hardpoints.Add(new FixedHardpointDefinition(fixedNode));
							break;
						case "revolute":
							foreach (IntermediateNode revoluteNode in hardpointTypeNode)
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
