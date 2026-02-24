// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;

namespace LibreLancer.Fx
{
	public class FxOrientedAppearance : FxBasicAppearance
	{
		public AlchemyFloatAnimation Height;
		public AlchemyFloatAnimation Width;

		public FxOrientedAppearance(AlchemyNode ale) : base(ale)
        {
            Height = ale.GetFloatAnimation(AleProperty.OrientedApp_Height);
            Width = ale.GetFloatAnimation(AleProperty.OrientedApp_Width);
		}

        public FxOrientedAppearance(string name) : base(name)
        {
            Size = null;
            Width = new(1);
            Height = new(1);
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.OrientedApp_Height, Height));
            n.Parameters.Add(new(AleProperty.OrientedApp_Width, Width));
            return n;
        }
	}
}

