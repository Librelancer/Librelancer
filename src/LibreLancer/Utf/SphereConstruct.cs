// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Utf
{
    public class SphereConstruct : AbstractConstruct
    {
		public Vector3 Offset { get; set; }
        public float Min1 { get; set; }
        public float Max1 { get; set; }
        public float Min2 { get; set; }
        public float Max2 { get; set; }
        public float Min3 { get; set; }
        public float Max3 { get; set; }

        public override Matrix4 Transform { get { return internalGetTransform(Rotation * Matrix4.CreateTranslation(Origin + Offset)); } }

        public SphereConstruct(ConstructCollection constructs) : base(constructs) {}

        public SphereConstruct(BinaryReader reader, ConstructCollection constructs)
            : base(reader, constructs)
        {
            Offset = ConvertData.ToVector3(reader);
            Rotation = ConvertData.ToMatrix3x3(reader);

            Min1 = reader.ReadSingle();
            Max1 = reader.ReadSingle();
            Min2 = reader.ReadSingle();
            Max2 = reader.ReadSingle();
            Min3 = reader.ReadSingle();
            Max3 = reader.ReadSingle();
        }
		protected SphereConstruct(SphereConstruct cf) : base(cf) { }
		public override AbstractConstruct Clone(ConstructCollection newcol)
		{
			var newc = new SphereConstruct(this);
			newc.constructs = newcol;
			newc.Offset = Offset;
			newc.Min1 = Min1;
			newc.Min2 = Min2;
			newc.Min3 = Min3;
			newc.Max1 = Max1;
			newc.Max2 = Max2;
			newc.Max3 = Max3;
			return newc;
		}
        public override void Reset()
        {
        }
        public override void Update(float distance)
        {
            //throw new NotImplementedException();
        }
    }
}
