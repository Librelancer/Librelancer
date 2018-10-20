// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;

namespace LibreLancer.Utf
{
    public class RevConstruct : AbstractConstruct
    {
        public Vector3 Offset { get; set; }
        public Vector3 AxisRotation { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }

        private Matrix4 currentTransform = Matrix4.Identity;

		public override Matrix4 Transform { get { return internalGetTransform((Rotation * currentTransform) * Matrix4.CreateTranslation(Origin + Offset)); } }

        public RevConstruct(ConstructCollection constructs) : base(constructs) {}

        public RevConstruct(BinaryReader reader, ConstructCollection constructs)
            : base(reader, constructs)
        {
            Offset = ConvertData.ToVector3(reader);
            Rotation = ConvertData.ToMatrix3x3(reader);
            AxisRotation = ConvertData.ToVector3(reader);

            Min = reader.ReadSingle();
            Max = reader.ReadSingle();
        }

		protected RevConstruct(RevConstruct cf) : base(cf) { }
		public override AbstractConstruct Clone(ConstructCollection newcol)
		{
			var newc = new RevConstruct(this);
			newc.Offset = Offset;
			newc.AxisRotation = AxisRotation;
			newc.Min = Min;
			newc.Max = Max;
			newc.constructs = newcol;
			return newc;
		}
        public override void Reset()
        {
            currentTransform = Matrix4.Identity;
        }
        public float Current = 0;
        public override void Update(float distance)
        {
            Current = MathHelper.Clamp(distance, Min, Max);
			currentTransform = Matrix4.CreateFromAxisAngle(AxisRotation, Current);
        }
    }
}
