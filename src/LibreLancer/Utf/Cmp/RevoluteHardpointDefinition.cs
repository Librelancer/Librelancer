// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Utf.Cmp
{
    public class RevoluteHardpointDefinition : HardpointDefinition
    {
        public Vector3 Axis;
        public float Max;
        public float Min;

        public RevoluteHardpointDefinition(IntermediateNode root)
            : base(root)
        {
            foreach (LeafNode node in root)
            {
                if (!parentNode(node))
                    switch (node.Name.ToLowerInvariant())
                    {
                        case "axis":
                            Axis = node.Vector3Data.Value;
                            break;
						case "max":
							Max = node.SingleArrayData [0];
                            break;
						case "min":
							Min = node.SingleArrayData [0];
                            break;
                        default:
                            throw new Exception("Invalid LeafNode in " + root.Name + ": " + node.Name);
                    }
            }
        }
        public RevoluteHardpointDefinition(string name) : base(name) {
            Axis = Vector3.UnitY;
        }
    }
}
