// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Ships
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
        [Entry("hit_pts")]
		public int Hitpoints;
        [Entry("nanobot_limit")]
		public int NanobotLimit;
        [Entry("shield_battery_limit")]
		public int ShieldBatteryLimit;
        [Entry("hold_size")]
		public int HoldSize;
        [Entry("mass")]
		public int Mass;
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

        bool HandleEntry(Entry e)
        {
            switch(e.Name.ToLowerInvariant())
            {
                case "hp_type":
                    return true;
            }
            return false;
        }
	}
}