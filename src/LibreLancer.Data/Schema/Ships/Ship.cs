// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Solar;

namespace LibreLancer.Data.Schema.Ships
{
    public enum ShipType
    {
        Fighter,
        Freighter,
        Gunboat,
        Cruiser,
        Transport,
        Capital,
        Mining,
    }

    [ParsedSection]
	public partial class Ship
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
		public ShipType Type;
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

        public List<ObjectFuse> Fuses = new List<ObjectFuse>();

        public List<ShipHpDef> HardpointTypes = new List<ShipHpDef>();

        public List<SurfaceHitEffects> SurfaceHitEffects = new List<SurfaceHitEffects>();

        public ShieldLink ShieldLink;

        [Section("collisiongroup", Child = true)]
        public List<CollisionGroup> CollisionGroups = new List<CollisionGroup>();

        [EntryHandler("fuse", Multiline = true, MinComponents = 3)]
        void HandleFuse(Entry e) => Fuses.Add(new ObjectFuse(e));

        [EntryHandler("hp_type", Multiline = true, MinComponents = 2)]
        void HandleHpType(Entry e) => HardpointTypes.Add(new ShipHpDef(e));

        [EntryHandler("surface_hit_effects", Multiline = true, MinComponents = 2)]
        void HandleSurface(Entry e) => SurfaceHitEffects.Add(new SurfaceHitEffects(e));

        [EntryHandler("shield_link", MinComponents = 3)]
        void HandleShieldLink(Entry e) => ShieldLink = new ShieldLink(e);
    }

    public class ShieldLink
    {
        public string Name;
        public string HardpointMount;
        public string HardpointShield;

        public ShieldLink()
        {
        }

        public ShieldLink(Entry e)
        {
            Name = e[0].ToString();
            HardpointMount = e[1].ToString();
            HardpointShield = e[2].ToString();
        }
    }

    public class SurfaceHitEffects
    {
        public float Threshold;
        public string[] Effects;

        public SurfaceHitEffects()
        {
        }
        public SurfaceHitEffects(Entry e)
        {
            Threshold = e[0].ToSingle();
            Effects = e.Skip(1).Select(x => x.ToString()).ToArray();
        }
    }
}
