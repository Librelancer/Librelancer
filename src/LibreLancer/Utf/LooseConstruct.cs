// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
namespace LibreLancer.Utf
{
    public class LooseConstruct : AbstractConstruct
    {
        public override Matrix4 Transform { get { return internalGetTransform(Rotation * Matrix4.CreateTranslation(Origin)); } }

        public LooseConstruct(BinaryReader reader, ConstructCollection constructs)
            : base(reader, constructs)
        {
            Rotation = ConvertData.ToMatrix3x3(reader);
        }

		protected LooseConstruct(LooseConstruct cf) : base(cf) { }

		public override AbstractConstruct Clone(ConstructCollection newcol)
		{
			var newc = new LooseConstruct(this);
			newc.constructs = constructs;
			return newc;
		}
        public override void Reset()
        {
        }
        public override void Update(float distance)
        {
            throw new NotImplementedException();
        }
    }
}
