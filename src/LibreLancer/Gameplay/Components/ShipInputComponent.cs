// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public class ShipInputComponent : GameComponent
    {
        public ChaseCamera Camera;
        public Vector2 MousePosition;
        public Vector2 Viewport;
        public float Throttle = 0;
        public float BankLimit = 35f;
        public bool MouseFlight = false;

        public int Rolling;

        ShipPhysicsComponent physics;

        public PIDController PitchControl = new PIDController() { P = 5 };
        public PIDController YawControl = new PIDController() { P = 40 };
        public PIDController RollControl = new PIDController() { P = 4f };
        public override void FixedUpdate(TimeSpan time)
        {
            if (physics == null) physics = Parent.GetComponent<ShipPhysicsComponent>();
            if (Camera == null) return;
            if (physics == null) return;
            physics.EnginePower = Throttle;
            if (MouseFlight)
            {
                //Calculate turning direction
                var ep = VectorMath.UnProject(new Vector3(MousePosition.X, MousePosition.Y, 0.25f), Camera.Projection, Camera.View, Viewport);
                var tgt = VectorMath.UnProject(new Vector3(MousePosition.X, MousePosition.Y, 0f), Camera.Projection, Camera.View, Viewport);
                var dir = (tgt - ep).Normalized();
                var gotoPos = Camera.Position + (dir * 1000);
                //Turn
                TurnTowards(gotoPos, time.TotalSeconds);
                BankShip(Camera.CameraUp, time.TotalSeconds);
            }
            else
            {
                physics.PlayerYaw = physics.PlayerPitch = 0;
                physics.Roll = 0;
            }
            if (Rolling == -1) physics.Roll = -1;
            else if (Rolling == 1) physics.Roll = 1;
        }

        void TurnTowards(Vector3 gotoPos,double dt)
        {
            var vec = Parent.InverseTransformPoint(gotoPos);
            //normalize it
            vec.Normalize();
            //update pitch/yaw
            physics.PlayerYaw = -MathHelper.Clamp((float)YawControl.Update(0, vec.X, dt), -1, 1);
            physics.PlayerPitch = -MathHelper.Clamp((float)PitchControl.Update(0, -vec.Y, dt), -1, 1);
        }

        void BankShip(Vector3 upVector, double dt)
        {
            float bankInfluence = (MousePosition.X - (Viewport.X * 0.5f)) / (Viewport.X * 0.5f);
            bankInfluence = MathHelper.Clamp(bankInfluence, -1, 1);

            bankInfluence *= Throttle;
            float bankTarget = MathHelper.DegreesToRadians(-(bankInfluence * BankLimit));
            var tr = Parent.PhysicsComponent.Body.Transform;
            var transformUp = CalcDir(ref tr, Vector3.Up);
            var transformForward = CalcDir(ref tr, Vector3.Forward);

            float signedAngle = Vector3.SignedAngle(transformUp, upVector, transformForward);
            float bankError = (signedAngle - bankTarget) * 0.1f;

            physics.Roll = (float)MathHelper.Clamp(RollControl.Update(bankTarget * 0.5f, signedAngle * 0.5f, dt), -1, 1);
            
        }

        //My math lib seems to be lacking at the moment
        Vector3 CalcDir(ref Matrix4 mat, Vector3 v)
        {
            var v0 = mat.Transform(Vector3.Zero);
            var v1 = mat.Transform(v);
            return (v1 - v0).Normalized();
        }

        public ShipInputComponent(GameObject parent) : base(parent)
        {
        }
    }
}
