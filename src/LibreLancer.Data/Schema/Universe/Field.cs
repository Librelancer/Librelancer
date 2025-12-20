// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe
{
    [ParsedSection]
	public partial class Field
    {
        [Entry("cube_size")]
        public int? CubeSize;

        [Entry("fill_dist")]
        public int? FillDist;

        [Entry("tint_field")]
        public Color4? TintField;

        [Entry("max_alpha")]
        public float? MaxAlpha;

        [Entry("diffuse_color")]
        public Color4 DiffuseColor = Color4.White;

        [Entry("ambient_color")]
        public Color4? AmbientColor;

        [Entry("ambient_increase")]
        public Color4 AmbientIncrease = Color4.Black;

        [Entry("empty_cube_frequency")]
        public float? EmptyCubeFrequency;

        [Entry("contains_fog_zone")]
        public bool? ContainsFogZone;
    }
}
