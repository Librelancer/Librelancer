using System;
using System.Linq;
using System.Numerics;
using LibreLancer.GameData.Items;

namespace LibreLancer.World.Components
{
    public class MissileLauncherComponent : WeaponComponent
    {
        public MissileLauncherEquipment Object;

        public MissileLauncherComponent(GameObject parent, MissileLauncherEquipment Def) : base(parent)
        {
            Object = Def;
        }

        protected override float TurnRate => Object.Def.TurnRate;

        public override float MaxRange => Object.Munition.Def.Lifetime * Object.Def.MuzzleVelocity;

        public override int IdsName => Object.IdsName;

        GameObject GetTarget()
        {
            if (Parent?.Parent == null) return null;
            if (Parent.Parent.TryGetComponent<SelectedTargetComponent>(out var selection))
                return selection.Selected;
            return null;
        }

        private Hardpoint hpFire;
        private ProjectileManager projectiles;
        protected override void OnFire(Vector3 point, GameObject target)
        {
            //Consume ammo
            if (Object.Munition.Def.RequiresAmmo)
            {
                if (!Parent.Parent.TryGetComponent<AbstractCargoComponent>(out var cargo) ||
                    !cargo.TryConsume(Object.Munition))
                {
                    return;
                }
            }
            //
            var world = Parent.GetWorld();
            if (hpFire == null)
            {
                hpFire = Parent
                    .GetHardpoints()
                    .FirstOrDefault(x => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase));
            }
            if (world.Server != null)
            {
                if (hpFire == null) return;
                var tr = hpFire.Transform * (Parent.Attachment.Transform * Parent.Parent.WorldTransform);
                world.Server.FireMissile(tr, Object.Munition, Object.Def.MuzzleVelocity,Parent.Parent, target ?? GetTarget());
            }
            else
            {
                var hp = Parent.Attachment.Name;
                if (projectiles == null)
                {
                    projectiles = Parent.GetWorld().Projectiles;
                }
                //Play sound locally for latency reasons,
                //we won't play it again for missiles owned by us
                var tr = hpFire.Transform * (Parent.Attachment.Transform * Parent.Parent.WorldTransform);
                var pos = Vector3.Transform(Vector3.Zero, tr);
                world.Projectiles.PlayProjectileSound(Parent.Parent, Object.Munition.Def.OneShotSound, pos, hp);
                if (!string.IsNullOrEmpty(hp))
                {
                    world.Projectiles.QueueMissile(hp, target ?? GetTarget());
                }
                else
                {
                    FLLog.Error("Missile", "Firing unmounted missile");
                }
            }
            CurrentCooldown = Object.Def.RefireDelay;
        }
    }
}