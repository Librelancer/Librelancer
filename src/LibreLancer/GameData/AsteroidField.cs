// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.GameData
{
	public class AsteroidField
	{
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
			FillDist = fillDist * FILLDIST_MULTIPLIER;
		}
		public float EmptyCubeFrequency;
		public int BillboardCount;
		public float BillboardDistance;
		public float BillboardFadePercentage;
		public TextureShape BillboardShape;
		public Vector2 BillboardSize;
		public Color3f BillboardTint;

		//Multiplier hardcoded in Freelancer's common.dll
		const float FILLDIST_MULTIPLIER = 1.74f;

	}
}

