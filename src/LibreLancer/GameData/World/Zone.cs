﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Universe;

namespace LibreLancer.GameData.World
{
	public class Zone
	{
		public string Nickname;
        public int IdsName;
        public int[] IdsInfo = Array.Empty<int>();
		public Vector3 Position;
		public Matrix4x4 RotationMatrix;
		public Vector3 RotationAngles;
		public ZoneShape Shape;
		public float EdgeFraction;
        public ZonePropFlags PropertyFlags;
        public Color4? PropertyFogColor;
        public float Sort;
        public VisitFlags VisitFlags;
        
        //Properties not yet used in game, but copied from ini for round trip support
        public string[] PopType;
        public string Music;
        public string Spacedust;
        public int SpacedustMaxParticles;
        public float Interference;
        public float PowerModifier;
        public float DragModifier;
        public string Comment;
        public int LaneId;
        public int TradelaneAttack;
        public int TradelaneDown;
        public float Damage;
        public int Toughness;
        public int Density;
        public bool? PopulationAdditive;
        public bool MissionEligible;
        public int MaxBattleSize;
        public int ReliefTime;
        public int RepopTime;
        
        //Encounter parameters
        public string[] AttackIds;
        public string[] MissionType;
        public string[] PathLabel;
        public string[] Usage;
        public string VignetteType;
        public Encounter[] Encounters;
        public DensityRestriction[] DensityRestrictions;
        
		public Zone ()
		{
		}

        public void CopyTo(Zone other)
        {
            other.Nickname = Nickname;
            other.IdsName = IdsName;
            other.IdsInfo = IdsInfo.ShallowCopy();
            other.Position = Position;
            other.RotationMatrix = RotationMatrix;
            other.RotationAngles = RotationAngles;
            other.Shape = Shape?.Clone(other);
            other.EdgeFraction = EdgeFraction;
            other.PropertyFlags = PropertyFlags;
            other.PropertyFogColor = PropertyFogColor;
            other.Sort = Sort;
            other.VisitFlags = VisitFlags;
            other.PopType = PopType.ShallowCopy();
            other.Music = Music;
            other.Spacedust = Spacedust;
            other.SpacedustMaxParticles = SpacedustMaxParticles;
            other.Interference = Interference;
            other.PowerModifier = PowerModifier;
            other.DragModifier = DragModifier;
            other.Comment = Comment;
            other.LaneId = LaneId;
            other.TradelaneAttack = TradelaneAttack;
            other.TradelaneDown = TradelaneDown;
            other.Damage = Damage;
            other.Toughness = Toughness;
            other.Density = Density;
            other.PopulationAdditive = PopulationAdditive;
            other.MissionEligible = MissionEligible;
            other.MaxBattleSize = MaxBattleSize;
            other.ReliefTime = ReliefTime;
            other.RepopTime = RepopTime;
            other.AttackIds = AttackIds.ShallowCopy();
            other.MissionType = MissionType.ShallowCopy();
            other.PathLabel = PathLabel.ShallowCopy();
            other.Usage = Usage.ShallowCopy();
            other.VignetteType = VignetteType;
            other.Encounters = Encounters.CloneCopy();
            other.DensityRestrictions = DensityRestrictions.ShallowCopy();
        }

        public Zone Clone()
        {
            var z = new Zone();
            CopyTo(z);
            return z;
        }
    }

    [Flags]
    public enum ZonePropFlags
    {
        None = 0,
        ObjDensityLow = 1,
        ObjDensityMed = 2,
        ObjDensityHigh = 4,
        DangerLow = 8,
        DangerMed = 16,
        DangerHigh = 32,
        Rock = 64,
        Debris = 128,
        Ice = 256,
        Lava = 512,
        Nomad = 1024,
        Crystal = 2048,
        Mines = 4096,
        Badlands = 8192,
        GasPockets = 16384,
        Cloud = 32768,
        Exclusion1 = 65536,
        Exclusion2 = 131072,
        Damage = 0x040000,
        DragModifier = 0x080000,
        Interference  = 0x100000,
        Spacedust = 0x200000,
        Music = 0x200000
    }
}

