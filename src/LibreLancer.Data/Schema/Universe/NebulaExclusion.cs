// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe
{
    [ParsedSection]
	public partial class NebulaExclusion
    {
        [Entry("exclude")]
        [Entry("exclusion")]
        public string ZoneName;
        [Entry("fog_far")]
        public float? FogFar;
        [Entry("fog_near")]
        public float? FogNear;
        [Entry("shell_scalar")]
        public float? ShellScalar;
        [Entry("max_alpha")]
        public float? MaxAlpha;
        [Entry("color")]
        public Color4? Color;
        [Entry("exclusion_tint")]
        public Color3f? Tint;
        [Entry("zone_shell")]
        public string ZoneShellPath;
    }
}
