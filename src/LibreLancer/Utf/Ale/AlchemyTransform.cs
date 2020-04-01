// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Numerics;

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
        public bool Animates;
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
                Animates = (TranslateX.Animates || TranslateY.Animates ||
                    TranslateZ.Animates || RotatePitch.Animates || RotateRoll.Animates ||
                    RotateYaw.Animates || ScaleX.Animates || ScaleY.Animates || ScaleZ.Animates);
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
            return Quaternion.CreateFromYawPitchRoll(
                MathHelper.DegreesToRadians(y),
                MathHelper.DegreesToRadians(x),
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

            return Quaternion.CreateFromYawPitchRoll(
                MathHelper.DegreesToRadians(y2 - y1),
                MathHelper.DegreesToRadians(x2 - x1), 
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

