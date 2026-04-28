// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace LibreLancer.Utf
{
    public class IntermediateNode : Node
    {
        public readonly List<Node> Children = [];

        public IntermediateNode(string name, List<Node> children) : base(name)
        {
            this.Children = children;
        }


        public IntermediateNode(int peerOffset, string name, BinaryReader reader, StringBlock stringBlock,
            byte[] dataBlock)
            : base(peerOffset, name)
        {
            // int zero = reader.ReadInt32();
            reader.BaseStream.Seek(sizeof(int), SeekOrigin.Current);

            Children = [];

            var childOffset = reader.ReadInt32();

            if (childOffset <= 0)
            {
                return;
            }

            var next = childOffset;

            do
            {
                if (Children.Count > 500000) throw new Exception("Node overflow. Broken UTF?");
                Node n = Node.FromStream(reader, next, stringBlock, dataBlock);
                Children.Add(n);
                next = n.PeerOffset;
            } while (next > 0);
            // else
            // throw new FileContentsException(UtfFile.FILE_TYPE, "IntermediateNode " + Name + " doesn't have any child nodes.");

            // int allocatedSize = reader.ReadInt32();
            // int size = reader.ReadInt32();
            // int size2 = reader.ReadInt32();
            // int timestamp1 = reader.ReadInt32();
            // int timestamp2 = reader.ReadInt32();
            // int timestamp3 = reader.ReadInt32();
        }


        public override string ToString()
        {
            var result = "{Inter: " + base.ToString() + "{";
            result = Children.Aggregate(result, (current, n) => current + (n + ", "));
            return result + "}";
        }
    }
}
