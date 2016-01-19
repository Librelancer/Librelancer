using System;
using System.Collections.Generic;
using System.IO;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyFloatAnimation
	{
		public EasingTypes Type;
		public List<AlchemyFloats> Items = new List<AlchemyFloats> ();
		public AlchemyFloatAnimation (BinaryReader reader)
		{
			Type = (EasingTypes)reader.ReadByte ();
			int itemsCount = reader.ReadByte ();
			for (int fc = 0; fc < itemsCount; fc++) {
				var floats = new AlchemyFloats ();
				floats.SParam = reader.ReadSingle ();
				floats.Type = (EasingTypes)reader.ReadByte ();
				floats.Data = new Tuple<float, float>[reader.ReadByte ()];
				for (int i = 0; i < floats.Data.Length; i++) {
					floats.Data [i] = new Tuple<float, float> (reader.ReadSingle (), reader.ReadSingle ());
				}
				Items.Add (floats);
			}
		}
		public override string ToString ()
		{
			return string.Format ("<Fanim: Type={0}, Count={1}>",Type,Items.Count);
		}
	}
}

