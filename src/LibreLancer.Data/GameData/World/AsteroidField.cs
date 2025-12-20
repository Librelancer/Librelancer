// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace LibreLancer.Data.GameData.World
{
	public class AsteroidField : IDataEquatable<AsteroidField>
    {
        public string SourceFile;
        public List<ResolvedTexturePanels> TexturePanels = new();
        public Zone Zone;
        //Field
        public Color4 DiffuseColor = Color4.White;
        public Color4 AmbientColor = Color4.White;
        public Color4 AmbientIncrease = Color4.Black;
        public float FillDist;
        public float EmptyCubeFrequency;
        public int CubeSize;
        //Cube
        public AsteroidCubeRotation CubeRotation;
        public List<StaticAsteroid> Cube;
        //Billboards
        public int BillboardCount;
        public float BillboardDistance;
        public float BillboardFadePercentage;
        public string BillboardShape;
        public Vector2 BillboardSize;
        public Color3f BillboardTint;
		public AsteroidBand Band;

        public List<DynamicAsteroids> DynamicAsteroids = new();
        public List<AsteroidExclusionZone> ExclusionZones = new();

        public DynamicLootZone FieldLoot;
        public List<DynamicLootZone> LootZones = new();
		public bool AllowMultipleMaterials = false;

		//Properties
        public FieldFlags Flags;

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
            o.ExclusionZones = ExclusionZones.Select(x => x.Clone(newZones)).ToList();
            o.DynamicAsteroids = DynamicAsteroids.CloneCopy();
            o.LootZones = LootZones.Select(x => x.Clone(newZones)).ToList();
            return o;
        }

        // ReSharper disable CompareOfFloatsByEqualityOperator
        public bool DataEquals(AsteroidField other) =>
            DataEquality.ListEquals(TexturePanels, other.TexturePanels) &&
            DataEquality.IdEquals(Zone?.Nickname, other.Zone?.Nickname) &&
            DiffuseColor == other.DiffuseColor &&
            AmbientColor == other.AmbientColor &&
            AmbientIncrease == other.AmbientIncrease &&
            FillDist == other.FillDist &&
            EmptyCubeFrequency == other.EmptyCubeFrequency &&
            CubeSize == other.CubeSize &&
            CubeRotation == other.CubeRotation &&
            DataEquality.ListEquals(Cube, other.Cube) &&
            BillboardCount == other.BillboardCount &&
            BillboardDistance == other.BillboardDistance &&
            BillboardFadePercentage == other.BillboardFadePercentage &&
            DataEquality.IdEquals(BillboardShape, other.BillboardShape) &&
            BillboardSize == other.BillboardSize &&
            BillboardTint == other.BillboardTint &&
            DataEquality.ObjectEquals(Band, other.Band) &&
            DataEquality.ListEquals(DynamicAsteroids, other.DynamicAsteroids) &&
            DataEquality.ListEquals(ExclusionZones, other.ExclusionZones) &&
            DataEquality.ObjectEquals(FieldLoot, other.FieldLoot) &&
            DataEquality.ListEquals(LootZones, other.LootZones) &&
            Flags == other.Flags;
    }
}

