// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreLancer.Utf
{
    public abstract class UtfFile
    {
        public const string FILE_TYPE = "UTF ";
        const int TAG_LEN = 4;
        public const int FILE_VERSION = 257;

        protected static IntermediateNode ParseV2(string path, BinaryReader reader)
        {
            byte ver = reader.ReadByte();
            if (ver != 1)
                throw new FileVersionException(path, "XUTF", ver, 1);
            uint stringBlockLength = reader.ReadUInt32();
            uint nodeBlockLength = reader.ReadUInt32();
            uint dataBlockLength = reader.ReadUInt32();
            var stringBlock = reader.ReadBytes((int)stringBlockLength);
            //Node block
            var nodeBlock = reader.ReadBytes((int)nodeBlockLength);
            var dataBlock = reader.ReadBytes((int)dataBlockLength);
            using (BinaryReader nodeReader = new BinaryReader(new MemoryStream(nodeBlock)))
            {
                var root =
                    Node.FromStreamV2(nodeReader, new StringBlock(stringBlock, true), dataBlock) as IntermediateNode;
                if (root == null)
                    throw new FileContentException(UtfFile.FILE_TYPE, "The root node doesn't have any child nodes.");
                return root;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct UtfHeader
        {
            public int FormatVersion;
            public int NodeBlockOffset;
            public int NodeBlockSize;
            public int Pad;
            public int EntrySize;
            public int StringBlockOffset;
            public int StringBlockAllocated;
            public int StringBlockSize;
            public int DataBlockOffset;
        }

        protected static IntermediateNode parseFile(string path, Stream stream)
        {
            byte[] nodeBlock;
            byte[] stringBlock;
            byte[] dataBlock;

            using var reader = new BinaryReader(stream);

            Span<byte> buffer = stackalloc byte[TAG_LEN];
            reader.Read(buffer);
            string fileType = Encoding.ASCII.GetString(buffer);
            if (buffer.SequenceEqual("XUTF"u8))
            {
                return ParseV2(path, reader);
            }

            if (!buffer.SequenceEqual("UTF "u8))
                throw new FileFormatException(path, fileType, FILE_TYPE);

            long fileLength = reader.BaseStream.Length; //This is a syscall, cache.

            var header = reader.ReadStruct<UtfHeader>();

            if (header.FormatVersion != FILE_VERSION)
                throw new FileVersionException(path, fileType, header.FormatVersion, FILE_VERSION);

            if (header.NodeBlockOffset + header.NodeBlockSize > fileLength)
                throw new FileContentException(fileType,
                    $"The node block was out of range ({header.NodeBlockOffset}, {header.NodeBlockSize})");

            if (header.StringBlockOffset + header.StringBlockSize > fileLength)
                throw new FileContentException(fileType,
                    $"The string block was out of range ({header.StringBlockOffset}, {header.StringBlockSize})");

            if (header.DataBlockOffset > fileLength)
                throw new FileContentException(fileType,
                    "The data block offset was out of range: " + header.DataBlockOffset);

            nodeBlock = new byte[header.NodeBlockSize];
            reader.BaseStream.Seek(header.NodeBlockOffset, SeekOrigin.Begin);
            reader.Read(nodeBlock);

            stringBlock = new byte[header.StringBlockSize];
            reader.BaseStream.Seek(header.StringBlockOffset, SeekOrigin.Begin);
            reader.Read(stringBlock);


            dataBlock = new byte[(int)(fileLength - header.DataBlockOffset)];
            reader.BaseStream.Seek(header.DataBlockOffset, SeekOrigin.Begin);
            reader.Read(dataBlock);

            IntermediateNode root;

            using (BinaryReader nodeReader = new BinaryReader(new MemoryStream(nodeBlock)))
            {
                root = Node.FromStream(nodeReader, 0, new StringBlock(stringBlock, false), dataBlock) as IntermediateNode;
                if (root == null)
                    throw new FileContentException(UtfFile.FILE_TYPE, "The root node doesn't have any child nodes.");
            }

            return root;
        }
    }
}
