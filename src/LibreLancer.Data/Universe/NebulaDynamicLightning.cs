// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using LibreLancer.Ini;
namespace LibreLancer.Data.Universe
{
    public class NebulaDynamicLightning
    {
        [Entry("gap")]
        public float Gap;
        [Entry("duration")]
        public float Duration;
        [Entry("color")]
        public Color4 Color;
        [Entry("ambient_intensity")]
        public float AmbientIntensity;
        [Entry("intensity_increase")]
        public float IntensityIncrease;
    }
}