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
		public List<AlchemyNode> Nodes = new List<AlchemyNode> ();

        public AlchemyNodeLibrary()
        {
        }

		public AlchemyNodeLibrary (LeafNode utfleaf)
		{
			using (var reader = new BinaryReader (utfleaf.DataSegment.GetReadStream()))
            {
                reader.Skip(4); //Version 1.1f
				int nodeCount = reader.ReadInt32 ();
				for (int nc = 0; nc < nodeCount; nc++) {
					ushort nameLen = reader.ReadUInt16 ();
					var nodeName = Encoding.ASCII.GetString (reader.ReadBytes (nameLen)).TrimEnd ('\0');
					reader.BaseStream.Seek(nameLen & 1, SeekOrigin.Current); //padding
					var node = new AlchemyNode () { ClassName = nodeName };
					node.CRC = CrcTool.FLAleCrc(nodeName);
                    uint id;
                    AleProperty prop;
					while (true) {
						id = reader.ReadUInt16 ();
						if (id == 0)
							break;
						AleTypes type = (AleTypes)(id & 0x7FFF);
						prop = (AleProperty)reader.ReadUInt32 ();
						object value = null;
						switch (type) {
						case AleTypes.Boolean:
							value = (id & 0x8000) != 0 ? true : false;
							break;
						case AleTypes.Integer:
							value = reader.ReadUInt32 ();
							break;
						case AleTypes.Float:
							value = reader.ReadSingle ();
							break;
						case AleTypes.Name:
							var vallen = reader.ReadUInt16 ();
							if (vallen != 0)
								value = Encoding.ASCII.GetString (reader.ReadBytes (vallen)).TrimEnd ('\0');
							reader.BaseStream.Seek(vallen & 1, SeekOrigin.Current); //padding
							break;
						case AleTypes.IPair:
							value = new Tuple<uint,uint> (reader.ReadUInt32 (), reader.ReadUInt32 ());
							break;
						case AleTypes.Transform:
							value = new AlchemyTransform (reader);
							break;
						case AleTypes.FloatAnimation:
							value = new AlchemyFloatAnimation (reader);
							break;
						case AleTypes.ColorAnimation:
							value = new AlchemyColorAnimation (reader);
							break;
						case AleTypes.CurveAnimation:
							value = new AlchemyCurveAnimation (reader);
							break;
						default:
							throw new InvalidDataException ("Invalid ALE Type: 0x" + (id & 0x7FFF).ToString ("x"));
						}
						node.Parameters.Add (new AleParameter () { Name = prop, Value = value });
					}
					AleParameter temp;
					if (node.TryGetParameter(AleProperty.Node_Name, out temp))
					{
						var nn = (string)temp.Value;
                        node.NodeName = nn;
						node.CRC = CrcTool.FLAleCrc(nn);
					}
					Nodes.Add (node);
				}
			}
		}
	}
}

