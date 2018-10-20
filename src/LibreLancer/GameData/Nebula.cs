// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Utf.Mat;

namespace LibreLancer.GameData
{
	public class Nebula
	{
		public Zone Zone;
		//Exterior
		public string ExteriorFill;
		public Color4 ExteriorColor;
		public bool HasExteriorBits = false;
		public int ExteriorMinBits;
		public int ExteriorMaxBits;
		public float ExteriorBitRadius;
		public float ExteriorBitRandomVariation;
		public float ExteriorMoveBitPercent;
		public WeightedRandomCollection<CloudShape> ExteriorCloudShapes;
		//Fog + Lighting
		public bool FogEnabled;
		public Color4 FogColor;
		public Vector2 FogRange;
		public Color4? AmbientColor;
		//Dynamic Lightning
		public bool DynamicLightning;
		public float DynamicLightningDuration;
		public float DynamicLightningGap;
		public Color4 DynamicLightningColor;
		//Background Lightning
		public bool BackgroundLightning;
		public float BackgroundLightningDuration;
		public float BackgroundLightningGap;
		public Color4 BackgroundLightningColor;
		//Cloud Lightning
		public bool CloudLightning;
		public float CloudLightningDuration;
		public float CloudLightningGap;
		public float CloudLightningIntensity;
		public Color4 CloudLightningColor;
		//Interior
		public bool HasInteriorClouds = false;
		public WeightedRandomCollection<CloudShape> InteriorCloudShapes;
		public Color3f InteriorCloudColorA;
		public Color3f InteriorCloudColorB;
		public int InteriorCloudRadius;
		public int InteriorCloudCount;
		public int InteriorCloudMaxDistance;
		public Vector2 InteriorCloudFadeDistance;
		public float InteriorCloudMaxAlpha;
		public float InteriorCloudDrift;
		//Sun burnthrough
		public float SunBurnthroughScale;
		public float SunBurnthroughIntensity;
		//Exclusion
		public List<ExclusionZone> ExclusionZones;
	}
	public struct CloudShape
	{
		public string Texture;
		public RectangleF Dimensions;
		public CloudShape(string tex, RectangleF dim)
		{
			Texture = tex;
			Dimensions = dim;
		}
	}
}

