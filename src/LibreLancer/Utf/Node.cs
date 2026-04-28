// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibreLancer.Utf
{
    public abstract class Node
    {
        public string Name;

        protected Node(string name)
        {
            this.Name = name;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NodeStruct
        {
            public int PeerOffset;
            public int NameOffset;
            public NodeFlags Flags;
            public int Padding;
            public int ChildOffset;
            public int AllocatedSize;
            public int Size;
        }

        public static Node FromBuffer(byte[] nodeBlock, int offset, StringBlock stringBlock, byte[] dataBlock)
        {
            ref var d = ref Unsafe.As<byte, NodeStruct>(ref nodeBlock[offset]);

            string name = stringBlock.GetString(d.NameOffset);

            if ((d.Flags & NodeFlags.Intermediate) == NodeFlags.Intermediate)
                return new IntermediateNode(ref d, name, nodeBlock, stringBlock, dataBlock);
            else if ((d.Flags & NodeFlags.Leaf) == NodeFlags.Leaf)
                return new LeafNode(ref d, name, dataBlock);
            else
                return new LeafNode(ref d, name, dataBlock);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

