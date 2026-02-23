// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxGravityField : FxField
    {
        public AlchemyCurveAnimation Gravity;
		public FxGravityField (AlchemyNode ale) : base(ale)
		{
            Gravity = ale.GetCurveAnimation(AleProperty.GravityField_Gravity);
		}

        public FxGravityField(string name) : base(name)
        {
            Gravity = new(1);
        }


        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.GravityField_Gravity, Gravity));
            return n;
        }
    }
}

