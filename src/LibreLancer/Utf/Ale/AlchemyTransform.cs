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
				RotateYaw = new AlchemyCurveAnimation (reader);
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

            var rotate = FromEulerAngles(
            	MathHelper.DegreesToRadians(RotatePitch.GetValue(sparam, time)),
				MathHelper.DegreesToRadians(RotateYaw.GetValue(sparam,time)),
				MathHelper.DegreesToRadians(RotateRoll.GetValue(sparam, time))
			);

			//var rotate = Matrix4.CreateFromQuaternion(quat);
          
            
			var s = new Vector3(ScaleX.GetValue(sparam, time),
								ScaleY.GetValue(sparam, time),
								ScaleZ.GetValue(sparam, time));
			var scale = Matrix4.CreateScale (s);
			return translate * rotate * scale;
		}

        static Matrix4 FromEulerAngles(float x, float y, float z)
        {
            var sinx = (float)Math.Sin(x);
            var cosx = (float)Math.Cos(x);

            var siny = (float)Math.Sin(y);
            var cosy = (float)Math.Cos(y);

            var sinz = (float)Math.Sin(z);
            var cosz = (float)Math.Cos(z);

            float sysx = siny * sinx;
            float cxsz = cosx * sinz;
            float cxcz = cosx * cosz;

            var m = new Matrix3();
            m.M11 = cosy * cosz;
            m.M12 = sysx * cosz - cxsz;
            m.M13 = siny * cxcz + sinx * sinz;

            m.M21 = cosy * sinz;
            m.M22 = sysx * sinz + cxcz;
            m.M23  = siny * cxsz - sinx * cosz;

            m.M31  = -siny;
            m.M32 = cosy * sinx;
            m.M33 = cosy * cosx;

            return new Matrix4(m);
        }

        public Quaternion GetDeltaRotation(float sparam, float t1, float t2)
        {
            if (!hasTransform)
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
			hasTransform = false;
		}
		public override string ToString ()
		{
			return string.Format ("<Xform: 0x{0:X}>", Xform);
		}
	}
}

