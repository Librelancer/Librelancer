// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxCollideField : FxField
	{
        public AlchemyCurveAnimation Reflectivity;
        public AlchemyCurveAnimation Width;
        public AlchemyCurveAnimation Height;
        public FxCollideField (AlchemyNode ale) : base(ale)
        {
            Reflectivity = ale.GetCurveAnimation(AleProperty.CollideField_Reflectivity);
            Width = ale.GetCurveAnimation(AleProperty.CollideField_Width);
            Height = ale.GetCurveAnimation(AleProperty.CollideField_Height);
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.CollideField_Reflectivity, Reflectivity));
            n.Parameters.Add(new(AleProperty.CollideField_Width, Width));
            n.Parameters.Add(new(AleProperty.CollideField_Height, Height));
            return n;
        }
    }
}

