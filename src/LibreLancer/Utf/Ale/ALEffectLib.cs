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
		public ALEffectLib (LeafNode node)
		{
			using (var reader = new BinaryReader (new MemoryStream (node.ByteArrayData))) {
				Version = reader.ReadSingle ();
				var effectCount = reader.ReadInt32 ();
				Effects = new List<ALEffect> (effectCount);
				for (int ef = 0; ef < effectCount; ef++) {
					ushort nameLen = reader.ReadUInt16 ();
					var name = Encoding.ASCII.GetString (reader.ReadBytes (nameLen)).TrimEnd ('\0');
					if (Version == 1.1f) {
						//Skip 4 unused floats
						reader.BaseStream.Seek(4 * sizeof(float), SeekOrigin.Current);
					}
					int fxCount = reader.ReadInt32();
					var refs = new List<EffectReference> (fxCount);
					for (int i = 0; i < fxCount; i++) {
						refs.Add (new EffectReference (
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
					Effects.Add (new ALEffect () { Name = name, Fx = refs, Pairs = pairs });
				}
			}
		}
	}
}

