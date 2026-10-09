using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Resources;
using LibreLancer.World.Components;

namespace LibreLancer.World.Equipments;

public class Gun : AbstractWeapon
{
    public GunEquipment GunEquipment;

    public Gun(
        GameObject parent,
        Hardpoint? attachment,
        GunEquipment equipment,
        EquipmentType type,
        ResourceManager resources) : base(parent, attachment, equipment, type, resources)
    {
        GunEquipment = equipment;
        if (type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject)
        {
            AddFlashEffect(GunEquipment.FlashEffect, resources);
        }
    }

    protected override float TurnRate => GunEquipment.Def.TurnRate;

    public override float MaxRange => GunEquipment.Munition.Def.Lifetime * GunEquipment.Def.MuzzleVelocity;

    private ProjectileManager projectiles = null!;
    private ProjectileData toSpawn = null!;
    private Hardpoint[] hpfires = [];
    private PowerCore? powercore;

    protected override bool OnFire(Vector3 point, GameWorld world, GameObject? target, bool fromServer)
    {
        if (!fromServer)
        {
            CurrentCooldown = GunEquipment.Def.RefireDelay;

            if (powercore != null)
            {
                if (powercore.CurrentEnergy < GunEquipment.Def.PowerUsage)
                {
                    return false;
                }

                powercore.CurrentEnergy -= GunEquipment.Def.PowerUsage;
            }
            else
            {
                return false;
            }
        }

        if ((ProjectileManager?)projectiles == null)
        {
            hpfires = GetHardpoints()
                .Where((x) => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase)).ToArray();
            projectiles = world.Projectiles!;
            toSpawn = projectiles.GetData(GunEquipment);
        }


        var tr = (Attachment!.Transform * Parent.Transform);
        var hp = Attachment.Name;
        bool didFire = false;

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

            didFire = true;
            projectiles.SpawnProjectile(Parent, hp, toSpawn, pos, heading);

            if (!fromServer)
            {
                projectiles.QueueFire(Parent, this, point);
            }
        }

        if(didFire)
            RunMuzzleFlash();

        return didFire;
    }

    public override void ResolveReferences()
    {
        powercore = Parent.EquipmentOfType<PowerCore>().FirstOrDefault();
    }
}
