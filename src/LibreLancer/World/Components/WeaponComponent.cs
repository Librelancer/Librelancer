// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Utf;

namespace LibreLancer.World.Components
{
    public abstract class WeaponComponent : GameComponent
    {
        protected const float MuzzleConeAngleDegrees = 40f;
        private const float RotationEpsilon = 0.0001f;
        private const float FullTurn = MathF.PI * 2;

        public double CurrentCooldown = 0;

        public Vector2 Angles = new(0, 0);

        private bool restPoseCaptured;
        private float restRotation;
        private float restPitch;
        private bool barrelSearched;
        private RevConstruct? barrelConstruct;

        protected WeaponComponent(GameObject parent) : base(parent)
        {
            CaptureRestPose();
        }

        protected abstract float TurnRate { get; }

        public abstract float MaxRange { get; }

        public abstract int IdsName { get; }

        public override void Update(double time, GameWorld world)
        {
            CurrentCooldown -= time;
            if (CurrentCooldown < 0)
            {
                CurrentCooldown = 0;
            }

            if (_targetX > -1000)
            {
                DoRotation(_targetX, _targetY, time);
            }
        }

        public override void Register(GameWorld world)
        {
            CaptureRestPose();
        }

        private bool DoRotation(float x, float y, double time)
        {
            CaptureRestPose();
            if (Parent.Attachment is not { } hp)
                return true;
            var rads = MathHelper.DegreesToRadians(TurnRate);
            var delta = (float)(time * rads);
            var atTarget = true;

            if (hp.Revolute != null)
            {
                var fullRotation = hp.Revolute.Max - hp.Revolute.Min >= FullTurn - RotationEpsilon;
                var current = hp.CurrentRevolution;
                var target = fullRotation
                    ? MapAngleNearestCurrent(x, current, hp.Revolute.Min, hp.Revolute.Max)
                    : MathHelper.Clamp(x, hp.Revolute.Min, hp.Revolute.Max);
                if (fullRotation)
                {
                    var difference = NormalizeAngle(target - current);
                    current = MathF.Abs(difference) <= delta
                        ? target
                        : MapAngleToRange(current + MathF.CopySign(delta, difference),
                            hp.Revolute.Min, hp.Revolute.Max);
                }
                else
                {
                    current = MoveTowards(current, target, delta);
                }

                var previous = hp.CurrentRevolution;
                hp.Revolve(current);
                if (MathF.Abs(previous - hp.CurrentRevolution) > RotationEpsilon)
                    Parent.InvalidateWorldTransform();
                Angles.X = hp.CurrentRevolution;
                atTarget &= fullRotation
                    ? MathF.Abs(NormalizeAngle(target - hp.CurrentRevolution)) <= RotationEpsilon
                    : MathF.Abs(target - hp.CurrentRevolution) <= RotationEpsilon;
            }

            var barrel = GetBarrelConstruct();

            if (barrel != null)
            {
                var target = MathHelper.Clamp(y, barrel.Min, barrel.Max);
                var current = MoveTowards(barrel.Current, target, delta);

                barrel.Update(current, Quaternion.Identity);
                Angles.Y = barrel.Current;
                Parent.Model!.RigidModel.UpdateTransform();
                atTarget &= MathF.Abs(target - barrel.Current) <= RotationEpsilon;
            }

            return atTarget;
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
            if (TryGetAimAngles(point, out var rotation, out var pitch))
                DoRotation(rotation, pitch, time);
        }

        // Returns the turret to the pose they had when the weapon was created (on combat end). Returns true once both axes are at rest.
        public bool ReturnToRest(double time)
        {
            CaptureRestPose();
            return DoRotation(restRotation, restPitch, time);
        }

        public bool CanAimAt(Vector3 point)
        {
            if (!TryGetAimAngles(point, out var rotation, out var pitch))
                return false;

            var hp = Parent.Attachment!;
            var barrel = GetBarrelConstruct();
            var muzzleCone = MathHelper.DegreesToRadians(MuzzleConeAngleDegrees);

            if (hp.Revolute != null)
            {
                if (!AngleInRange(rotation, hp.Revolute.Min, hp.Revolute.Max))
                    return false;
            }
            else if (MathF.Abs(rotation) > muzzleCone)
            {
                return false;
            }

            if (barrel != null)
            {
                if (!AngleInRange(pitch, barrel.Min, barrel.Max))
                    return false;
            }
            else if (MathF.Abs(pitch) > muzzleCone)
            {
                return false;
            }

            // A completely fixed gun has a circular muzzle cone, rather than
            // independent yaw and pitch limits.
            return hp.Revolute != null || barrel != null ||
                   MathF.Acos(Math.Clamp(MathF.Cos(rotation) * MathF.Cos(pitch), -1f, 1f)) <= muzzleCone;
        }

