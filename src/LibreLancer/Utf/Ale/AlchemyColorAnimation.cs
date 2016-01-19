using System;
using System.Collections.Generic;
using System.IO;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyColorAnimation
	{
		public EasingTypes Type;
		public List<AlchemyColors> Items = new List<AlchemyColors> ();
		public AlchemyColorAnimation (BinaryReader reader)
		{
			Type = (EasingTypes)reader.ReadByte ();
			int itemsCount = reader.ReadByte ();
			for (int fc = 0; fc < itemsCount; fc++) {
				var colors = new AlchemyColors ();
				colors.SParam = reader.ReadSingle ();
				colors.Type = (EasingTypes)reader.ReadByte ();
				colors.Data = new Tuple<float, Color3f>[reader.ReadByte ()];
				for (int i = 0; i < colors.Data.Length; i++) {
					colors.Data [i] = new Tuple<float, Color3f> (reader.ReadSingle (), new Color3f (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ()));
				}
				Items.Add (colors);
			}
		}
		public override string ToString ()
		{
			return string.Format ("<Canim: Type={0}, Count={1}>",Type,Items.Count);
		}
	}
}

