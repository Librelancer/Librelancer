// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Utf.Anm
{
	public struct JointMap
    {
		public string ParentName;
		public string ChildName;
		public Channel Channel;
		public JointMap(IntermediateNode root, AnmBuffer buffer, StringDeduplication dedup = null)
        {
			foreach (Node node in root)
            {
                if (node.Name.Equals("parent name", StringComparison.OrdinalIgnoreCase))
                    ParentName = dedup == null
                        ? (node as LeafNode).StringData
                        : dedup.Get((node as LeafNode).StringData);
                else if (node.Name.Equals("child name", StringComparison.OrdinalIgnoreCase))
                    ChildName = dedup == null
                        ? (node as LeafNode).StringData
                        : dedup.Get((node as LeafNode).StringData);
                else if (node.Name.Equals("channel", StringComparison.OrdinalIgnoreCase))
                    Channel = new Channel(node as IntermediateNode, buffer);
            }
		}
	}
}
