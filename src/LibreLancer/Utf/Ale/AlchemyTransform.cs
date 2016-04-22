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

