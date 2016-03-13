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
							FxTree = BuildTree(refs),
							Fx = refs,
							Pairs = pairs
						}
					);
				}
			}
		}
		static List<AlchemyNodeRef> BuildTree(IEnumerable<AlchemyNodeRef> source)
		{
			var groups = source.GroupBy (i => i.Parent);

			var roots = groups.FirstOrDefault(g => g.Key == 32768).ToList();

			if (roots.Count > 0)
			{
				var dict = groups.Where(g => g.Key != 32768).ToDictionary(g => g.Key, g => g.ToList());
				for (int i = 0; i < roots.Count; i++)
					AddChildren(roots[i], dict);
			}

			return roots;
		}
		private static void AddChildren(AlchemyNodeRef node, Dictionary<uint, List<AlchemyNodeRef>> source)
		{
			if (source.ContainsKey(node.Index))
			{
				node.Children = source[node.Index];
				for (int i = 0; i < node.Children.Count; i++)
					AddChildren(node.Children[i], source);
			}
			else
			{
				node.Children = new List<AlchemyNodeRef>();
			}
		}
	}
}

