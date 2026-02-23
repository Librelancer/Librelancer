// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxRadialField : FxField
    {
        public AlchemyCurveAnimation Radius;
        public AlchemyFloatAnimation Attenuation;
        public AlchemyCurveAnimation Magnitude;
        public AlchemyCurveAnimation Approach;
		public FxRadialField (AlchemyNode ale) : base(ale)
		{
            Radius = ale.GetCurveAnimation(AleProperty.RadialField_Radius);
            Attenuation = ale.GetFloatAnimation(AleProperty.RadialField_Attenuation);
            Magnitude = ale.GetCurveAnimation(AleProperty.RadialField_Magnitude);
            Approach = ale.GetCurveAnimation(AleProperty.RadialField_Approach);
		}

        public FxRadialField(string name) : base(name)
        {
            Radius = new(1);
            Attenuation = new(1);
            Magnitude = new(1);
            Approach = new(1);
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.RadialField_Radius, Radius));
            n.Parameters.Add(new(AleProperty.RadialField_Attenuation, Attenuation));
            n.Parameters.Add(new(AleProperty.RadialField_Magnitude, Magnitude));
            n.Parameters.Add(new(AleProperty.RadialField_Approach, Approach));
            return n;
        }
    }
}

