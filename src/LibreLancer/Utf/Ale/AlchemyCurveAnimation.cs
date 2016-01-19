using System;
using System.Collections.Generic;
using System.IO;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyCurveAnimation
	{
		public EasingTypes Type;
		public List<CurveParameterKeyframe> Keyframes;
		public AlchemyCurveAnimation (BinaryReader reader)
		{
			Type = (EasingTypes)reader.ReadByte ();
			int scount = reader.ReadByte ();
			Keyframes = new List<CurveParameterKeyframe> (scount);
			for (int i = 0; i < scount; i++) {
				var cpkf = new CurveParameterKeyframe ();
				cpkf.SParam = reader.ReadSingle ();
				cpkf.Value = reader.ReadSingle ();
				ushort loop = reader.ReadUInt16 ();
				ushort lcnt = reader.ReadUInt16 ();
				if (loop != 0 || lcnt != 0) {
					var l = new List<CurveKeyframe> (lcnt);
					for (int j = 0; j < lcnt; j++) {
						l.Add (new CurveKeyframe () {
							FrameIndex = reader.ReadSingle(),
							Value = reader.ReadSingle(),
							In = reader.ReadSingle(),
							Out = reader.ReadSingle()
						});
					}
					cpkf.Keyframes = l;
				}
				Keyframes.Add (cpkf);
			}
		}
	}
}

