// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.IO;
using System.IO.Compression;
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
            var nodeBlock = reader.ReadBytes((int) nodeBlockLength);
            var dataBlock = reader.ReadBytes((int) dataBlockLength);
            using (BinaryReader nodeReader = new BinaryReader(new MemoryStream(nodeBlock)))
            {
                var root = Node.FromStreamV2(nodeReader, new StringBlock(stringBlock, true), dataBlock) as IntermediateNode;
                if (root == null)
                    throw new FileContentException(UtfFile.FILE_TYPE, "The root node doesn't have any child nodes.");
                return root;
            }
        }
        protected static IntermediateNode parseFile(string path, Stream stream)
        {
            byte[] nodeBlock;
            byte[] stringBlock;
            byte[] dataBlock;

			using (BinaryReader reader = new BinaryReader(stream))
            {
                byte[] buffer = new byte[TAG_LEN];
                reader.Read(buffer, 0, TAG_LEN);
                string fileType = Encoding.ASCII.GetString(buffer);
                if (fileType == "XUTF")
                {
                    return ParseV2(path, reader);
                }
                if (fileType != FILE_TYPE)
                    throw new FileFormatException(path, fileType, FILE_TYPE);

                int formatVersion = reader.ReadInt32();
                if (formatVersion != FILE_VERSION)
                    throw new FileVersionException(path, fileType, formatVersion, FILE_VERSION);


                int nodeBlockOffset = reader.ReadInt32();
                if (nodeBlockOffset > reader.BaseStream.Length)
                    throw new FileContentException(fileType, "The node block offset was out of range: " + nodeBlockOffset);

                int nodeBlockSize = reader.ReadInt32();
                if (nodeBlockOffset + nodeBlockSize > reader.BaseStream.Length)
                    throw new FileContentException(fileType, "The node block size was out of range: " + nodeBlockSize);

                //int zero = reader.ReadInt32();
                //int headerSize = reader.ReadInt32();
                reader.BaseStream.Seek(2 * sizeof(int), SeekOrigin.Current);

                int stringBlockOffset = reader.ReadInt32();
                if (stringBlockOffset > reader.BaseStream.Length)
                    throw new FileContentException(fileType, "The string block offset was out of range: " + stringBlockOffset);

                int stringBlockSize = reader.ReadInt32();
                if (stringBlockOffset + stringBlockSize > reader.BaseStream.Length)
                    throw new FileContentException(fileType, "The string block size was out of range: " + stringBlockSize);

                //int unknown = reader.ReadInt32();
                reader.BaseStream.Seek(sizeof(int), SeekOrigin.Current);

                int dataBlockOffset = reader.ReadInt32();
                if (dataBlockOffset > reader.BaseStream.Length)
                    throw new FileContentException(fileType, "The data block offset was out of range: " + dataBlockOffset);

                nodeBlock = new byte[nodeBlockSize];
                reader.BaseStream.Seek(nodeBlockOffset, SeekOrigin.Begin);
                reader.Read(nodeBlock, 0, nodeBlockSize);

                Array.Resize<byte>(ref buffer, stringBlockSize);
                reader.BaseStream.Seek(stringBlockOffset, SeekOrigin.Begin);
                reader.Read(buffer, 0, stringBlockSize);
                stringBlock = buffer;

                dataBlock = new byte[(int)(reader.BaseStream.Length - dataBlockOffset)];
                reader.BaseStream.Seek(dataBlockOffset, SeekOrigin.Begin);
                reader.Read(dataBlock, 0, dataBlock.Length);
            }

            IntermediateNode root = null;

            using (BinaryReader reader = new BinaryReader(new MemoryStream(nodeBlock)))
            {
                root = Node.FromStream(reader, 0, new StringBlock(stringBlock, false), dataBlock) as IntermediateNode;
                if (root == null)
                    throw new FileContentException(UtfFile.FILE_TYPE, "The root node doesn't have any child nodes.");
            }

            return root;
        }


    }
}
