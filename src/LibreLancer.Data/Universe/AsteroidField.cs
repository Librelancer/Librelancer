// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
    [SelfSection("cube")]
	public class AsteroidField : ZoneReference
    {
        [Section("field")]
        public Field Field;
        
        [Entry("xaxis_rotation")]
        public Vector4? Cube_RotationX;
        [Entry("yaxis_rotation")]
        public Vector4? Cube_RotationY;
        [Entry("zaxis_rotation")]
        public Vector4? Cube_RotationZ;
        
        public List<CubeAsteroid> Cube = new List<CubeAsteroid>();
        
        [Section("band")] 
        public Band Band;
        [Section("exclusionband")] 
        public Band ExclusionBand;
        [Section("asteroidbillboards")]
        public AsteroidBillboards AsteroidBillboards;
        
        [Section("dynamicasteroids")]
		public List<DynamicAsteroids> DynamicAsteroids = new List<DynamicAsteroids>();
        
        [Section("lootablezone")]
		public List<LootableZone> LootableZones = new List<LootableZone>();

        [EntryHandler("asteroid", Multiline = true, MinComponents = 7)]
        void HandleAsteroid(Entry e) => Cube.Add(new CubeAsteroid(e));
    }
}