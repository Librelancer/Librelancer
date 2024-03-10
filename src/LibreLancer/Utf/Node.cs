// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;

namespace LibreLancer.Utf
{
    public abstract class Node
    {
        public int PeerOffset;
        public string Name;

        protected Node(int peerOffset, string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            this.PeerOffset = peerOffset;
            this.Name = name;
        }
        protected Node(string name)
        {
            this.Name = name;
        }

        public static Node FromStreamV2(BinaryReader reader, StringBlock stringBlock, byte[] dataBlock)
        {
            var nameOffset = (int)reader.ReadVarUInt64();
            var name = stringBlock.GetString(nameOffset);
            var type = reader.ReadByte();
            if (type == 0)
                return LeafNode.LeafV2(name, reader, dataBlock);
            else
                return IntermediateNode.IntermediateV2(name, reader, stringBlock, dataBlock);
        }
        public static Node FromStream(BinaryReader reader, int offset, StringBlock stringBlock, byte[] dataBlock)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (stringBlock == null) throw new ArgumentNullException("stringBlock");
            if (dataBlock == null) throw new ArgumentNullException("dataBlock");

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);

            int peerOffset = reader.ReadInt32();
            int nameOffset = reader.ReadInt32();
            string name = stringBlock.GetString(nameOffset);

            NodeFlags flags = (NodeFlags)reader.ReadInt32();
            if ((flags & NodeFlags.Intermediate) == NodeFlags.Intermediate)
                return new IntermediateNode(peerOffset, name, reader, stringBlock, dataBlock);
            else if ((flags & NodeFlags.Leaf) == NodeFlags.Leaf)
                return new LeafNode(peerOffset, name, reader, dataBlock);
            else
            {
                //throw new FileContentException(UtfFile.FILE_TYPE, "Neither required flag set. Flags: " + flags);
                return new LeafNode(peerOffset, name, reader, dataBlock);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

