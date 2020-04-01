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

        private Matrix4x4 currentTransform = Matrix4x4.Identity;

        public override Matrix4x4 LocalTransform { get { return internalGetTransform(Rotation * currentTransform * Matrix4x4.CreateTranslation(Origin + Offset)); } }

        public PrisConstruct() : base() {}

        public PrisConstruct(BinaryReader reader)
            : base(reader)
        {
            Offset = ConvertData.ToVector3(reader);
            Rotation = ConvertData.ToMatrix3x3(reader);
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
            currentTransform = Matrix4x4.Identity;
        }
        public override void Update(float distance, Quaternion quat)
        {
			Vector3 currentTranslation = AxisTranslation * MathHelper.Clamp(distance, Min, Max);
            currentTransform = Matrix4x4.CreateTranslation(currentTranslation);
        }
    }
}