        private bool TryGetAimAngles(Vector3 point, out float rotation, out float pitch)
        {
            rotation = 0;
            pitch = 0;

            if (Parent.Attachment == null || Parent.Parent == null)
                return false;

            // Work in the unrotated mounting hardpoint's coordinate system.
            // Freelancer weapons face -Z; yaw is around +Y and pitch around +X.
            var mountingTransform = Parent.Attachment.TransformNoRotate * Parent.Parent.WorldTransform;
            var local = mountingTransform.InverseTransform(point) -
                        mountingTransform.InverseTransform(GetAimOrigin(mountingTransform));
            if (local.LengthSquared() < float.Epsilon)
                return false;

            rotation = -MathF.Atan2(local.X, -local.Z);
            pitch = MathF.Atan2(local.Y, MathF.Sqrt(local.X * local.X + local.Z * local.Z));
            return true;
        }

        protected virtual Vector3 GetAimOrigin(Transform3D mountingTransform) => mountingTransform.Position;

        private RevConstruct? GetBarrelConstruct()
        {
            if (barrelSearched || Parent.Model == null)
                return barrelConstruct;

            barrelSearched = true;
            foreach (var mdl in Parent.Model.RigidModel.AllParts)
            {
                if (mdl.Construct is RevConstruct revCon)
                    barrelConstruct = revCon;
            }
            return barrelConstruct;
        }

        private void CaptureRestPose()
        {
            if (restPoseCaptured || Parent.Attachment == null)
                return;

            restRotation = Parent.Attachment.CurrentRevolution;
            var barrel = GetBarrelConstruct();
            restPitch = barrel?.Current ?? 0;
            Angles = new Vector2(restRotation, restPitch);
            restPoseCaptured = true;
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            var difference = target - current;
            if (MathF.Abs(difference) <= maxDelta)
                return target;
            return current + MathF.CopySign(maxDelta, difference);
        }

        private static float NormalizeAngle(float value)
        {
            var normalized = (value + MathF.PI) % FullTurn;
            if (normalized < 0)
                normalized += FullTurn;
            return normalized - MathF.PI;
        }

        private static float MapAngleToRange(float angle, float min, float max)
        {
            while (angle < min)
                angle += FullTurn;
            while (angle > max)
                angle -= FullTurn;

            // Numerical noise at the wrap boundary can put the equivalent angle
            // just outside the declared full-turn range.
            if (angle < min)
                return min;
            if (angle > max)
                return max;
            return angle;
        }

        private static float MapAngleNearestCurrent(float angle, float current, float min, float max)
        {
            angle = MapAngleToRange(angle, min, max);
            var nearest = current + NormalizeAngle(angle - current);
            if (nearest >= min - RotationEpsilon && nearest <= max + RotationEpsilon)
                return MathHelper.Clamp(nearest, min, max);

            var alternate = nearest < min ? nearest + FullTurn : nearest - FullTurn;
            return alternate >= min - RotationEpsilon && alternate <= max + RotationEpsilon
                ? MathHelper.Clamp(alternate, min, max)
                : angle;
        }

        private static bool AngleInRange(float angle, float min, float max)
        {
            const float epsilon = 0.0001f;
            if (max - min >= FullTurn - epsilon)
                return true;

            angle = NormalizeAngle(angle);
            min = NormalizeAngle(min);
            max = NormalizeAngle(max);
            return min <= max
                ? angle >= min - epsilon && angle <= max + epsilon
                : angle >= min - epsilon || angle <= max + epsilon;
        }

        protected static float GetAngle(Vector3 pointA, Vector3 pointB)
        {
            var angle = MathF.Acos(Math.Clamp(
                Vector3.Dot(pointA.Normalized(), pointB.Normalized()), -1f, 1f));
            return angle;
        }

        protected abstract bool OnFire(Vector3 point, GameWorld world, GameObject? target, bool server);

        public bool Fire(Vector3 point, GameWorld world, GameObject? target = null, bool fromServer = false)
        {
            if (!fromServer && Parent.Parent!.TryGetComponent<ShipPhysicsComponent>(out var flight) &&
                flight.EngineState is EngineStates.Cruise or EngineStates.CruiseCharging)
            {
                return false;
            }

            if (CurrentCooldown > 0 && !fromServer)
            {
                return false;
            }

            // Cloaked ships can't fire weapons
            return !Parent.Parent!.Flags.HasFlag(GameObjectFlags.Cloaked) && OnFire(point, world, target, fromServer);
        }
    }
}
