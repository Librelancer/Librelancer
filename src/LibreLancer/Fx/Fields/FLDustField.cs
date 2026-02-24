// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FLDustField : FxField
	{
        public AlchemyCurveAnimation MaxRadius;

        public FLDustField (AlchemyNode ale) : base(ale)
        {
            MaxRadius = ale.GetCurveAnimation(AleProperty.SphereEmitter_MaxRadius);
        }

        public FLDustField(string name) : base(name)
        {
            MaxRadius = new(1);
        }
	}
}

