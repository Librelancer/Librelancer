// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Render.Cameras;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Client.Components
{
    public class ShipInputComponent(GameObject parent) : GameComponent(parent)
    {
        public ChaseCamera? Camera;
        public Vector2 MousePosition;
        public Vector2 Viewport;
        public float Throttle = 0;
        public float AutopilotThrottle = 0;
        public bool InFormation = false;
        public float BankLimit = 35f;
        public bool MouseFlight = false;
        public bool Reverse = false;

        public int Rolling;

        private ShipSteeringComponent? steering;

        public PIDController PitchControl = new() { P = 3.5f };
        public PIDController YawControl = new() { P = 3.5f };
        public PIDController RollControl = new() { P = 4f };
        public override void Update(double time, GameWorld world)
        {
            steering ??= Parent!.GetComponent<ShipSteeringComponent>();
            if (Camera == null || steering == null)
            {
                return;
            }

            if (InFormation || AutopilotThrottle > 0)
            {
                steering.InThrottle = AutopilotThrottle;
            }
            else
            {
                steering.InThrottle = Reverse ? -1 : Throttle;
            }

            if (MouseFlight)
            {
                // Calculate turning direction
                var ep = Vector3Ex.UnProject(new Vector3(MousePosition.X, MousePosition.Y, 0.25f), Camera.Projection, Camera.View, Viewport);
                var tgt = Vector3Ex.UnProject(new Vector3(MousePosition.X, MousePosition.Y, 0f), Camera.Projection, Camera.View, Viewport);
                var dir = (tgt - ep).Normalized();
                var gotoPos = Camera.Position + (dir * 1000);
                // Turn
                TurnTowards(gotoPos, time);
                BankShip(Camera.CameraUp, time);
                steering.PlayerInput = true;
            }
            else
            {
                steering.InPitch = steering.InYaw = steering.InRoll = 0;
                steering.PlayerInput = false;
            }

            steering.InRoll = Rolling switch
            {
                -1 => -1,
                1 => 1,
                _ => steering.InRoll
            };
        }

        private void TurnTowards(Vector3 gotoPos, double dt)
        {
            var vec = Parent!.InverseTransformPoint(gotoPos);
            // normalize it
            vec.Normalize();
            // update pitch/yaw
            steering?.InYaw = -MathHelper.Clamp((float)YawControl.Update(0, vec.X, dt), -1, 1);
            steering?.InPitch = -MathHelper.Clamp((float)PitchControl.Update(0, -vec.Y, dt), -1, 1);
        }

        private void BankShip(Vector3 upVector, double dt)
        {
            var bankInfluence = (MousePosition.X - (Viewport.X * 0.5f)) / (Viewport.X * 0.5f);
            bankInfluence = MathHelper.Clamp(bankInfluence, -1, 1);

            bankInfluence *= Throttle;
            var bankTarget = MathHelper.DegreesToRadians(-(bankInfluence * BankLimit));
            var tr = Parent!.PhysicsComponent!.Body!.Orientation;
            var transformUp = CalcDir(tr, Vector3.UnitY);
            var transformForward = CalcDir(tr, -Vector3.UnitZ);
            var signedAngle = Vector3Ex.SignedAngle(transformUp, upVector, transformForward);
            var bankError = (signedAngle - bankTarget) * 0.1f;
            steering?.InRoll = (float)MathHelper.Clamp(RollControl.Update(bankTarget * 0.5f, signedAngle * 0.5f, dt), -1, 1);
        }

        // My math lib seems to be lacking at the moment
        private Vector3 CalcDir(Quaternion mat, Vector3 v)
        {
            var v0 = Vector3.Transform(Vector3.Zero, mat);
            var v1 = Vector3.Transform(v, mat);
            return (v1 - v0).Normalized();
        }
    }
}
