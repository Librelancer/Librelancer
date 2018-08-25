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
        public bool HasTransform;
		public AlchemyTransform (BinaryReader reader)
		{
			Xform = (uint)reader.ReadByte () << 8;
			Xform |= (uint)reader.ReadByte () << 4;
			Xform |= (uint)reader.ReadByte ();

			HasTransform = reader.ReadByte () != 0;
			if (HasTransform) {
				TranslateX = new AlchemyCurveAnimation (reader);
				TranslateY = new AlchemyCurveAnimation (reader);
				TranslateZ = new AlchemyCurveAnimation (reader);
				RotatePitch = new AlchemyCurveAnimation (reader);
				RotateYaw = new AlchemyCurveAnimation (reader);
				RotateRoll = new AlchemyCurveAnimation (reader);
				ScaleX = new AlchemyCurveAnimation (reader);
				ScaleY = new AlchemyCurveAnimation (reader);
				ScaleZ = new AlchemyCurveAnimation (reader);
			}
		}
		
        public Vector3 GetTranslation(float sparam, float t)
        {
            if (!HasTransform)
                return Vector3.Zero;
            var x = TranslateX.GetValue(sparam, t);
            var y = TranslateY.GetValue(sparam, t);
            var z = TranslateZ.GetValue(sparam, t);

            return new Vector3(x, y, z);
        }

        public Vector3 GetDeltaTranslation(float sparam, float t1, float t2)
        {
            if (!HasTransform)
                return Vector3.Zero;
            var x1 = TranslateX.GetValue(sparam, t1);
            var y1 = TranslateY.GetValue(sparam, t1);
            var z1 = TranslateZ.GetValue(sparam, t1);

            var x2 = TranslateX.GetValue(sparam, t2);
            var y2 = TranslateY.GetValue(sparam, t2);
            var z2 = TranslateZ.GetValue(sparam, t2);

            return new Vector3(x2 - x1, y2 - y1, z2 - z1);
        }

        public Quaternion GetRotation(float sparam, float t)
        {
            if (!HasTransform)
                return Quaternion.Identity;
            var x = RotatePitch.GetValue(sparam, t);
            var y = RotateYaw.GetValue(sparam, t);
            var z = RotateRoll.GetValue(sparam, t);
            return Quaternion.FromEulerAngles(
                MathHelper.DegreesToRadians(x),
                MathHelper.DegreesToRadians(y),
                MathHelper.DegreesToRadians(z)
            );
        }

        public Quaternion GetDeltaRotation(float sparam, float t1, float t2)
        {
            if (!HasTransform)
                return Quaternion.Identity;
            var x1 = RotatePitch.GetValue(sparam, t1);
            var y1 = RotateYaw.GetValue(sparam, t1);
            var z1 = RotateRoll.GetValue(sparam, t1);

            var x2 = RotatePitch.GetValue(sparam, t2);
            var y2 = RotateYaw.GetValue(sparam, t2);
            var z2 = RotateRoll.GetValue(sparam, t2);

            return Quaternion.FromEulerAngles(
                MathHelper.DegreesToRadians(x2 - x1), 
                MathHelper.DegreesToRadians(y2 - y1),
                MathHelper.DegreesToRadians(z2 - z1)
            );
        }

		public AlchemyTransform()
		{
			HasTransform = false;
		}
		public override string ToString ()
		{
			return string.Format ("<Xform: 0x{0:X}>", Xform);
		}
	}
}

