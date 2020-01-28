// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class ExclusionZone
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
        [Entry("exclude_billboards")] 
        public int? ExcludeBillboards;
        [Entry("exclude_dynamic_asteroids")] 
        public int? ExcludeDynamicAsteroids;
        [Entry("empty_cube_frequency")] 
        public float? EmptyCubeFrequency;
        [Entry("billboard_count")] 
        public int? BillboardCount;
        [Entry("zone_shell")]
        public string ZoneShellPath;
    }
}