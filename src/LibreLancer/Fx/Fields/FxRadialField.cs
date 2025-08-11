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
            Radius = ale.GetCurveAnimation("RadialField_Radius");
            Attenuation = ale.GetFloatAnimation("RadialField_Attenuation");
            Magnitude = ale.GetCurveAnimation("RadialField_Magnitude");
            Approach = ale.GetCurveAnimation("RadialField_Approach");
		}

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new ("RadialField_Radius", Radius));
            n.Parameters.Add(new ("RadialField_Attenuation", Attenuation));
            n.Parameters.Add(new ("RadialField_Magnitude", Magnitude));
            n.Parameters.Add(new ("RadialField_Approach", Approach));
            return n;
        }
    }
}

