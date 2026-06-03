using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Server.Components;

namespace LibreLancer.World.Components;

public class CargoPodComponent : GameComponent
{
    public List<BasicCargo> Cargo = [];

    private GameWorld? world;
    private SHealthComponent? health;
    private readonly Random random = new();
    private bool exploded;

    public CargoPodComponent(GameObject parent) : base(parent)
    {
    }

    public override void Register(GameWorld world)
    {
        this.world = world;
        health = Parent.GetComponent<SHealthComponent>();
        if (health != null)
            health.KilledHook += OnKilled;
    }

    public override void Unregister(GameWorld world)
    {
        if (health != null)
            health.KilledHook -= OnKilled;
        this.world = null;
    }

    private Vector3 RandomDirection()
    {
        var direction = new Vector3(
            (random.NextSingle() * 2) - 1,
            (random.NextSingle() * 2) - 1,
            (random.NextSingle() * 2) - 1);

        return direction.LengthSquared() > float.Epsilon ? direction.Normalized() : Vector3.UnitY;
    }

    private void OnKilled(GameObject? attacker)
    {
        var currentWorld = world;
        if (exploded || currentWorld?.Server == null)
            return;

        exploded = true;
        var center = Parent.WorldTransform.Position;

        foreach (var cargo in Cargo)
        {
            var crate = cargo.Item.LootAppearance;
            if (crate == null || cargo.Count <= 0)
                continue;

            var direction = RandomDirection();
            var offset = direction * (2 + (random.NextSingle() * 4));
            var impulse = direction * (40 + (random.NextSingle() * 60));
            currentWorld.Server.SpawnLoot(crate, cargo.Item, cargo.Count,
                new Transform3D(center + offset, Quaternion.Identity),
                initialImpulse: impulse);
        }

        var solar = Parent.Parent;
        var hardpoint = Parent.Attachment?.Name;
        if (solar != null && hardpoint != null)
        {
            solar.RemoveEquipment(hardpoint, currentWorld);
            currentWorld.Server.EquipmentDestroyed(solar, hardpoint);
        }
        else
        {
            Parent.Unregister(currentWorld);
            solar?.Children.Remove(Parent);
        }
    }
}
