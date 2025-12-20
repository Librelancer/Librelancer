// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe
{
    [ParsedSection]
    public partial class NebulaExterior
    {
        [Entry("shape", Multiline = true)]
        public List<string> Shape = new List<string>();
        [Entry("shape_weights")]
        public int[] ShapeWeights;
        [Entry("fill_shape")]
        public string FillShape;
        [Entry("plane_slices")]
        public int? PlaneSlices;
        [Entry("bit_radius")]
        public int? BitRadius;
        [Entry("bit_radius_random_variation")]
        public int? BitRadiusRandomVariation;
        [Entry("min_bits")]
        public int? MinBits;
        [Entry("max_bits")]
        public int? MaxBits;
        [Entry("move_bit_percent")]
        public float? MoveBitPercent;
        [Entry("equator_bias")]
        public float? EquatorBias;
        [Entry("color")]
        public Color4? Color;
        [Entry("opacity")]
        public float Opacity;
    }
}
