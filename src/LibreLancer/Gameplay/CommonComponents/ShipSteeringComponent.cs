// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
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
        public int Tick;
        
        PIDController rollPID = new PIDController() { P = 2 };
        
        public ShipSteeringComponent(GameObject parent) : base(parent) { }

        public Vector3 OutputSteering;

        public override void Update(double time)
        {
            var physics = Parent.GetComponent<ShipPhysicsComponent>(); //Get mounted engine

            //Set output parameters
            var steerControl = new Vector3(InPitch, InYaw, InRoll);
            double pitch, yaw, roll;
            DecomposeOrientation(Parent.PhysicsComponent.Body.Transform, out pitch, out yaw, out roll);
            
            if (Math.Abs(InPitch) < float.Epsilon && Math.Abs(InYaw) < float.Epsilon)
                steerControl.Z = MathHelper.Clamp((float) rollPID.Update(0, roll, (float) time), -0.5f, 0.5f);
            else
                rollPID.Reset();
            OutputSteering = Parent.PhysicsComponent.Body.RotateVector(steerControl);
            OutputSteering = MathHelper.ApplyEpsilon(OutputSteering);
            physics.Steering = OutputSteering;
            physics.EnginePower = InThrottle;
            physics.Tick = Tick;
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