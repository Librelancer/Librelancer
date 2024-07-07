// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.World.Components
{
    public abstract class WeaponComponent : GameComponent
    {
        public double CurrentCooldown = 0;

        public Vector2 Angles = new Vector2(0, 0);

        protected WeaponComponent(GameObject parent) : base(parent) { }

        protected abstract float TurnRate { get; }

        public abstract float MaxRange { get; }

        public abstract int IdsName { get; }

        public override void Update(double time)
        {
            CurrentCooldown -= time;
            if (CurrentCooldown < 0) CurrentCooldown = 0;
            if (_targetX > -1000) {
                DoRotation(_targetX, _targetY, time);
            }
        }

        void DoRotation(float x, float y, double time)
        {
            var hp = Parent.Attachment;
            var rads = MathHelper.DegreesToRadians(TurnRate);
            var delta = (float)(time * rads);
            if(hp.Revolute != null)
            {
                var target = x;
                var current = Parent.Attachment.CurrentRevolution;

                if(current > target) {
                    current -= delta;
                    if (current <= target) current = target;
                }
                if(current < target) {
                    current += delta;
                    if (current >= target) current = target;
                }
                hp.Revolve(current);
                Angles.X = current;
            }
            //TODO: Finding barrel construct properly?
            Utf.RevConstruct barrel = null;
            foreach (var mdl in Parent.RigidModel.AllParts)
                if (mdl.Construct is Utf.RevConstruct revCon)
                    barrel = revCon;
            if(barrel != null) {
                var target = y;
                var current = barrel.Current;
                if (current > target)
                {
                    current -= delta;
                    if (current <= target) current = target;
                }
                if (current < target)
                {
                    current += delta;
                    if (current >= target) current = target;
                }

                barrel.Update(target, Quaternion.Identity);
                Angles.Y = current;
                Parent.RigidModel.UpdateTransform();
            }
        }

        private float _targetX = -1000;
        private float _targetY = -1000;
        public void RotateTowards(float x, float y)
        {
            _targetX = x;
            _targetY = y;
        }

        public void AimTowards(Vector3 point, double time)
        {
            var hp = Parent.Attachment;
            //Parent is the gun itself rotated
            var br = (hp.TransformNoRotate * Parent.Parent.WorldTransform).Matrix();
            //Inverse Transform
            Matrix4x4.Invert(br, out var beforeRotate);
            var local = TransformGL(point, beforeRotate);
            var localProper = local.Normalized();
            var x = -localProper.X * (float) Math.PI;
            var y = localProper.Y * (float) Math.PI;
            DoRotation(x, y, time);
        }

        static Vector3 TransformGL(Vector3 position, Matrix4x4 matrix)
        {
            return new Vector3(
                position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41,
                position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42,
                position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43);
        }

        protected static float GetAngle(Vector3 pointA, Vector3 pointB)
        {
            var angle = MathF.Acos(Vector3.Dot(pointA.Normalized(), pointB.Normalized()));
            return angle;
        }

        protected abstract bool OnFire(Vector3 point, GameObject target, bool server);

        public bool Fire(Vector3 point, GameObject target = null, bool fromServer = false)
        {
            if (!fromServer && Parent.Parent.TryGetComponent<ShipPhysicsComponent>(out var flight) &&
                (flight.EngineState == EngineStates.Cruise || flight.EngineState == EngineStates.CruiseCharging))
                return false;
            if (CurrentCooldown > 0 && !fromServer) return false;
            return OnFire(point, target, fromServer);

        }
    }
}
