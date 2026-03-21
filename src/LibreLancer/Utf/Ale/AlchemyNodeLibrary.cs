// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using LibreLancer.Data;

namespace LibreLancer.Utf.Ale
{
    public class AlchemyNodeLibrary
    {
        public List<AlchemyNode> Nodes = [];

        public AlchemyNodeLibrary()
        {
        }

        static string ReadName(BinaryReader reader)
        {
            string value = "";
            var vallen = reader.ReadUInt16();
            if (vallen != 0)
                value = Encoding.UTF8.GetString(reader.ReadBytes(vallen)).TrimEnd('\0');
            reader.BaseStream.Seek(vallen & 1, SeekOrigin.Current); //padding
            return value;
        }

        public AlchemyNodeLibrary(LeafNode utfleaf)
        {
            using var reader = new BinaryReader(utfleaf.DataSegment.GetReadStream());
            reader.Skip(4); //Version 1.1f
            int nodeCount = reader.ReadInt32();

            for (int nc = 0; nc < nodeCount; nc++)
            {
                ushort nameLen = reader.ReadUInt16();
                var nodeName = Encoding.ASCII.GetString(reader.ReadBytes(nameLen)).TrimEnd('\0');
                reader.BaseStream.Seek(nameLen & 1, SeekOrigin.Current); //padding
                var node = new AlchemyNode
                {
                    ClassName = nodeName,
                    CRC = CrcTool.FLAleCrc(nodeName)
                };

                uint id;
                AleProperty prop;

                while (true)
                {
                    id = reader.ReadUInt16();
                    if (id == 0)
                        break;
                    AleTypes type = (AleTypes) (id & 0x7FFF);
                    prop = (AleProperty) reader.ReadUInt32();
                    object value = type switch
                    {
                        AleTypes.Boolean => (id & 0x8000) != 0 ? true : false,
                        AleTypes.Integer => reader.ReadUInt32(),
                        AleTypes.Float => reader.ReadSingle(),
                        AleTypes.Name => ReadName(reader),
                        AleTypes.IPair => new Tuple<uint, uint>(reader.ReadUInt32(), reader.ReadUInt32()),
                        AleTypes.Transform => new AlchemyTransform(reader),
                        AleTypes.FloatAnimation => new AlchemyFloatAnimation(reader),
                        AleTypes.ColorAnimation => new AlchemyColorAnimation(reader),
                        AleTypes.CurveAnimation => new AlchemyCurveAnimation(reader),
                        _ => throw new InvalidDataException("Invalid ALE Type: 0x" + (id & 0x7FFF).ToString("x"))
                    };
                    node.Parameters.Add(new AleParameter(prop, value));
                }

                if (node.TryGetParameter(AleProperty.Node_Name, out var temp))
                {
                    var nn = (string)temp.Value;
                    node.NodeName = nn;
                    node.CRC = CrcTool.FLAleCrc(nn);
                }

                Nodes.Add(node);
            }
        }
    }
}
