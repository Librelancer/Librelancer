// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
	[Flags]
	public enum StrafeControls
	{
		None = 0,
		Left = 2,
		Right = 4,
		Up = 8,
		Down = 16
	}
	public enum EngineStates
	{
		Standard,
		CruiseCharging,
		Cruise,
		EngineKill
	}
    public class ShipPhysicsComponent : GameComponent
    {
        public bool Active { get; set; }

        public GameData.Ship Ship;
        public float EnginePower = 0f; //from 0 to 1
                                       //TODO: I forget how this is configured in .ini files. Constants.ini?
                                       //Some mods have a per-ship (engine?) cruise speed. Check how this is implemented, and include as native feature.
        public bool ThrustEnabled = false;
        public EngineStates EngineState = EngineStates.Standard;
        public StrafeControls CurrentStrafe = StrafeControls.None;
        public float Pitch; //From -1 to 1
        public float Yaw; //From -1 to 1
        public float Roll; //From -1 to 1
                           //I know it's hacky :(
        public float PlayerPitch;
        public float PlayerYaw;
        PIDController rollPID = new PIDController() { P = 2 };

        public ShipPhysicsComponent(GameObject parent) : base(parent)
        {
            Active = true;
        }
        //TODO: Engine Kill
        public override void FixedUpdate(TimeSpan time)
        {
            if (!Active) return;
            //Component checks
            var engine = Parent.GetComponent<CEngineComponent>(); //Get mounted engine
            var power = Parent.GetComponent<PowerCoreComponent>();
            if (Parent.PhysicsComponent == null) return;
            if (Parent.PhysicsComponent.Body == null) return;
            if (engine == null) return;
            if (power == null) return;
            //Drag = -linearDrag * Velocity
            var drag = -engine.Engine.Def.LinearDrag * Parent.PhysicsComponent.Body.LinearVelocity;
            var engine_force = EnginePower * engine.Engine.Def.MaxForce;
            power.CurrentThrustCapacity += power.Equip.ThrustChargeRate * (float)(time.TotalSeconds);
            power.CurrentThrustCapacity = MathHelper.Clamp(power.CurrentThrustCapacity, 0, power.Equip.ThrustCapacity);
            foreach (var thruster in Parent.GetChildComponents<CThrusterComponent>())
            {
                thruster.Enabled = false;
            }
            if (ThrustEnabled)
            {
                foreach (var thruster in Parent.GetChildComponents<CThrusterComponent>())
                {
                    engine_force += thruster.Equip.Force;
                    thruster.Enabled = true;
                    power.CurrentThrustCapacity -= (float)(thruster.Equip.Drain * time.TotalSeconds);
                    power.CurrentThrustCapacity = MathHelper.Clamp(power.CurrentThrustCapacity, 0, power.Equip.ThrustCapacity);
                    if (power.CurrentThrustCapacity == 0) ThrustEnabled = false;
                }
            }
            if (EngineState == EngineStates.Cruise)
            { //Cruise has entirely different force calculation
                engine_force = Ship.CruiseSpeed * engine.Engine.Def.LinearDrag;
                //Set fx sparam. TODO: This is poorly named
                engine.Speed = 1.0f;
            }
            else
            {
                engine.Speed = EnginePower * 0.9f;
            }

            Vector3 strafe = Vector3.Zero;
            //TODO: Trying to strafe during cruise should drop you out
            if (EngineState != EngineStates.Cruise) //Cannot strafe during cruise
            {
                if ((CurrentStrafe & StrafeControls.Left) == StrafeControls.Left)
                {
                    strafe -= Vector3.UnitX; 
                }
                else if ((CurrentStrafe & StrafeControls.Right) == StrafeControls.Right)
                {
                    strafe += Vector3.UnitX; 
                }
                if ((CurrentStrafe & StrafeControls.Up) == StrafeControls.Up)
                {
                    strafe += Vector3.UnitY;
                }
                else if ((CurrentStrafe & StrafeControls.Down) == StrafeControls.Down)
                {
                    strafe -= Vector3.UnitY;
                }
                if (strafe != Vector3.Zero)
                {
                    strafe.Normalize();
                    strafe = Parent.PhysicsComponent.Body.RotateVector(strafe);
                    //Apply strafe force
                    strafe *= Ship.StrafeForce;
                }
            }
            var totalForce = (
                drag +
                strafe +
                (Parent.PhysicsComponent.Body.RotateVector(-Vector3.UnitZ) * engine_force)
            );
            Parent.PhysicsComponent.Body.AddForce(totalForce);
            //steer
            //based on the amazing work of Why485 (https://www.youtube.com/user/Why485)
            var steerControl = new Vector3(Math.Abs(PlayerPitch) > 0 ? PlayerPitch : Pitch,
                                           Math.Abs(PlayerYaw) > 0 ? PlayerYaw : Yaw,
                                           Roll);
            double pitch, yaw, roll;
            DecomposeOrientation(Parent.PhysicsComponent.Body.Transform, out pitch, out yaw, out roll);
            if (Math.Abs(PlayerPitch) < float.Epsilon && Math.Abs(PlayerYaw) < float.Epsilon)
                steerControl.Z = MathHelper.Clamp((float)rollPID.Update(0, roll, (float)time.TotalSeconds), -0.5f, 0.5f);
            else
                rollPID.Reset();

            var angularForce = Parent.PhysicsComponent.Body.RotateVector(steerControl * Ship.SteeringTorque);
            angularForce += (Parent.PhysicsComponent.Body.AngularVelocity * -1) * Ship.AngularDrag;
            //transform torque by direction = unity's AddRelativeTorque
            Parent.PhysicsComponent.Body.AddTorque(angularForce);
        }

        //Specific decomposition for roll
        static void DecomposeOrientation(Matrix4x4 mx, out double xPitch, out double yYaw, out double zRoll)
        {
            xPitch = Math.Asin(-mx.M32);
            double threshold = 0.001; // Hardcoded constant – burn him, he’s a witch
            double test = Math.Cos(xPitch);

            if (test > threshold)
            {
                zRoll = Math.Atan2(mx.M12, mx.M22);
                yYaw = Math.Atan2(mx.M31, mx.M33);
            }
            else
            {
                zRoll = Math.Atan2(-mx.M21, mx.M11);
                yYaw = 0.0;
            }
        }
    }
}
