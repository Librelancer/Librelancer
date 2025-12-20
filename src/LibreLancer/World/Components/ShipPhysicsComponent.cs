// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Server.Components;

namespace LibreLancer.World.Components
{
	[Flags]
	public enum StrafeControls
	{
		None = 0,
		Left = 1,
		Right = 2,
		Up = 4,
		Down = 8
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

        public Ship Ship;
        public float EnginePower = 0f; //from 0 to 1
                                       //TODO: I forget how this is configured in .ini files. Constants.ini?
                                       //Some mods have a per-ship (engine?) cruise speed. Check how this is implemented, and include as native feature.
        public bool ThrustEnabled = false;
        public bool CruiseEnabled = false;
        public EngineStates EngineState = EngineStates.Standard;
        public StrafeControls CurrentStrafe = StrafeControls.None;
        public float ChargePercent;
        public Vector3 Steering;
        public float CruiseAccelPct = 0;

        public ShipPhysicsComponent(GameObject parent) : base(parent)
        {
            Active = true;
        }

        //TODO: Engine Kill

        public void ResyncChargePercent(float prev, float time)
        {
            if (EngineState == EngineStates.Cruise || EngineState == EngineStates.CruiseCharging)
            {
                var engine = Parent.GetComponent<SEngineComponent>(); //Get mounted engine
                ChargePercent = prev + (1.0f / engine.Engine.Def.CruiseChargeTime) * (float) time;
                if (ChargePercent >= 1) {
                    ChargePercent = 1;
                    EngineState = EngineStates.Cruise;
                }
                else {
                    EngineState = EngineStates.CruiseCharging;
                }
            }
        }

        public void ResyncCruiseAccel(float prev, float time)
        {
            if (EngineState == EngineStates.Cruise)
            {
                var engine = Parent.GetComponent<SEngineComponent>(); //Get mounted engine
                CruiseAccelPct = prev + (float)(time * 1.0f / engine.Engine.CruiseAccelTime);
                if (CruiseAccelPct > 1.0f) CruiseAccelPct = 1.0f;
            }
        }

        public override void Update(double time)
        {
            if (!Active) return;
            if (CruiseEnabled)
            {
                if (EngineState != EngineStates.Cruise &&
                    EngineState != EngineStates.CruiseCharging)
                {
                    EngineState = EngineStates.CruiseCharging;
                    ChargePercent = 0f;
                    CruiseAccelPct = 0f;
                }
            }
            else
            {
                EngineState = EngineStates.Standard;
                ChargePercent = 0;
                CruiseAccelPct = 0;
            }
            //Component checks
            var engine = Parent.GetComponent<SEngineComponent>(); //Get mounted engine
            var power = Parent.GetComponent<PowerCoreComponent>();
            if (Parent.PhysicsComponent == null) return;
            if (Parent.PhysicsComponent.Body == null) return;
            if (engine == null) return;
            if (power == null) return;
            //Drag = -linearDrag * Velocity
            if (EnginePower <= 0) {
                EnginePower = MathHelper.Clamp(EnginePower, -engine.Engine.Def.ReverseFraction, 1);
            }
            var drag = -engine.Engine.Def.LinearDrag * Parent.PhysicsComponent.Body.LinearVelocity;
            if (EngineState == EngineStates.CruiseCharging) {
                EnginePower = 1f;
            }
            var engine_force = EnginePower * engine.Engine.Def.MaxForce;
            power.CurrentThrustCapacity += power.Equip.ThrustChargeRate * (float)(time);
            power.CurrentThrustCapacity = MathHelper.Clamp(power.CurrentThrustCapacity, 0, power.Equip.ThrustCapacity);
            foreach (var thruster in Parent.GetChildComponents<ThrusterComponent>())
            {
                thruster.Enabled = false;
            }
            if (ThrustEnabled)
            {
                foreach (var thruster in Parent.GetChildComponents<ThrusterComponent>())
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
                    ChargePercent = 1f;
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
                CruiseAccelPct += (float)(time * 1.0f / engine.Engine.CruiseAccelTime);
                if (CruiseAccelPct > 1.0f) CruiseAccelPct = 1.0f;
                var cruise_force = engine.Engine.CruiseSpeed * engine.Engine.Def.LinearDrag;
                engine_force = engine.Engine.Def.MaxForce + (cruise_force - engine.Engine.Def.MaxForce) * CruiseAccelPct;
                //Set fx sparam. TODO: This is poorly named
                engine.Speed = 1.0f;
                ChargePercent = 1f;
            }
            else
            {
                engine.Speed = Math.Clamp(EnginePower * 0.9f, 0, 1);
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
