// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.GameData
{
	public class Ship
	{
		public string Nickname;
        public int NameIds;
        public int Infocard;
        public uint CRC;
		public IDrawable Drawable;
		public Vector3 SteeringTorque;
		public Vector3 AngularDrag;
		public Vector3 RotationInertia;
		public float Mass;
		public float StrafeForce;
		public float CruiseSpeed;

        public Vector3 ChaseOffset;
        public float CameraHorizontalTurnAngle;
        public float CameraVerticalTurnUpAngle;
        public float CameraVerticalTurnDownAngle;

        internal Action LoadResAction;
        public void LoadResources()
        {
            if (LoadResAction != null)
            {
                LoadResAction();
                LoadResAction = null;
            }
        }

        public Ship ()
		{
		}
	}
}

