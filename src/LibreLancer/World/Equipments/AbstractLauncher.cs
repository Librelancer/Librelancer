using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Resources;
using LibreLancer.World.Components;

namespace LibreLancer.World.Equipments;

public abstract class AbstractLauncher : AbstractWeapon
{
    protected Hardpoint? HpFire;

    protected AbstractLauncher(GameObject parent,
        Hardpoint? attachment,
        Equipment equipment,
        EquipmentType type,
        ResourceManager resources) : base(parent, attachment, equipment, type, resources)
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


    public int AmmoCount
    {
        get
        {
            if (Munition == null || !Parent.TryGetComponent<AbstractCargoComponent>(out var cargo))
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
        var owner = Parent;
        if (munition == null || munition.ModelFile == null)
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
        RunMuzzleFlash();

        if (world.Server != null)
        {
            world.Server.FireDeployable(transform, munition, MuzzleVelocity, owner);
        }
        else
        {
            var hardpoint = Attachment!;
            world.Projectiles.PlayProjectileSound(owner, munition.Def.OneShotSound,
                transform.Position, hardpoint.Name);
            world.Projectiles.QueueMissile(hardpoint.CRC, null);
        }

        CurrentCooldown = GetRefireDelay();
        return true;
    }

    private bool TryConsumeResources(GameObject owner, MunitionEquip munition, bool consumePower)
    {
        if (consumePower && PowerUsage > 0 && owner.CoreEquipment.Power != null)
        {
            var power = owner.CoreEquipment.Power;
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
        if (Attachment == null)
        {
            return false;
        }

        HpFire ??= GetHardpoints()
            .FirstOrDefault(x => x.Name.StartsWith("hpfire", StringComparison.OrdinalIgnoreCase));

        var shipTransform = Parent.PhysicsComponent?.Body is { } body
            ? new Transform3D(body.Position, body.Orientation)
            : Parent.Transform;
        var mount = Attachment.Transform * shipTransform;
        transform = (HpFire?.Transform ?? Transform3D.Identity) * mount;
        return true;
    }
}
