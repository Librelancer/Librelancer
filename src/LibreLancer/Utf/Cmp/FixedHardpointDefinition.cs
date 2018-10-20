// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;


namespace LibreLancer.Utf.Cmp
{
    public class FixedHardpointDefinition : HardpointDefinition
    {
        public FixedHardpointDefinition(IntermediateNode root)
            : base(root)
        {
            foreach (LeafNode node in root)
            {
                if (!parentNode(node))
                    throw new Exception("Invalid LeafNode in " + root.Name + ": " + node.Name);
            }
        }
        public FixedHardpointDefinition(string name) : base(name) { }
    }
}