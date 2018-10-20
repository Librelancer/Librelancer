// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;

namespace LibreLancer.Utf
{
    public class PrisConstruct : AbstractConstruct
    {
        public Vector3 Offset { get; set; }
        public Vector3 AxisTranslation { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }

        private Matrix4 currentTransform = Matrix4.Identity;

        public override Matrix4 Transform { get { return internalGetTransform(Rotation * currentTransform * Matrix4.CreateTranslation(Origin + Offset)); } }

        public PrisConstruct(ConstructCollection constructs) : base(constructs) {}

        public PrisConstruct(BinaryReader reader, ConstructCollection constructs)
            : base(reader, constructs)
        {
            Offset = ConvertData.ToVector3(reader);
            Rotation = ConvertData.ToMatrix3x3(reader);
            AxisTranslation = ConvertData.ToVector3(reader);

            Min = reader.ReadSingle();
            Max = reader.ReadSingle();
        }
		protected PrisConstruct(PrisConstruct cloneFrom) : base(cloneFrom) { }
		public override AbstractConstruct Clone(ConstructCollection newcol)
		{
			var newc = new PrisConstruct(this);
			newc.constructs = newcol;
			newc.Offset = Offset;
			newc.AxisTranslation = AxisTranslation;
			newc.Min = Min;
			newc.Max = Max;
			return newc;
		}
        public override void Reset()
        {
            currentTransform = Matrix4.Identity;
        }
        public override void Update(float distance)
        {
			Vector3 currentTranslation = AxisTranslation * MathHelper.Clamp(distance, Min, Max);
            currentTransform = Matrix4.CreateTranslation(currentTranslation);
        }
    }
}
