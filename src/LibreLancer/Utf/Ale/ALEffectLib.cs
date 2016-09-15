/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace LibreLancer.Utf.Ale
{
	public class ALEffectLib
	{
		public float Version;
		public List<ALEffect> Effects;

		public ALEffectLib (LeafNode node)
		{
			using (var reader = new BinaryReader (new MemoryStream (node.ByteArrayData))) {
				Version = reader.ReadSingle ();
				var effectCount = reader.ReadInt32 ();
				Effects = new List<ALEffect> (effectCount);
				for (int ef = 0; ef < effectCount; ef++) {
					ushort nameLen = reader.ReadUInt16 ();
					var name = Encoding.ASCII.GetString (reader.ReadBytes (nameLen)).TrimEnd ('\0');
					reader.BaseStream.Seek((nameLen & 1), SeekOrigin.Current);
					if (Version == 1.1f) {
						//Skip 4 unused floats
						reader.BaseStream.Seek(4 * sizeof(float), SeekOrigin.Current);
					}
					int fxCount = reader.ReadInt32();
					var refs = new List<AlchemyNodeRef> (fxCount);
					for (int i = 0; i < fxCount; i++) {
						refs.Add (new AlchemyNodeRef (
							reader.ReadUInt32(),
							reader.ReadUInt32(),
							reader.ReadUInt32(),
							reader.ReadUInt32()
						));
					}
					int pairsCount = reader.ReadInt32 ();
					var pairs = new List<Tuple<uint,uint>> (pairsCount);
					for (int i = 0; i < pairsCount; i++) {
						pairs.Add (new Tuple<uint, uint> (reader.ReadUInt32 (), reader.ReadUInt32 ()));
					}
					Effects.Add (
						new ALEffect () {
							Name = name,
							CRC = CrcTool.FLAleCrc(name),
							Fx = refs,
							Pairs = pairs
						}
					);
				}
			}
		}
	}
}

