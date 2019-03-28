// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Ships
{
	public class Ship
	{
        [Entry("ids_name")]
		public int IdsName;
        [Entry("ids_info")]
		public int IdsInfo;
        [Entry("ids_info1")]
        public int IdsInfo1 = -1;
        [Entry("ids_info2")]
        public int IdsInfo2 = -1;
        [Entry("ids_info3")]
        public int IdsInfo3 = -1;
        [Entry("nickname")]
		public string Nickname;
        [Entry("da_archetype")]
		public string DaArchetypeName;
        [Entry("material_library", Multiline = true)]
		public List<string> MaterialLibraries = new List<string>();
        [Entry("envmap_material")]
        public string EnvmapMaterial;
        [Entry("explosion_arch")]
        public string ExplosionArch;
        [Entry("msg_id_prefix")]
        public string MsgIdPrefix;
        [Entry("nudge_force")]
        public float NudgeForce;
        [Entry("hit_pts")]
		public int Hitpoints;
        [Entry("nanobot_limit")]
		public int NanobotLimit;
        [Entry("shield_battery_limit")]
		public int ShieldBatteryLimit;
        [Entry("hold_size")]
		public int HoldSize;
        [Entry("mass")]
		public float Mass;
        [Entry("ship_class")]
		public int ShipClass;
        [Entry("type")]
		public string Type;
        [Entry("steering_torque")]
		public Vector3 SteeringTorque;
        [Entry("angular_drag")]
		public Vector3 AngularDrag;
        [Entry("rotation_inertia")]
		public Vector3 RotationInertia;
        [Entry("strafe_force")]
		public float StrafeForce;
        [Entry("strafe_power_usage")]
        public float StrafePowerUsage;
        [Entry("max_bank_angle")]
        public float MaxBankAngle;
        [Entry("bay_door_anim")]
        public string BayDoorAnim;
        [Entry("bay_doors_open_snd")]
        public string BayDoorsOpenSound;
        [Entry("bay_doors_close_snd")]
        public string BayDoorsCloseSound;
        [Entry("mission_property")]
        public string MissionProperty;
        [Entry("linear_drag")]
        public float LinearDrag;
        [Entry("cockpit")]
        public string Cockpit;
        [Entry("pilot_mesh")]
        public string PilotMesh;
        [Entry("camera_offset")]
		Vector2 _cameraOffset;
        public Vector3 CameraOffset
        {
            get { return new Vector3(0, _cameraOffset.X, _cameraOffset.Y); }
        }

        [Entry("camera_angular_acceleration")]
		public float CameraAngularAcceleration;
        [Entry("camera_horizontal_turn_angle")]
		public float CameraHorizontalTurnAngle;
        [Entry("camera_vertical_turn_up_angle")]
		public float CameraVerticalTurnUpAngle;
        [Entry("camera_vertical_turn_down_angle")]
		public float CameraVerticalTurnDownAngle;
        [Entry("camera_turn_look_ahead_slerp_amount")]
		public float CameraTurnLookAheadSlerpAmount;

        [Entry("lodranges")]
        public float[] LodRanges;

        [Entry("hp_bay_surface")]
        public string HpBaySurface;
        [Entry("hp_bay_external")]
        public string HpBayExternal;
        [Entry("hp_tractor_source")]
        public string HpTractorSource;
        [Entry("num_exhaust_nozzles")]
        public int NumExhaustNozzles;

        [Entry("nomad")]
        public bool Nomad;

        [Entry("explosion_resistance")]
        public float ExplosionResistance;

        bool HandleEntry(Entry e)
        {
            switch(e.Name.ToLowerInvariant())
            {
                case "shield_link":
                case "fuse":
                case "surface_hit_effects":
                case "hp_type":
                    return true;
            }
            return false;
        }
	}
}