using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using LibreLancer.Data;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.Schema.Equipment;

namespace LibreLancer.World.Components
{
    public class MissileLauncherComponent : WeaponComponent
    {
        public MissileLauncherEquipment Object;
        private float maxRange;

        private Hardpoint? hpFire;
        private ProjectileManager? projectiles;

        protected override float TurnRate => Object.Def.TurnRate;
        public override float MaxRange => maxRange;
        public override int IdsName => Object.IdsName;

        internal static float CalculateRange(float lifetime, float muzzleVelocity, Motor? motor)
        {
            if (motor == null)
                return lifetime * muzzleVelocity;

            var range = motor.Delay * muzzleVelocity;
            var accelerationEnd = MathF.Min(motor.Lifetime + motor.Delay, lifetime);
            var accelerationTime = accelerationEnd - motor.Delay;
            if (accelerationTime > 0)
            {
                range += accelerationTime * muzzleVelocity +
                         0.5f * motor.Accel * accelerationTime * accelerationTime;

                var maxSpeedTime = lifetime - accelerationEnd;
                if (maxSpeedTime > 0)
                {
                    var maxSpeed = muzzleVelocity + accelerationTime * motor.Accel;
                    range += maxSpeed * maxSpeedTime;
                }
            }

            return range;
        }

        public MissileLauncherComponent(GameObject parent, MissileLauncherEquipment Def) : base(parent)
        {
            Object = Def;

            maxRange = CalculateRange(Object.Munition.Def.Lifetime, Object.Def.MuzzleVelocity,
                Object.Munition.Motor);

            FLLog.Debug("Missile", $"{Def.Nickname} {maxRange}");
        }

        private GameObject? GetTarget()
        {
            if (Parent?.Parent == null)
            {
                return null;
            }

            return Parent.Parent.TryGetComponent<SelectedTargetComponent>(out var selection)
                ? selection?.Selected
                : null;
        }

        protected override bool OnFire(Vector3 point, GameWorld world, GameObject? target, bool fromServer)
        {
            // Consume ammo
            if (Object.Munition.Def.RequiresAmmo)
            {
                if (!Parent.Parent!.TryGetComponent<AbstractCargoComponent>(out var cargo) ||
                    cargo.TryConsume(Object.Munition) == 0)
                {
                    return false;
                }
            }

            if (hpFire == null)
            {
                hpFire = Parent
                    .GetHardpoints()
                    .FirstOrDefault(x => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase));
            }

            if (world.Server != null)
            {
                if (hpFire == null)
                {
                    return false;
                }

                var tr = hpFire.Transform * (Parent.Attachment!.Transform * Parent.Parent!.WorldTransform);
                world.Server.FireMissile(tr, Object.Munition, Object.Def.MuzzleVelocity, Parent.Parent,
                    target ?? GetTarget());
            }
            else
            {
                var hp = Parent.Attachment!.Name;

                projectiles ??= world.Projectiles;

                // Play sound locally for latency reasons,
                // we won't play it again for missiles owned by us
                var tr = hpFire!.Transform * (Parent.Attachment.Transform * Parent.Parent!.WorldTransform);
                world.Projectiles.PlayProjectileSound(Parent.Parent, Object.Munition.Def.OneShotSound, tr.Position, hp);

                if (!string.IsNullOrEmpty(hp))
                {
                    world.Projectiles.QueueMissile(Parent.Attachment.CRC, target ?? GetTarget());
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
