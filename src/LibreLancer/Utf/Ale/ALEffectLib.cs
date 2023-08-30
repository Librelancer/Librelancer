// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibreLancer.Utf.Ale
{
    public class ALEffectLib
    {
        public float Version;
        public List<ALEffect> Effects;

        public ALEffectLib(LeafNode node)
        {
            using BinaryReader reader = new(node.DataSegment.GetReadStream());
            Version = reader.ReadSingle();

            int effectCount = reader.ReadInt32();
            Effects = new List<ALEffect>(effectCount);

            for (int ef = 0; ef < effectCount; ef++)
            {
                string name = ReadName(reader);
                SkipUnusedFloats(reader);
                Effects.Add(new ALEffect {
                    Name = name,
                    CRC = CrcTool.FLAleCrc(name),
                    Fx = ReadAlchemyNodeReferences(reader),
                    Pairs = ReadPairs(reader)
                });
            }
        }

        private static string ReadName(BinaryReader reader)
        {
            ushort nameLen = reader.ReadUInt16();
            Span<byte> nameBytes = stackalloc byte[nameLen];
            int bytesRead = reader.Read(nameBytes);

            Span<byte> name = nameBytes[..^1]; // Get rid of \0
            reader.BaseStream.Seek(nameLen & 1, SeekOrigin.Current);

            return Encoding.ASCII.GetString(name);
        }

        private void SkipUnusedFloats(BinaryReader reader)
        {
            if (Version != 1.1f)
                return;
            //Skip 4 unused floats
            reader.BaseStream.Seek(4 * sizeof(float), SeekOrigin.Current);
        }

        private static List<AlchemyNodeRef> ReadAlchemyNodeReferences(BinaryReader reader)
        {
            int fxCount = reader.ReadInt32();
            List<AlchemyNodeRef> refs = new(fxCount);
            for (int i = 0; i < fxCount; i++)
            {
                refs.Add(new AlchemyNodeRef(
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32()
                ));
            }

            return refs;
        }

        private static List<(uint, uint)> ReadPairs(BinaryReader reader)
        {
            int pairsCount = reader.ReadInt32();
            List<(uint, uint)> pairs = new(pairsCount);
            int remainingBytes = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
            int pairByteSize = (sizeof(uint) * 2);
            int requiredBytes = pairsCount * pairByteSize;

            // invalid pairs, i.e. emitter without appearance, will increase pair count but not amount of bytes.
            if (requiredBytes > remainingBytes) 
                pairsCount = remainingBytes / pairByteSize;

            for (int i = 0; i < pairsCount; i++)
                pairs.Add((reader.ReadUInt32(), reader.ReadUInt32()));

            return pairs;
        }
    }
}

