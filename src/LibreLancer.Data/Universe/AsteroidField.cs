// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
    [SelfSection("cube")]
	public class AsteroidField : ZoneReference
    {
        [Section("field")]
        public Field Field;
        
        public Vector4? Cube_RotationX;
        public Vector4? Cube_RotationY;
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

        bool HandleEntry(Entry e)
        {
            if (e.Name.Equals("xaxis_rotation", StringComparison.OrdinalIgnoreCase))
            {
                Cube_RotationX = new Vector4(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle());
                return true;
            } else if (e.Name.Equals("yaxis_rotation", StringComparison.OrdinalIgnoreCase))
            {
                Cube_RotationY = new Vector4(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle());
                return true;
            } else if (e.Name.Equals("zaxis_rotation", StringComparison.OrdinalIgnoreCase))
            {
                Cube_RotationZ = new Vector4(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle());
                return true;
            } else if (e.Name.Equals("asteroid"))
            {
                Cube.Add(new CubeAsteroid(e));
                return true;
            }

            return false;
        }
    }
}