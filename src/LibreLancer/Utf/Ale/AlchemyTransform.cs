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
		public AlchemyCurveAnimation RotatePitch;
		public AlchemyCurveAnimation RotateYaw;
		public AlchemyCurveAnimation RotateRoll;
		public AlchemyCurveAnimation ScaleX;
		public AlchemyCurveAnimation ScaleY;
		public AlchemyCurveAnimation ScaleZ;
		bool hasTransform;
		public AlchemyTransform (BinaryReader reader)
		{
			Xform = (uint)reader.ReadByte () << 8;
			Xform |= (uint)reader.ReadByte () << 4;
			Xform |= (uint)reader.ReadByte ();

			hasTransform = reader.ReadByte () != 0;
			if (hasTransform) {
				TranslateX = new AlchemyCurveAnimation (reader);
				TranslateY = new AlchemyCurveAnimation (reader);
				TranslateZ = new AlchemyCurveAnimation (reader);
				RotatePitch = new AlchemyCurveAnimation (reader);
				RotateYaw= new AlchemyCurveAnimation (reader);
				RotateRoll = new AlchemyCurveAnimation (reader);
				ScaleX = new AlchemyCurveAnimation (reader);
				ScaleY = new AlchemyCurveAnimation (reader);
				ScaleZ = new AlchemyCurveAnimation (reader);
			}
		}
		public Matrix4 GetMatrix(float sparam, float time)
		{
			if (!hasTransform)
				return Matrix4.Identity;
			var translate = Matrix4.CreateTranslation (
				TranslateX.GetValue (sparam, time),
				TranslateY.GetValue (sparam, time),
				TranslateZ.GetValue (sparam, time)
			);
			
			var quat = Quaternion.FromEulerAngles(
				MathHelper.TwoPi - MathHelper.DegreesToRadians(RotatePitch.GetValue(sparam, time)),
				MathHelper.TwoPi - MathHelper.DegreesToRadians(RotateYaw.GetValue(sparam,time)),
				MathHelper.TwoPi - MathHelper.DegreesToRadians(RotateRoll.GetValue(sparam, time))
			);

			var rotate = Matrix4.CreateFromQuaternion(quat);
			var s = new Vector3(ScaleX.GetValue(sparam, time),
								ScaleY.GetValue(sparam, time),
								ScaleZ.GetValue(sparam, time));
			var scale = Matrix4.CreateScale (s);
			return translate * rotate  * scale;
		}
		public AlchemyTransform()
		{
			hasTransform = false;
		}
		public override string ToString ()
		{
			return string.Format ("<Xform: 0x{0:X}>", Xform);
		}
	}
}

