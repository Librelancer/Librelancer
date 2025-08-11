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
            Height = ale.GetFloatAnimation("OrientedApp_Height");
            Width = ale.GetFloatAnimation("OrientedApp_Width");
		}

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new("OrientedApp_Height", Height));
            n.Parameters.Add(new("OrientedApp_Width", Width));
            return n;
        }
	}
}

