// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe
{
    [ParsedSection]
    public partial class NebulaBackgroundLightning
    {
        [Entry("duration")]
        public float Duration;
        [Entry("gap")]
        public float Gap;
        [Entry("color")]
        public Color4 Color;
    }
}
