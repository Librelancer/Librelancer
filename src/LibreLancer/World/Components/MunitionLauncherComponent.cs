using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData.Items;

namespace LibreLancer.World.Components;

public abstract class MunitionLauncherComponent : WeaponComponent
{
    protected Hardpoint? HpFire;

    protected MunitionLauncherComponent(GameObject parent) : base(parent)
    {
    }

    public abstract MunitionEquip? Munition { get; }
    protected abstract float MuzzleVelocity { get; }
    protected abstract float PowerUsage { get; }
    protected abstract string? UseAnimation { get; }

    protected override float TurnRate => 0;

    public override float MaxRange => Munition == null
        ? 0
        : Munition.Def.Lifetime * MuzzleVelocity;

    public override int IdsName => Parent.GetComponent<EquipmentComponent>()?.Equipment.IdsName ?? 0;

    public int AmmoCount
    {
        get
        {
            if (Munition == null || Parent.Parent == null ||
                !Parent.Parent.TryGetComponent<AbstractCargoComponent>(out var cargo))
            {
                return 0;
            }

            return cargo.GetCargo(0)
                .Where(x => x.EquipCRC == Munition.CRC && string.IsNullOrEmpty(x.Hardpoint))
                .Sum(x => x.Count);
        }
    }

    public bool UsesAmmo => Munition?.Def.RequiresAmmo == true;

    protected override bool OnFire(Vector3 point, GameWorld world, GameObject? target, bool fromServer)
    {
        var munition = Munition;
        var owner = Parent.Parent;
        if (munition == null || owner == null || munition.ModelFile == null)
        {
            return false;
        }

        if (!TryGetFireTransform(out var transform))
        {
            return false;
        }

        if (munition.Def.RequiresAmmo && AmmoCount <= 0)
        {
            return false;
        }

        if (!TryConsumeResources(owner, munition, world.Server != null || !fromServer))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(UseAnimation))
        {
            Parent.AnimationComponent?.StartAnimation(UseAnimation, false);
        }
        if (Parent.TryGetComponent<CMuzzleFlashComponent>(out var muzzleFlash))
        {
            muzzleFlash.OnFired();
        }

        if (world.Server != null)
        {
            world.Server.FireDeployable(transform, munition, MuzzleVelocity, owner);
        }
        else
        {
            var hardpoint = Parent.Attachment!;
            world.Projectiles.PlayProjectileSound(owner, munition.Def.OneShotSound,
                transform.Position, hardpoint.Name);
            world.Projectiles.QueueMissile(hardpoint.CRC, null);
        }

        CurrentCooldown = GetRefireDelay();
        return true;
    }

    private bool TryConsumeResources(GameObject owner, MunitionEquip munition, bool consumePower)
    {
        if (consumePower && PowerUsage > 0 &&
            owner.TryGetComponent<PowerCoreComponent>(out var power))
        {
            if (power.CurrentEnergy < PowerUsage ||
                (munition.Def.RequiresAmmo && !TryConsumeAmmo(owner, munition)))
            {
                return false;
            }

            power.CurrentEnergy -= PowerUsage;
            return true;
        }

        return !munition.Def.RequiresAmmo || TryConsumeAmmo(owner, munition);
    }

    private static bool TryConsumeAmmo(GameObject owner, MunitionEquip munition) =>
        owner.TryGetComponent<AbstractCargoComponent>(out var cargo) &&
        cargo.TryConsume(munition) > 0;

    protected abstract double GetRefireDelay();

    private bool TryGetFireTransform(out Transform3D transform)
    {
        transform = Transform3D.Identity;
        if (Parent.Parent == null || Parent.Attachment == null)
        {
            return false;
        }

        HpFire ??= Parent.GetHardpoints()
            .FirstOrDefault(x => x.Name.StartsWith("hpfire", StringComparison.OrdinalIgnoreCase));

        var shipTransform = Parent.Parent.PhysicsComponent?.Body is { } body
            ? new Transform3D(body.Position, body.Orientation)
            : Parent.Parent.WorldTransform;
        var mount = Parent.Attachment.Transform * shipTransform;
        transform = (HpFire?.Transform ?? Transform3D.Identity) * mount;
        return true;
    }
}

public sealed class CountermeasureLauncherComponent : MunitionLauncherComponent
{
    public CountermeasureEquipment Object { get; }

    public CountermeasureLauncherComponent(GameObject parent, CountermeasureEquipment equipment) : base(parent)
    {
        Object = equipment;
    }

    public override MunitionEquip? Munition => Object.Munition;
    protected override float MuzzleVelocity => Object.Def.MuzzleVelocity;
    protected override float PowerUsage => Object.Def.PowerUsage;
    protected override string? UseAnimation => Object.Def.UseAnimation;
    protected override double GetRefireDelay() => Object.Def.RefireDelay;
}

public sealed class MineLauncherComponent : MunitionLauncherComponent
{
    public MineDropperEquipment Object { get; }

    public MineLauncherComponent(GameObject parent, MineDropperEquipment equipment) : base(parent)
    {
        Object = equipment;
    }

    public override MunitionEquip? Munition => Object.Mine;
    protected override float MuzzleVelocity => Object.Def.MuzzleVelocity;
    protected override float PowerUsage => Object.Def.PowerUsage;
    protected override string? UseAnimation => Object.Def.UseAnimation;
    protected override double GetRefireDelay() => Object.Def.RefireDelay;
}
