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
        public float ChargePercent;
        public Vector3 Steering;
        public int Tick;

        float cruiseAccelPct = 0;

        public void CruiseToggle()
        {
            if (EngineState == EngineStates.Cruise ||
                EngineState == EngineStates.CruiseCharging)
            {
                EngineState = EngineStates.Standard;
            }
            else
            {
                BeginCruise();
            }
        }

        public void EndCruise()
        {
            EngineState = EngineStates.Standard;
        }

        public void BeginCruise()
        {
            if (EngineState != EngineStates.Cruise &&
                EngineState != EngineStates.CruiseCharging)
            {
                EngineState = EngineStates.CruiseCharging;
                ChargePercent = 0f;
                cruiseAccelPct = 0f;
            }
        }

        public ShipPhysicsComponent(GameObject parent) : base(parent)
        {
            Active = true;
        }
        
        //TODO: Engine Kill
        public override void Update(double time)
        {
            if (!Active) return;
            //Component checks
            var engine = Parent.GetComponent<SEngineComponent>(); //Get mounted engine
            var power = Parent.GetComponent<PowerCoreComponent>();
            if (Parent.PhysicsComponent == null) return;
            if (Parent.PhysicsComponent.Body == null) return;
            if (engine == null) return;
            if (power == null) return;
            //Drag = -linearDrag * Velocity
            var drag = -engine.Engine.Def.LinearDrag * Parent.PhysicsComponent.Body.LinearVelocity;
            if (EngineState == EngineStates.CruiseCharging) {
                EnginePower = 1f;
            }
            var engine_force = EnginePower * engine.Engine.Def.MaxForce;
            power.CurrentThrustCapacity += power.Equip.ThrustChargeRate * (float)(time);
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
                    power.CurrentThrustCapacity -= (float)(thruster.Equip.Drain * time);
                    power.CurrentThrustCapacity = MathHelper.Clamp(power.CurrentThrustCapacity, 0, power.Equip.ThrustCapacity);
                    if (power.CurrentThrustCapacity == 0) ThrustEnabled = false;
                }
            }

            if (EngineState == EngineStates.CruiseCharging) {
                EnginePower = 1f;
                ChargePercent += (1.0f / engine.Engine.Def.CruiseChargeTime) * (float)time;
                if (ChargePercent >= 1.0f)
                {
                    EngineState = EngineStates.Cruise;
                }

                if (ChargePercent >= 0.6f) {
                    var fxPct = (ChargePercent - 0.6f) / 0.4f * 0.1f;
                    engine.Speed = 0.9f + fxPct;
                }
                else {
                    engine.Speed = 0.901f;
                }
            }
            else if (EngineState == EngineStates.Cruise)
            { //Cruise has entirely different force calculation
                cruiseAccelPct += (float)(time * 1.0f / engine.Engine.CruiseAccelTime);
                if (cruiseAccelPct > 1.0f) cruiseAccelPct = 1.0f;
                var cruise_force = engine.Engine.CruiseSpeed * engine.Engine.Def.LinearDrag;
                engine_force = engine.Engine.Def.MaxForce + (cruise_force - engine.Engine.Def.MaxForce) * cruiseAccelPct;
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
            var angularForce = Steering * Ship.SteeringTorque;
            angularForce += (Parent.PhysicsComponent.Body.AngularVelocity * -1) * Ship.AngularDrag;
            //Add forces
            Parent.PhysicsComponent.Body.AddForce(totalForce);
            Parent.PhysicsComponent.Body.AddTorque(angularForce);
        }

        
    }
}
