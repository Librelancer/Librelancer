// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace LibreLancer.Data.GameData.World
{
	public class Nebula
    {
        public string SourceFile;
        public List<ResolvedTexturePanels> TexturePanels = new();
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
		public WeightedRandomCollection<string> ExteriorCloudShapes;
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
		public WeightedRandomCollection<string> InteriorCloudShapes;
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
		public List<NebulaExclusionZone> ExclusionZones;

        public Nebula Clone(Dictionary<string, Zone> newZones)
        {
            var o = (Nebula)MemberwiseClone();
            o.Zone = Zone == null
                ? null
                : newZones.GetValueOrDefault(Zone.Nickname);
            o.ExteriorCloudShapes = ExteriorCloudShapes?.Clone();
            o.InteriorCloudShapes = InteriorCloudShapes?.Clone();
            if (ExclusionZones != null)
                o.ExclusionZones = ExclusionZones.Select(x => x.Clone(newZones)).ToList();
            return o;
        }
    }
}

