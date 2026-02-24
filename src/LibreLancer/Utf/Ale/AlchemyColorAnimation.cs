// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyColorAnimation
	{
		public EasingTypes Type;
		public List<AlchemyColors> Items = new List<AlchemyColors> ();

        public AlchemyColorAnimation()
        {
            Type = EasingTypes.Linear;
        }

        public AlchemyColorAnimation(Color3f constant)
        {
            Items = [new() { Keyframes = [new(0, constant)] }];
        }

		public AlchemyColorAnimation (BinaryReader reader)
		{
			Type = (EasingTypes)reader.ReadByte ();
			int itemsCount = reader.ReadByte ();
			for (int fc = 0; fc < itemsCount; fc++) {
				var colors = new AlchemyColors ();
				colors.SParam = reader.ReadSingle ();
				colors.Type = (EasingTypes)reader.ReadByte ();
                var len = reader.ReadByte();
                colors.Keyframes = new RefList<ColorKeyframe>(len);
				for (int i = 0; i < len; i++) {
					colors.Keyframes.Add(new  (reader.ReadSingle (), new Color3f (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ())));
				}
				Items.Add (colors);
			}
		}
		public Color3f GetValue(float sparam, float time)
		{
			//1 item, 1 value
			if (Items.Count == 1) {
				return Items [0].GetValue (time);
			}
			//Find 2 keyframes to interpolate between
			AlchemyColors c1 = null, c2 = null;
			for (int i = 0; i < Items.Count - 1; i++) {
				if (sparam >= Items [i].SParam && sparam <= Items [i + 1].SParam) {
					c1 = Items [i];
					c2 = Items [i + 1];
                    break;
                }
			}
			//We're at the end
			if (c1 == null) {
				return Items [Items.Count - 1].GetValue(time);
			}
			//Interpolate between SParams
			var v1 = c1.GetValue (time);
			var v2 = c2.GetValue (time);
			return Easing.EaseColorRGB (Type, sparam, c1.SParam, c2.SParam, v1, v2);
		}
		public override string ToString ()
		{
			return string.Format ("<Canim: Type={0}, Count={1}>",Type,Items.Count);
		}
	}
}

