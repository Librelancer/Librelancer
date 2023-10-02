// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;

namespace LibreLancer.Utf.Anm
{
    public class Script
    {
        public string Name { get; private set; }
        public bool HasRootHeight { get; private set; }
        public float RootHeight { get; private set; }
		public ObjectMap[] ObjectMaps { get; private set; }
		public JointMap[] JointMaps { get; private set; }

        public Script(IntermediateNode root, AnmBuffer buffer, StringDeduplication strings)
        {
            Name = root.Name;
			var om = new List<ObjectMap>();
			var jm = new List<JointMap>();
            foreach (Node node in root)
            {
                if (node.Name.Equals("root height", StringComparison.OrdinalIgnoreCase))
                {
                    HasRootHeight = true;
                    RootHeight = (node as LeafNode).SingleData.Value;
                }
				else if (node.Name.StartsWith("object map", StringComparison.OrdinalIgnoreCase))
					om.Add(new ObjectMap(node as IntermediateNode, buffer, strings));
				else if (node.Name.StartsWith("joint map", StringComparison.OrdinalIgnoreCase))
                    jm.Add(new JointMap(node as IntermediateNode, buffer, strings));
                else
                {
                    FLLog.Warning("Anm", $"{root.Name}: invalid node {node.Name}, possible broken animation?");
                }
            }
            ObjectMaps = om.ToArray();
            JointMaps = jm.ToArray();
        }
    }
}
