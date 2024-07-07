// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;

namespace LibreLancer.Utf
{
    public class FixConstruct : AbstractConstruct
    {
		Transform3D fixtransform;
        public override Transform3D LocalTransform { get { return internalGetTransform(fixtransform); } }

        public FixConstruct() : base()
        {

        }

        public FixConstruct(BinaryReader reader)
            : base(reader)
        {
            Rotation = ConvertData.ToMatrix3x3(reader).ExtractRotation();
            fixtransform = new Transform3D(Origin, Rotation);
        }

		protected FixConstruct(FixConstruct cf) : base(cf) { }
		public override AbstractConstruct Clone()
		{
			var newc = new FixConstruct(this);
			newc.fixtransform = fixtransform;
			return newc;
		}
        public override void Reset()
        {
            fixtransform = new Transform3D(Origin, Rotation);
        }
        public override void Update(float distance, Quaternion quat)
        {
            throw new NotImplementedException();
        }
    }
}
