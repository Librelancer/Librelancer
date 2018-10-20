// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Utf
{
    public class FixConstruct : AbstractConstruct
    {
		Matrix4 fixtransform;
        public override Matrix4 Transform { get { return internalGetTransform(fixtransform); } }

        public FixConstruct(ConstructCollection constructs) : base(constructs)
        {

        }

        public FixConstruct(BinaryReader reader, ConstructCollection constructs)
            : base(reader, constructs)
        {
            Rotation = ConvertData.ToMatrix3x3(reader);
			fixtransform = Rotation * Matrix4.CreateTranslation(Origin);
        }

		protected FixConstruct(FixConstruct cf) : base(cf) { }
		public override AbstractConstruct Clone(ConstructCollection newcol)
		{
			var newc = new FixConstruct(this);
			newc.constructs = newcol;
			newc.fixtransform = fixtransform;
			return newc;
		}
        public override void Reset()
        {
            fixtransform = Rotation * Matrix4.CreateTranslation(Origin);
        }
        public override void Update(float distance)
        {
            throw new NotImplementedException();
        }
    }
}
