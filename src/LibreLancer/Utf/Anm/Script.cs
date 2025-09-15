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
        public bool HasRootHeight { get; set; }
        public float RootHeight { get; set; }
		public RefList<ObjectMap> ObjectMaps { get; private set; }
		public RefList<JointMap> JointMaps { get; private set; }

        public Script(string name)
        {
            Name = name;
            ObjectMaps = new();
            JointMaps = new();
        }

        public Script(IntermediateNode root, AnmBuffer buffer, StringDeduplication strings)
        {
            Name = root.Name;
			var om = new RefList<ObjectMap>();
			var jm = new RefList<JointMap>();
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

            om.Shrink();
            jm.Shrink();

            ObjectMaps = om;
            JointMaps = jm;
        }
    }
}
