// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxTurbulenceField : FxField
	{
        public AlchemyCurveAnimation Magnitude;
        public AlchemyCurveAnimation Approach;
        public FxTurbulenceField (AlchemyNode ale) : base(ale)
        {
            Magnitude = ale.GetCurveAnimation(AleProperty.TurbulenceField_Magnitude);
            Approach = ale.GetCurveAnimation(AleProperty.TurbulenceField_Approach);
        }

        public FxTurbulenceField(string name) : base(name)
        {
            Magnitude = new(1);
            Approach = new(1);
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.TurbulenceField_Magnitude, Magnitude));
            n.Parameters.Add(new(AleProperty.TurbulenceField_Approach, Approach));
            return n;
        }
    }
}

