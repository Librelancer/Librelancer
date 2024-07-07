// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Numerics;


namespace LibreLancer.Utf
{
    public class RevConstruct : AbstractConstruct
    {
        public Vector3 Offset { get; set; }
        public Vector3 AxisRotation { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }

        private Quaternion currentTransform = Quaternion.Identity;

        public override Transform3D LocalTransform => internalGetTransform(new Transform3D(Origin + Offset, Rotation * currentTransform));

        public RevConstruct() : base() {}

        public RevConstruct(BinaryReader reader) : base(reader)
        {
            Offset = ConvertData.ToVector3(reader);
            Rotation = ConvertData.ToMatrix3x3(reader).ExtractRotation();
            AxisRotation = ConvertData.ToVector3(reader);

            Min = reader.ReadSingle();
            Max = reader.ReadSingle();
        }

		protected RevConstruct(RevConstruct cf) : base(cf) { }
		public override AbstractConstruct Clone()
		{
			var newc = new RevConstruct(this);
			newc.Offset = Offset;
			newc.AxisRotation = AxisRotation;
			newc.Min = Min;
			newc.Max = Max;
			return newc;
		}
        public override void Reset()
        {
            currentTransform = Quaternion.Identity;
        }
        public float Current = 0;
        public override void Update(float distance, Quaternion quat)
        {
            Current = MathHelper.Clamp(distance, Min, Max);
			currentTransform = Quaternion.CreateFromAxisAngle(AxisRotation, Current);
        }
    }
}
