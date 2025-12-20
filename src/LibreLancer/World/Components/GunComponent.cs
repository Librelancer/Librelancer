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

        ProjectileManager projectiles;
        ProjectileData toSpawn;
        Hardpoint[] hpfires;

        protected override bool OnFire(Vector3 point, GameObject target, bool fromServer)
        {
            if (!fromServer)
            {
                CurrentCooldown = Object.Def.RefireDelay;
                if (Parent.Parent.TryGetComponent<PowerCoreComponent>(out var powercore))
                {
                    if (powercore.CurrentEnergy < Object.Def.PowerUsage)
                        return false;
                    powercore.CurrentEnergy -= Object.Def.PowerUsage;
                }
            }
            if (projectiles == null)
            {
                hpfires = Parent.GetHardpoints()
                    .Where((x) => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase)).ToArray();
                projectiles = Parent.GetWorld().Projectiles;
                toSpawn = projectiles.GetData(Object);
            }

            if (Parent.TryGetComponent<CMuzzleFlashComponent>(out var muzzleFlash))
            {
                muzzleFlash.OnFired();
            }

            var tr = (Parent.Attachment.Transform * Parent.Parent.WorldTransform);
            var hp = Parent.Attachment.Name;
            bool retval = false;
            for (int i = 0; i < hpfires.Length; i++)
            {
                var x = hpfires[i].Transform * tr;
                var pos = x.Position;
                var normal = Vector3.Transform(-Vector3.UnitZ, x.Orientation);
                var heading = (point - pos).Normalized();

                var angle = GetAngle(normal, heading);
                if (fromServer || angle <= MathHelper.DegreesToRadians(40)) //TODO: MUZZLE_CONE_ANGLE constant
                {
                    retval = true;
                    projectiles.SpawnProjectile(Parent.Parent, hp, toSpawn, pos, heading);
                    if (!fromServer)
                        projectiles.QueueFire(Parent.Parent, this, point);
                }
            }
            return retval;
        }
    }
}
