// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Numerics;


namespace LibreLancer.Utf
{
    public class PrisConstruct : AbstractConstruct
    {
        public Vector3 Offset { get; set; }
        public Vector3 AxisTranslation { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }

        private Vector3 currentTranslation = Vector3.Zero;

        public override Transform3D LocalTransform
        {
            get
            {
                return internalGetTransform(new Transform3D(currentTranslation, Quaternion.Identity) * new Transform3D( Offset + Origin, Rotation));
            }
        }

        public PrisConstruct() : base() {}

        public PrisConstruct(BinaryReader reader)
            : base(reader)
        {
            Offset = ConvertData.ToVector3(reader);
            Rotation = ConvertData.ToMatrix3x3(reader).ExtractRotation();
            AxisTranslation = ConvertData.ToVector3(reader);

            Min = reader.ReadSingle();
            Max = reader.ReadSingle();
        }
		protected PrisConstruct(PrisConstruct cloneFrom) : base(cloneFrom) { }
		public override AbstractConstruct Clone()
		{
			var newc = new PrisConstruct(this);
			newc.Offset = Offset;
			newc.AxisTranslation = AxisTranslation;
			newc.Min = Min;
			newc.Max = Max;
			return newc;
		}
        public override void Reset()
        {
            currentTranslation = Vector3.Zero;
        }
        public override void Update(float distance, Quaternion quat)
        {
            currentTranslation = AxisTranslation * MathHelper.Clamp(distance, Min, Max);
        }
    }
}
