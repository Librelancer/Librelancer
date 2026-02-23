// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxAirField : FxField
	{
        public AlchemyCurveAnimation Magnitude;
        public AlchemyCurveAnimation Approach;
        public FxAirField (AlchemyNode ale) : base(ale)
        {
            Magnitude = ale.GetCurveAnimation(AleProperty.AirField_Magnitude);
            Approach = ale.GetCurveAnimation(AleProperty.AirField_Approach);
        }

        public FxAirField(string name) : base(name)
        {
            Magnitude = new(1);
            Approach = new(1);
        }

	}
}

