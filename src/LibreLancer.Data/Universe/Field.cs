// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class Field
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
        public Color4? DiffuseColor;
        
        [Entry("ambient_color")] 
        public Color4? AmbientColor;
        
        [Entry("ambient_increase")] 
        public Color4? AmbientIncrease;
        
        [Entry("empty_cube_frequency")] 
        public float? EmptyCubeFrequency;
        
        [Entry("contains_fog_zone")] 
        public bool? ContainsFogZone;
    }
}