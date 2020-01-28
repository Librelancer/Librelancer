// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using LibreLancer.Ini;
namespace LibreLancer.Data.Universe
{
    public class NebulaBackgroundLightning
    {
        [Entry("duration")]
        public float Duration;
        [Entry("gap")]
        public float Gap;
        [Entry("color")]
        public Color4 Color;
    }
}