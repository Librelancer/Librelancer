// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Server.Components;

namespace LibreLancer.World.Components
{
    /// <summary>
    /// Take steering controls from autopilot or player and send to physics
    /// </summary>
    public class ShipSteeringComponent : GameComponent
    {
        public float InPitch;
        public float InYaw;
        public float InRoll;
        public float InThrottle;
        public bool Cruise;
        public float CruiseSpeedOffset;
        public bool Thrust;
        public StrafeControls CurrentStrafe;
        public int Tick;
        public bool EngineKill;

        public bool PlayerInput;

        private PIDController rollPID = new() { P = 2 };

        public ShipSteeringComponent(GameObject parent) : base(parent) { }

        public Vector3 OutputSteering;

        internal static float EscortSpeedFactor(bool isPlayer, ReadOnlySpan<float> memberFactors)
        {
            if (isPlayer)
                return 1;
            var factor = 1f;
            foreach (var memberFactor in memberFactors)
                factor = MathF.Min(factor, MathHelper.Clamp(memberFactor, 0, 1));
            return factor;
        }

        private static float IncludeEscortSpeedFactor(float current, GameObject ship)
        {
            if (ship.TryGetComponent<AutopilotComponent>(out var autopilot))
                return MathF.Min(current, autopilot.ReferenceSpeedFactor);
            return current;
        }

        private float GetEscortSpeedFactor()
        {
            if (Parent.Flags.HasFlag(GameObjectFlags.Player) || Parent.Formation == null)
                return 1;
            var factor = 1f;
            factor = IncludeEscortSpeedFactor(factor, Parent.Formation.LeadShip);
            foreach (var follower in Parent.Formation.Followers)
                factor = IncludeEscortSpeedFactor(factor, follower);
            return MathHelper.Clamp(factor, 0, 1);
        }

        public override void Update(double time, GameWorld world)
        {
            var physics = Parent.GetComponent<ShipPhysicsComponent>(); // Get mounted engine

            // Set output parameters
            Vector3 steerControl;
            if (!PlayerInput && Parent.TryGetComponent<AutopilotComponent>(out var autoPilot) &&
                autoPilot.CurrentBehavior != AutopilotBehaviors.None)
            {
                steerControl = new Vector3(autoPilot.OutPitch, autoPilot.OutYaw, 0);
            }
            else
            {
                steerControl = new Vector3(InPitch, InYaw, InRoll);
            }

            double pitch, yaw;
            DecomposeOrientation(Matrix4x4.CreateFromQuaternion(Parent.PhysicsComponent!.Body.Orientation), out pitch, out yaw, out var roll);

            if (Math.Abs(InPitch) < float.Epsilon && Math.Abs(InYaw) < float.Epsilon)
                steerControl.Z = MathHelper.Clamp((float) rollPID.Update(0, roll, (float) time), -0.5f, 0.5f);
            else
                rollPID.Reset();
            OutputSteering = Parent.PhysicsComponent.Body.RotateVector(steerControl);
            OutputSteering = MathHelper.ApplyEpsilon(OutputSteering);

            var strafe = CurrentStrafe;
            if (strafe == StrafeControls.None &&
                Parent.TryGetComponent<AutopilotComponent>(out autoPilot) &&
                autoPilot.CurrentBehavior != AutopilotBehaviors.None)
            {
                strafe = autoPilot.AutopilotStrafe;
            }

            physics!.Steering = OutputSteering;
            physics.CurrentStrafe = strafe;
            var escortSpeedFactor = GetEscortSpeedFactor();
            physics.EnginePower = InThrottle * escortSpeedFactor;
            physics.ThrustEnabled = Thrust;
            physics.CruiseEnabled = Cruise;
            physics.CruiseSpeedOffset = Cruise ? CruiseSpeedOffset : 0;
            if (Cruise && escortSpeedFactor < 1)
            {
                var cruiseSpeed = Parent.GetComponent<SEngineComponent>()?.Engine.CruiseSpeed ?? 300;
                physics.CruiseSpeedOffset = MathF.Min(physics.CruiseSpeedOffset,
                    -cruiseSpeed * (1 - escortSpeedFactor));
            }
            physics.EngineKillEnabled = EngineKill;
        }

        // Specific decomposition for roll
        private static void DecomposeOrientation(Matrix4x4 mx, out double xPitch, out double yYaw, out double zRoll)
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
