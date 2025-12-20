// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar
{
    [ParsedSection]
	public partial class Star
	{
        [Entry("nickname")]
        public string Nickname;
        [Entry("star_glow")]
        public string StarGlow;
        [Entry("star_center")]
        public string StarCenter;
        [Entry("lens_flare")]
        public string LensFlare;
        [Entry("lens_glow")]
        public string LensGlow;
        [Entry("spines")]
        public string Spines;
        [Entry("intensity_fade_in")]
        public int IntensityFadeIn;
        [Entry("intensity_fade_out")]
        public int IntensityFadeOut;
        [Entry("zone_occlusion_fade_in")]
        public float? ZoneOcclusionFadeIn;
        [Entry("zone_occlusion_fade_out")]
        public float? ZoneOcclusionFadeOut;
        [Entry("radius")]
        public float Radius;
	}
}
