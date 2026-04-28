// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Utf.Anm
{
    public struct ObjectMap
    {
        public string ParentName = null!;
        public string ChildName = null!;
        public Channel Channel;

        public ObjectMap(IntermediateNode root, AnmBuffer buffer, StringDeduplication? dedup = null)
        {
            foreach (Node node in root.Children)
            {
                var leaf = (node as LeafNode)!;
                var intermediate = (node as IntermediateNode)!;
                if (node.Name.Equals("parent name", StringComparison.OrdinalIgnoreCase))
                    ParentName = dedup == null
                        ? leaf.StringData
                        : dedup.Get(leaf.StringData);
                else if (node.Name.Equals("child name", StringComparison.OrdinalIgnoreCase))
                    ChildName = dedup == null
                        ? leaf.StringData
                        : dedup.Get(leaf.StringData);
                else if (node.Name.Equals("channel", StringComparison.OrdinalIgnoreCase))
                    Channel = new Channel(intermediate, buffer);
            }
        }
    }
}
