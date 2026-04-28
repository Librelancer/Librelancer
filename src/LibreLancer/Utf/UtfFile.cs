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
        private const int TAG_LEN = 4;
        public const int FILE_VERSION = 257;


        [StructLayout(LayoutKind.Sequential)]
        private struct UtfHeader
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
            if (!buffer.SequenceEqual("UTF "u8))
                throw new FileFormatException(path, fileType, FILE_TYPE);

            long fileLength = reader.BaseStream.Length; // This is a syscall, cache.

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
            reader.ReadExactly(nodeBlock);

            stringBlock = new byte[header.StringBlockSize];
            reader.BaseStream.Seek(header.StringBlockOffset, SeekOrigin.Begin);
            reader.ReadExactly(stringBlock);

            dataBlock = new byte[(int)(fileLength - header.DataBlockOffset)];
            reader.BaseStream.Seek(header.DataBlockOffset, SeekOrigin.Begin);
            reader.ReadExactly(dataBlock);


            var root = Node.FromBuffer(nodeBlock, 0, new StringBlock(stringBlock), dataBlock) as IntermediateNode;
            return root ?? throw new FileContentException(FILE_TYPE, "The root node doesn't have any child nodes.");
        }
    }
}
