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
		public float GetValue(float sparam, float time)
		{
			//1 item, 1 value
			if (Items.Count == 1) {
				return Items [0].GetValue (time);
			}
			//Find 2 keyframes to interpolate between
			AlchemyFloats f1 = null, f2 = null;
			for (int i = 0; i < Items.Count - 1; i++) {
				if (sparam >= Items [i].SParam && sparam <= Items [i + 1].SParam) {
					f1 = Items [i];
					f2 = Items [i + 1];
				}
			}
			//We're at the end
			if (f1 == null) {
				return Items [Items.Count - 1].GetValue(time);
			}
			//Interpolate between SParams
			var v1 = f1.GetValue (time);
			var v2 = f2.GetValue (time);
			return AlchemyEasing.Ease (Type, sparam, f1.SParam, f2.SParam, v1, v2);
		}
		public override string ToString ()
		{
			return string.Format ("<Fanim: Type={0}, Count={1}>",Type,Items.Count);
		}
	}
}

