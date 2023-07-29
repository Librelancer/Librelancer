// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Render;

namespace LibreLancer.GameData.World
{
	public class AsteroidField
    {
        public string SourceFile;
		public Zone Zone;
		public AsteroidBand Band;
		public AsteroidCubeRotation CubeRotation;
		public List<StaticAsteroid> Cube;
		public List<ExclusionZone> ExclusionZones;
		public int CubeSize;
		public bool AllowMultipleMaterials = false;
		public float FillDist { get; private set; }
		public void SetFillDist(int fillDist)
		{
			FillDist = fillDist;
		}
		public float EmptyCubeFrequency;
		public int BillboardCount;
		public float BillboardDistance;
		public float BillboardFadePercentage;
		public TextureShape BillboardShape;
		public Vector2 BillboardSize;
		public Color3f BillboardTint;

		//Multiplier hardcoded in Freelancer's common.dll
        //This is for near_field, not for rendering
		//const float FILLDIST_MULTIPLIER = 1.74f;

        public AsteroidField Clone(Dictionary<string,Zone> newZones)
        {
            var o = (AsteroidField) MemberwiseClone();
            o.Zone = Zone == null
                ? null
                : newZones.GetValueOrDefault(Zone.Nickname);
            o.Band = Band?.Clone();
            o.CubeRotation = CubeRotation?.Clone();
            o.Cube = Cube.CloneCopy();
            if (ExclusionZones != null)
                o.ExclusionZones = ExclusionZones.Select(x => x.Clone(newZones)).ToList();
            return o;
        }
    }
}

