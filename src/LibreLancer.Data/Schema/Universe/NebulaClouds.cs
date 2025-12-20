// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe
{
    [ParsedSection]
    public partial class NebulaClouds
    {
        [Entry("max_distance")]
        public int? MaxDistance;
        [Entry("puff_count")]
        public int? PuffCount;
        [Entry("puff_radius")]
        public int? PuffRadius;
        [Entry("puff_colora")]
        public Color3f? PuffColorA;
        [Entry("puff_colorb")]
        public Color3f? PuffColorB;
        [Entry("puff_max_alpha")]
        public float? PuffMaxAlpha;
        [Entry("puff_shape", Multiline = true)]
        public List<string> PuffShape = new List<string>();
        [Entry("puff_weights")]
        public int[] PuffWeights;
        [Entry("puff_drift")]
        public float? PuffDrift;
        [Entry("near_fade_distance")]
        public Vector2? NearFadeDistance;
        [Entry("lightning_intensity")]
        public float? LightningIntensity;
        [Entry("lightning_color")]
        public Color4? LightningColor;
        [Entry("lightning_gap")]
        public float? LightningGap;
        [Entry("lightning_duration")]
        public float? LightningDuration;
    }
}
