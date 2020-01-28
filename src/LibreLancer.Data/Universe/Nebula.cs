// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class Nebula : ZoneReference
    {
        [Section("fog")] 
        public NebulaFog Fog;

        [Section("exterior")]
        public NebulaExterior Exterior;

        [Section("nebulalight")]
		public List<NebulaLight> NebulaLights = new List<NebulaLight>();

        [Section("clouds")] 
        public List<NebulaClouds> Clouds = new List<NebulaClouds>();

        [Section("backgroundlightning")] 
        public NebulaBackgroundLightning BackgroundLightning;

        [Section("dynamiclightning")] 
        public NebulaDynamicLightning DynamicLightning;
    }
}