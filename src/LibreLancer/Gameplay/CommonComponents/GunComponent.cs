using System;
using System.Numerics;
using System.Linq;
using LibreLancer.GameData.Items;

namespace LibreLancer
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

        protected override void OnFire(Vector3 point, GameObject target)
        {
            if (projectiles == null)
            {
                hpfires = Parent.GetHardpoints()
                    .Where((x) => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase)).ToArray();
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