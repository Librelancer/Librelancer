using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using LibreLancer.Data.GameData.Items;

namespace LibreLancer.World.Components
{
    public class MissileLauncherComponent : WeaponComponent
    {
        public MissileLauncherEquipment Object;

        private float maxRange;

        public MissileLauncherComponent(GameObject parent, MissileLauncherEquipment Def) : base(parent)
        {
            Object = Def;

            var lt = Object.Munition.Def.Lifetime;

            if (Object.Munition.Motor != null)
            {
                maxRange = Object.Munition.Motor.Delay * Object.Def.MuzzleVelocity; //initial time + initial accel
                var accelEndTime = Object.Munition.Motor.Lifetime + Object.Munition.Motor.Delay;
                if (accelEndTime > lt)
                    accelEndTime = lt;
                var t = (accelEndTime - Object.Munition.Motor.Delay);
                if (t > 0) {
                    maxRange += (t * Object.Def.MuzzleVelocity) + 0.5f * Object.Munition.Motor.Accel * (t * t);
                }
                var maxSpeedTime = lt - accelEndTime;
                if (maxSpeedTime > 0 && t > 0) {
                    var newVel = Object.Def.MuzzleVelocity + (t * Object.Munition.Motor.Accel);
                    maxRange += newVel * maxSpeedTime;
                }
            }
            else
            {
                maxRange = Object.Munition.Def.Lifetime * Object.Def.MuzzleVelocity;
            }
            FLLog.Debug("Missile", $"{Def.Nickname} {maxRange}");
        }

        protected override float TurnRate => Object.Def.TurnRate;

        public override float MaxRange => maxRange;

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
        protected override bool OnFire(Vector3 point, GameObject target, bool fromServer)
        {
            //Consume ammo
            if (Object.Munition.Def.RequiresAmmo)
            {
                if (!Parent.Parent.TryGetComponent<AbstractCargoComponent>(out var cargo) ||
                    cargo.TryConsume(Object.Munition) == 0)
                {
                    return false;
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
                if (hpFire == null) return false;
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
                world.Projectiles.PlayProjectileSound(Parent.Parent, Object.Munition.Def.OneShotSound, tr.Position, hp);
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
            return true;
        }
    }
}
