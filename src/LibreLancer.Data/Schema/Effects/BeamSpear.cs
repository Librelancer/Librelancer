// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Effects
{
    [ParsedSection]
    public partial class BeamSpear
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("tip_length")]
        public float TipLength;
        [Entry("tail_length")]
        public float TailLength;
        [Entry("head_width")]
        public float HeadWidth;
        [Entry("core_width")]
        public float CoreWidth;
        [Entry("tip_color")]
        public Color4 TipColor;
        [Entry("core_color")]
        public Color4 CoreColor;
        [Entry("outter_color")] //Intentional
        public Color4 OuterColor;
        [Entry("tail_color")]
        public Color4 TailColor;
        [Entry("head_brightness")]
        public float HeadBrightness;
        [Entry("trail_brightness")]
        public float TrailBrightness;
        [Entry("head_texture")]
        public string HeadTexture;
        [Entry("trail_texture")]
        public string TrailTexture;
        [Entry("flash_size")]
        public float FlashSize;
    }
}
