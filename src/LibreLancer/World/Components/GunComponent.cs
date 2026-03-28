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

        private ProjectileManager projectiles = null!;
        private ProjectileData toSpawn = null!;
        private Hardpoint[] hpfires = [];

        protected override bool OnFire(Vector3 point, GameWorld world, GameObject? target, bool fromServer)
        {
            if (!fromServer)
            {
                CurrentCooldown = Object.Def.RefireDelay;

                if (Parent?.Parent?.TryGetComponent<PowerCoreComponent>(out var powercore) ?? false)
                {
                    if (powercore.CurrentEnergy < Object.Def.PowerUsage)
                    {
                        return false;
                    }

                    powercore.CurrentEnergy -= Object.Def.PowerUsage;
                }
            }

            if ((ProjectileManager?)projectiles == null)
            {
                hpfires = Parent!.GetHardpoints()
                    .Where((x) => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase)).ToArray();
                projectiles = world.Projectiles!;
                toSpawn = projectiles.GetData(Object);
            }

            if (Parent!.TryGetComponent<CMuzzleFlashComponent>(out var muzzleFlash))
            {
                muzzleFlash.OnFired();
            }

            var tr = (Parent.Attachment!.Transform * Parent.Parent!.WorldTransform);
            var hp = Parent.Attachment.Name;
            bool retval = false;

            foreach (var hpFire in hpfires)
            {
                var transform = hpFire.Transform * tr;
                var pos = transform.Position;
                var normal = Vector3.Transform(-Vector3.UnitZ, transform.Orientation);
                var heading = (point - pos).Normalized();

                var angle = GetAngle(normal, heading);

                if (!fromServer && !(angle <= MathHelper.DegreesToRadians(40))) // TODO: MUZZLE_CONE_ANGLE constant
                {
                    continue;
                }

                retval = true;
                projectiles.SpawnProjectile(Parent.Parent, hp, toSpawn, pos, heading);

                if (!fromServer)
                {
                    projectiles.QueueFire(Parent.Parent, this, point);
                }
            }

            return retval;
        }
    }
}
