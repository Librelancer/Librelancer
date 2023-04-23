// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Text;
using LibreLancer.Data;

namespace LibreLancer.GameData.World
{
	public class Zone
	{
		public string Nickname;
		public Vector3 Position;
		public Matrix4x4 RotationMatrix;
		public Vector3 RotationAngles;
		public ZoneShape Shape;
		public float EdgeFraction;
        public ZonePropFlags PropertyFlags;
        public Color4 PropertyFogColor;
        public float Sort;
        public int VisitFlags;
        
        //Properties not yet used in game, but copied from ini for round trip support
        public string Music;
        public string Spacedust;
        public int SpacedustMaxParticles;
        public float Interference;
        public float PowerModifier;
        public float DragModifier;
        public string[] Comment;
        public int LaneId;
        public int TradelaneAttack;
        public int TradelaneDown;
        public float Damage;
        public int Toughness;
        public int Density;
        public bool PopulationAdditive;
        public bool MissionEligible;
        public int MaxBattleSize;
        public int ReliefTime;
        public int RepopTime;

        
		public Zone ()
		{
		}

        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.AppendSection("Zone");
            sb.AppendEntry("nickname", Nickname);
            if(Comment != null)
                foreach(var c in Comment)
                    sb.AppendEntry("comment", c);
            sb.AppendEntry("pos", Position);
            var rot = RotationMatrix.GetEulerDegrees();
            var ln = rot.Length();
            if(!float.IsNaN(ln) && ln > 0)
                sb.AppendEntry("rotate", new Vector3(rot.Y, rot.X, rot.Z));
            sb.Append(Shape.Serialize());
            sb.AppendEntry("property_flags", (uint) PropertyFlags, false);
            sb.AppendEntry("tradelane_attack", TradelaneAttack, false);
            sb.AppendEntry("sort", Sort);
            sb.AppendEntry("Music", Music);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (EdgeFraction != 0.25f)
                sb.AppendEntry("edge_fraction", EdgeFraction);
            sb.AppendEntry("spacedust", Spacedust);
            sb.AppendEntry("spacedust_maxparticles", SpacedustMaxParticles, false);
            sb.AppendEntry("toughness", Toughness, false);
            sb.AppendEntry("density", Density, false);
            sb.AppendEntry("repop_time", RepopTime, false);
            sb.AppendEntry("max_battle_size", MaxBattleSize, false);
            //pop_type
            sb.AppendEntry("relief_time", ReliefTime, false);
            //path_label
            //usage
            if (MissionEligible)
                sb.AppendLine("mission_eligible = true");
            //density_restriction
            //encounter
            //faction
            sb.AppendEntry("powermodifier", PowerModifier, false);
            sb.AppendEntry("dragmodifier", DragModifier, false);
            sb.AppendEntry("damage", Damage, false);
            sb.AppendEntry("interference", Interference, false);
            sb.AppendEntry("lane_id", LaneId, false);
            sb.AppendEntry("tradelane_down", TradelaneDown, false);
         
            return sb.ToString();
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

