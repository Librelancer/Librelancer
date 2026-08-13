using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData.Items;

namespace LibreLancer.World.Components
{
    public class GunComponent : WeaponComponent
    {
        public GunEquipment Object;

        public GunComponent(GameObject parent, GunEquipment Def) : base(parent)
        {
            Object = Def;
        }

        protected override float TurnRate => Object.Def.TurnRate;

        public override float MaxRange => Object.Munition.Def.Lifetime * Object.Def.MuzzleVelocity;

        public override int IdsName => Object.IdsName;

        private ProjectileManager? projectiles;
        private ProjectileData? toSpawn;
        private Hardpoint[] hpfires = [];
        private bool fireHardpointsInitialized;

        private Hardpoint[] GetFireHardpoints()
        {
            if (Parent.Model == null)
                return [];

            if (!fireHardpointsInitialized)
            {
                hpfires = Parent.GetHardpoints()
                    .Where(x => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase))
                    .ToArray();
                fireHardpointsInitialized = true;
            }
            return hpfires;
        }

        private bool TryGetMuzzleTransform(Hardpoint hpFire, out Transform3D transform)
        {
            if (Parent.Attachment == null || Parent.Parent == null)
            {
                transform = Transform3D.Identity;
                return false;
            }

            var mountTransform = Parent.Attachment.Transform * Parent.Parent.WorldTransform;
            transform = hpFire.Transform * mountTransform;
            return true;
        }

        internal bool TryGetMuzzleTransform(out Transform3D transform)
        {
            var fireHardpoints = GetFireHardpoints();
            if (fireHardpoints.Length > 0)
                return TryGetMuzzleTransform(fireHardpoints[0], out transform);

            transform = Transform3D.Identity;
            return false;
        }

        protected override Vector3 GetAimOrigin(Transform3D mountingTransform)
        {
            var fireHardpoints = GetFireHardpoints();
            return fireHardpoints.Length > 0
                ? (fireHardpoints[0].Transform * mountingTransform).Position
                : base.GetAimOrigin(mountingTransform);
        }

        internal bool IsMuzzleAligned(Vector3 point, float toleranceDegrees)
        {
            var fireHardpoints = GetFireHardpoints();
            if (fireHardpoints.Length == 0)
                return false;

            var minimumDot = MathF.Cos(MathHelper.DegreesToRadians(toleranceDegrees));
            foreach (var hpFire in fireHardpoints)
            {
                if (!TryGetMuzzleTransform(hpFire, out var transform))
                    return false;

                var toTarget = point - transform.Position;
                if (toTarget.LengthSquared() < float.Epsilon)
                    return false;

                var normal = Vector3.Transform(-Vector3.UnitZ, transform.Orientation);
                if (Vector3.Dot(Vector3.Normalize(normal), Vector3.Normalize(toTarget)) < minimumDot)
                    return false;
            }
            return true;
        }

        protected override bool OnFire(Vector3 point, GameWorld world, GameObject? target, bool fromServer)
        {
            var owner = Parent.Parent;
            var attachment = Parent.Attachment;
            if (owner == null || attachment == null)
                return false;

            if (!fromServer)
            {
                CurrentCooldown = Object.Def.RefireDelay;

                if (owner.TryGetComponent<PowerCoreComponent>(out var powercore))
                {
                    if (powercore.CurrentEnergy < Object.Def.PowerUsage)
                    {
                        return false;
                    }

                    powercore.CurrentEnergy -= Object.Def.PowerUsage;
                }
            }

            if (projectiles == null)
            {
                projectiles = world.Projectiles;
                if (projectiles == null)
                    return false;
                toSpawn = projectiles.GetData(Object);
            }

            if (Parent!.TryGetComponent<CMuzzleFlashComponent>(out var muzzleFlash))
            {
                muzzleFlash.OnFired();
            }

            var hp = attachment.Name;
            bool retval = false;

            foreach (var hpFire in GetFireHardpoints())
            {
                if (!TryGetMuzzleTransform(hpFire, out var transform))
                    continue;
                var pos = transform.Position;
                var normal = Vector3.Transform(-Vector3.UnitZ, transform.Orientation);
                var heading = (point - pos).Normalized();

                var angle = GetAngle(normal, heading);

                if (!fromServer && !(angle <= MathHelper.DegreesToRadians(MuzzleConeAngleDegrees)))
                {
                    continue;
                }

                retval = true;
                projectiles.SpawnProjectile(owner, hp, toSpawn!, pos, heading);

                if (!fromServer)
                {
                    projectiles.QueueFire(owner, this, point);
                }
            }

            return retval;
        }
    }
}
