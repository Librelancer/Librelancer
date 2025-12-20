// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar
{
    [ParsedSection]
	public partial class LensGlow
	{
        [Entry("nickname")]
		public string Nickname;
        [Entry("shape")]
		public string Shape;
        [Entry("radius_scale")]
		public int RadiusScale;
        [Entry("inner_color")]
		public Color3f InnerColor;
        [Entry("outer_color")]
		public Color3f OuterColor;
        [Entry("glow_fade_in_seconds")]
		public float GlowFadeInSeconds;
        [Entry("glow_fade_out_seconds")]
		public float GlowFadeOutSeconds;
	}
}

