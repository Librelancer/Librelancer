using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Resources;

namespace LibreLancer.World.Equipments;

public class CargoPod : EquipmentModelObject<CargoPodEquipment>
{
    private const int UnitsPerDroppedContainer = 30;
    public List<BasicCargo> Cargo = [];
    private bool exploded = false;

    public CargoPod(GameObject parent,
        Hardpoint? attachment,
        CargoPodEquipment equipment,
        EquipmentType type,
        ResourceManager resources) : base(parent, attachment, equipment, type, resources, false)
    {
    }

    public override void OnKilled(GameObject? attacker, GameWorld world)
    {
        var currentWorld = world;
        if (exploded || currentWorld?.Server == null)
            return;

        exploded = true;
        var center = Parent.Transform.Position;

        foreach (var cargo in Cargo)
        {
            var crate = cargo.Item.LootAppearance;
            if (crate == null || cargo.Count <= 0)
                continue;

            var remaining = cargo.Count;
            while (remaining > 0)
            {
                var count = Math.Min(remaining, UnitsPerDroppedContainer);
                remaining -= count;

                var direction = world.Random.NextUnitVector();
                var offset = direction * (2 + (world.Random.NextSingle() * 4));
                var impulse = direction * (40 + (world.Random.NextSingle() * 60));
                currentWorld.Server.SpawnLoot(crate, cargo.Item, count,
                    new Transform3D(center + offset, Quaternion.Identity),
                    initialImpulse: impulse);
            }
        }

        var solar = Parent;
        var hardpoint = Attachment?.Name;
        if (hardpoint != null)
        {
            solar.RemoveEquipment(hardpoint, currentWorld);
            currentWorld.Server.OnEquipmentDestroyed(solar, this);
        }
        base.OnKilled(attacker, world);
    }
}
