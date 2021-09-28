// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Equipment;

namespace LibreLancer.GameData
{
	public class Ship
	{
		public string Nickname;
        public int NameIds;
        public int Infocard;
        public uint CRC;
		public ResolvedModel ModelFile;
		public Vector3 SteeringTorque;
		public Vector3 AngularDrag;
		public Vector3 RotationInertia;
		public float Mass;
		public float StrafeForce;
		public float CruiseSpeed;
        public float Hitpoints;

        public Vector3 ChaseOffset;
        public float CameraHorizontalTurnAngle;
        public float CameraVerticalTurnUpAngle;
        public float CameraVerticalTurnDownAngle;

        public List<DamageFuse> Fuses = new List<DamageFuse>();

        public Dictionary<string, List<HpType>> HardpointTypes = new (StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> PossibleHardpoints = new (StringComparer.OrdinalIgnoreCase); 
        public Ship ()
		{
		}

        public override string ToString() => Nickname;
    }
}

