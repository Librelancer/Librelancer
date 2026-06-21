// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Physics;
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
        EngineKill,
		CruiseCharging,
		Cruise,
	}
    public class ShipPhysicsComponent : ShipControlAccessComponent
    {
        public bool Active { get; set; }

        public Ship Ship;
        public bool ThrustEnabled = false;
        private bool cruiseEnabled = false;
        private bool previousCruiseEnabled = false;
        public override bool CruiseEnabled
        {
            get => cruiseEnabled;
            set => cruiseEnabled = value;
        }
        public float CruiseSpeedOffset = 0;
        public bool EngineKillEnabled = false;
        public float ChargePercent;
        public float CruiseAccelPct = 0;

        public ShipPhysicsComponent(GameObject parent, Ship ship) : base(parent)
        {
            Active = true;
            Ship = ship;
        }

        internal static float CruiseChargeEnginePower(float enginePower, bool formationFollower,
            bool restrictedCruiseSpeed = false) =>
            formationFollower || restrictedCruiseSpeed ? MathHelper.Clamp(enginePower, 0, 1) : 1f;

        internal static float CruiseEngineForce(float cruiseSpeed, float normalCruiseSpeed, float maxForce,
            float linearDrag, float accelerationPercent)
        {
            var initialForceFactor = cruiseSpeed < normalCruiseSpeed && normalCruiseSpeed > 0
                ? MathHelper.Clamp(cruiseSpeed / normalCruiseSpeed, 0, 1)
                : 1;
            var initialForce = maxForce * initialForceFactor;
            var targetForce = cruiseSpeed * linearDrag;
            return initialForce + (targetForce - initialForce) * accelerationPercent;
        }

        public override void SetEngineState(EngineStates es)
        {
            throw new InvalidOperationException("Cannot force EngineState on sim object");
        }

        // TODO: Engine Kill

        private void StartCruiseCharge()
        {
            EngineState = EngineStates.CruiseCharging;
            ChargePercent = 0f;
            CruiseAccelPct = 0f;
        }

        public void StopCruise()
        {
            cruiseEnabled = false;
            previousCruiseEnabled = false;
            CruiseSpeedOffset = 0f;
            EngineState = EngineStates.Standard;
            ChargePercent = 0f;
            CruiseAccelPct = 0f;
        }

        public void SetCruiseState(float chargePercent, float accelPercent)
        {
            chargePercent = MathHelper.Clamp(chargePercent, 0f, 1f);
            accelPercent = MathHelper.Clamp(accelPercent, 0f, 1f);

            if (chargePercent <= 0f && accelPercent <= 0f)
            {
                StopCruise();
                return;
            }

            cruiseEnabled = true;
            previousCruiseEnabled = true;
            ChargePercent = chargePercent;
            CruiseAccelPct = accelPercent;
            EngineState = chargePercent >= 1f ? EngineStates.Cruise : EngineStates.CruiseCharging;
        }

        public void ResyncChargePercent(float prev, float time)
        {
            if (prev <= 0f && EngineState is not (EngineStates.Cruise or EngineStates.CruiseCharging))
            {
                return;
            }

            SetCruiseState(prev, CruiseAccelPct);
            var engine = Parent.GetComponent<SEngineComponent>()!; // Get mounted engine
            ChargePercent = prev + (1.0f / engine.Engine.Def.CruiseChargeTime) * (float) time;
            if (ChargePercent >= 1) {
                ChargePercent = 1;
                EngineState = EngineStates.Cruise;
            }
            else {
                EngineState = EngineStates.CruiseCharging;
            }
        }

        public void ResyncCruiseAccel(float prev, float time)
        {
            if (prev > 0f && EngineState != EngineStates.Cruise)
            {
                SetCruiseState(1f, prev);
            }

            if (EngineState == EngineStates.Cruise)
            {
                var engine = Parent.GetComponent<SEngineComponent>()!; // Get mounted engine
                CruiseAccelPct = prev + (float)(time * 1.0f / engine.Engine.CruiseAccelTime);
                if (CruiseAccelPct > 1.0f) CruiseAccelPct = 1.0f;
            }
        }

        public override void Update(double time, GameWorld world)
        {
            if (!Active) return;
            if (CruiseEnabled)
            {
                if (!previousCruiseEnabled ||
                    (EngineState != EngineStates.Cruise &&
                    EngineState != EngineStates.CruiseCharging)
                   )
                {
                    StartCruiseCharge();
                }
            }
            else if (EngineKillEnabled)
            {
                EngineState = EngineStates.EngineKill;
                ChargePercent = 0;
                CruiseAccelPct = 0;
            }
            else
            {
                EngineState = EngineStates.Standard;
                ChargePercent = 0;
                CruiseAccelPct = 0;
            }
            previousCruiseEnabled = CruiseEnabled;
            // Component checks
            var engine = Parent.GetComponent<SEngineComponent>(); // Get mounted engine
            var power = Parent.GetComponent<PowerCoreComponent>();
            if (Parent.PhysicsComponent == null) return;

            if ((PhysicsObject?)Parent.PhysicsComponent.Body == null)
            {
                return;
            }

            if (engine == null) return;
            if (power == null) return;
            // Drag = -linearDrag * Velocity

            float requestedEnginePower = EnginePower;
            if (requestedEnginePower <= 0)
            {
                requestedEnginePower = MathHelper.Clamp(EnginePower, -engine.Engine.Def.ReverseFraction, 1);
            }

            float totalDrag = Ship.LinearDrag;
            float thrusterForce = 0;

            power.CurrentThrustCapacity += power.Equip.ThrustChargeRate * (float)(time);
            power.CurrentThrustCapacity = MathHelper.Clamp(power.CurrentThrustCapacity, 0, power.Equip.ThrustCapacity);
            foreach (var thruster in Parent.GetChildComponents<ThrusterComponent>())
            {
                thruster.Enabled = false;
            }
            if (ThrustEnabled && EngineState <= EngineStates.EngineKill)
            {
                foreach (var thruster in Parent.GetChildComponents<ThrusterComponent>())
                {
                    thrusterForce += thruster.Equip.Force;
                    thruster.Enabled = true;
                    power.CurrentThrustCapacity -= (float)(thruster.Equip.Drain * time);
                    power.CurrentThrustCapacity = MathHelper.Clamp(power.CurrentThrustCapacity, 0, power.Equip.ThrustCapacity);
                    if (power.CurrentThrustCapacity == 0) ThrustEnabled = false;
                }
            }

            if (EngineState == EngineStates.EngineKill)
            {
                if (thrusterForce > 0 || CurrentStrafe != StrafeControls.None ||
                    requestedEnginePower < 0)
                {
                    requestedEnginePower = requestedEnginePower < 0 ? requestedEnginePower : 1;
                    totalDrag += engine.Engine.Def.LinearDrag;
                    engine.EngineKill = false;
                }
                else
                {
                    requestedEnginePower = 0;
                    engine.EngineKill = true;
                }
            }
            else
            {
                totalDrag += engine.Engine.Def.LinearDrag;
                engine.EngineKill = false;
            }
            var drag = -totalDrag * Parent.PhysicsComponent.Body.LinearVelocity;
            if (EngineState == EngineStates.CruiseCharging)
            {
                var formationFollower = Parent.Formation != null && Parent.Formation.LeadShip != Parent;
                requestedEnginePower = EnginePower = CruiseChargeEnginePower(EnginePower, formationFollower,
                    CruiseSpeedOffset < 0);
            }

            var engineForce = requestedEnginePower * engine.Engine.Def.MaxForce
                              + thrusterForce;


            if (EngineState == EngineStates.CruiseCharging) {
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
            { // Cruise has entirely different force calculation
                CruiseAccelPct += (float)(time * 1.0f / engine.Engine.CruiseAccelTime);
                if (CruiseAccelPct > 1.0f) CruiseAccelPct = 1.0f;
                var cruiseSpeed = MathF.Max(0, engine.Engine.CruiseSpeed + CruiseSpeedOffset);
                engineForce = CruiseEngineForce(cruiseSpeed, engine.Engine.CruiseSpeed,
                    engine.Engine.Def.MaxForce, engine.Engine.Def.LinearDrag, CruiseAccelPct);
                // Set fx sparam. TODO: This is poorly named
                engine.Speed = 1.0f;
                ChargePercent = 1f;
            }
            else
            {
                engine.Speed = MathHelper.Clamp(requestedEnginePower, 0, 1) * 0.9f;
            }

            Vector3 strafe = Vector3.Zero;
            var strafeControl = StrafeControlsToVector(CurrentStrafe);
            if (strafeControl.LengthSquared() > 1f)
            {
                strafeControl = Vector2.Normalize(strafeControl);
            }

            if (strafeControl != Vector2.Zero)
            {
                strafe = new Vector3(strafeControl.X, strafeControl.Y, 0);
                strafe = Parent.PhysicsComponent.Body.RotateVector(strafe);
                // Apply strafe force
                strafe *= Ship.StrafeForce;
            }
            var totalForce = (
                drag +
                strafe +
                (Parent.PhysicsComponent.Body.RotateVector(-Vector3.UnitZ) * engineForce)
            );
            var angularForce = Steering * Ship.SteeringTorque;
            angularForce += (Parent.PhysicsComponent.Body.AngularVelocity * -1) * Ship.AngularDrag;
            // Add forces
            Parent.PhysicsComponent.Body.AddForce(totalForce);
            Parent.PhysicsComponent.Body.AddTorque(angularForce);
        }

        public static Vector2 StrafeControlsToVector(StrafeControls controls)
        {
            var strafe = Vector2.Zero;
            if ((controls & StrafeControls.Left) == StrafeControls.Left)
            {
                strafe.X -= 1;
            }
            else if ((controls & StrafeControls.Right) == StrafeControls.Right)
            {
                strafe.X += 1;
            }
            if ((controls & StrafeControls.Up) == StrafeControls.Up)
            {
                strafe.Y += 1;
            }
            else if ((controls & StrafeControls.Down) == StrafeControls.Down)
            {
                strafe.Y -= 1;
            }
            return strafe;
        }

    }
}
