// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Numerics;

namespace LibreLancer.Utf
{
    public class LooseConstruct : AbstractConstruct
    {
        public override Transform3D LocalTransform { get { return internalGetTransform(new Transform3D(Origin, Rotation)); } }

        public LooseConstruct() { }

        public LooseConstruct(BinaryReader reader)
            : base(reader)
        {
            Rotation = ConvertData.ToMatrix3x3(reader).ExtractRotation();
        }

		protected LooseConstruct(LooseConstruct cf) : base(cf) { }

		public override AbstractConstruct Clone()
		{
			var newc = new LooseConstruct(this);
			return newc;
		}
        public override void Reset()
        {
        }
        public override void Update(float distance, Quaternion quat)
        {
            throw new NotImplementedException();
        }
    }
}
