// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using LibreLancer.GameData.Items;
namespace LibreLancer
{
    public class WeaponComponent : GameComponent
    {
        public GunEquipment Object;
        public double CurrentCooldown = 0;

        public Vector2 Angles = new Vector2(0, 0);

        public WeaponComponent(GameObject parent, GunEquipment def) : base(parent)
        {
            Object = def;
        }

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
            var rads = MathHelper.DegreesToRadians(Object.Def.TurnRate);
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

        void DrawDebugPoints()
        {
            if (projectiles == null) {
                hpfires = Parent.GetHardpoints().Where((x) => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase)).ToArray();
                projectiles = Parent.GetWorld().Projectiles;
                toSpawn = projectiles.GetData(Object);
            }
            var tr = (Parent.Attachment.Transform * Parent.Parent.WorldTransform);
            for (int i = 0; i < hpfires.Length; i++)
            {
                var pos = Vector3.Transform(Vector3.Zero, hpfires[i].Transform * tr);
                Parent.Parent.World.DrawDebug(pos);
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
            DrawDebugPoints();
            var hp = Parent.Attachment;
            //Parent is the gun itself rotated
            var br = hp.TransformNoRotate * Parent.Parent.WorldTransform;
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


        ProjectileManager projectiles;
        ProjectileData toSpawn;
        Hardpoint[] hpfires;


        static float GetAngle(Vector3 pointA, Vector3 pointB)
        {
            var angle = MathF.Acos(Vector3.Dot(pointA.Normalized(), pointB.Normalized()));
            return angle;
        }

        public void Fire(Vector3 point)
        {
            if (Parent.Parent.TryGetComponent<ShipPhysicsComponent>(out var flight) &&
                (flight.EngineState == EngineStates.Cruise || flight.EngineState == EngineStates.CruiseCharging))
                return;
            if (CurrentCooldown > 0) return;
            if (projectiles == null) {
                hpfires = Parent.GetHardpoints().Where((x) => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase)).ToArray();
                projectiles = Parent.GetWorld().Projectiles;
                toSpawn = projectiles.GetData(Object);
            }
            var tr = (Parent.Attachment.Transform * Parent.Parent.WorldTransform);
            var hp = Parent.Attachment.Name;
            for (int i = 0; i < hpfires.Length; i++)
            {
                var pos = Vector3.Transform(Vector3.Zero, hpfires[i].Transform * tr);
                var normal = Vector3.TransformNormal(-Vector3.UnitZ, hpfires[i].Transform * tr);
                var heading = (point - pos).Normalized();

                var angle = GetAngle(normal, heading);
                if (angle <= MathHelper.DegreesToRadians(40)) //TODO: MUZZLE_CONE_ANGLE constant
                {
                    projectiles.SpawnProjectile(Parent.Parent, hp, toSpawn, pos, heading);
                    projectiles.QueueProjectile(Parent.Parent.NetID, Object, hp, pos, heading);
                }
            }
            CurrentCooldown = Object.Def.RefireDelay;
        }
    }
}
