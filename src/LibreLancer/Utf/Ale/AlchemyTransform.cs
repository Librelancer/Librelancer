using System;
using System.IO;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyTransform
	{
		public uint Xform;
		public AlchemyCurveAnimation TranslateX;
		public AlchemyCurveAnimation TranslateY;
		public AlchemyCurveAnimation TranslateZ;
		public AlchemyCurveAnimation RotateX;
		public AlchemyCurveAnimation RotateY;
		public AlchemyCurveAnimation RotateZ;
		public AlchemyCurveAnimation ScaleX;
		public AlchemyCurveAnimation ScaleY;
		public AlchemyCurveAnimation ScaleZ;
		public AlchemyTransform (BinaryReader reader)
		{
			Xform = (uint)reader.ReadByte () << 8;
			Xform |= (uint)reader.ReadByte () << 4;
			Xform |= (uint)reader.ReadByte ();

			var hasTransform = reader.ReadByte () != 0;
			if (hasTransform) {
				TranslateX = new AlchemyCurveAnimation (reader);
				TranslateY = new AlchemyCurveAnimation (reader);
				TranslateZ = new AlchemyCurveAnimation (reader);
				RotateX = new AlchemyCurveAnimation (reader);
				RotateY = new AlchemyCurveAnimation (reader);
				RotateZ = new AlchemyCurveAnimation (reader);
				ScaleX = new AlchemyCurveAnimation (reader);
				ScaleY = new AlchemyCurveAnimation (reader);
				ScaleZ = new AlchemyCurveAnimation (reader);
			}
		}

		public override string ToString ()
		{
			return string.Format ("<Xform: 0x{0:X}>", Xform);
		}
	}
}

