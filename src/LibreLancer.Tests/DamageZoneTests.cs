using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using Xunit;

namespace LibreLancer.Tests;

public class DamageZoneTests
{
    private static Zone Sphere(Vector3 position, float radius, float damage) => new()
    {
        Position = position,
        Size = new Vector3(radius),
        Shape = ShapeKind.Sphere,
        Damage = damage
    };

    [Fact]
    public void ZoneDamageAtAddsOverlappingZones()
    {
        var system = new StarSystem { SourceFile = "" };
        system.Zones.Add(Sphere(Vector3.Zero, 100, 10));
        system.Zones.Add(Sphere(new Vector3(50, 0, 0), 100, 5));

        Assert.Equal(15, system.ZoneDamageAt(Vector3.Zero));
        Assert.Equal(0, system.ZoneDamageAt(new Vector3(1000, 0, 0)));
    }

    [Fact]
    public void InAtmosphereUsesSolarRange()
    {
        var system = new StarSystem { SourceFile = "" };
        system.Zones.Add(Sphere(Vector3.Zero, 1000, 10));
        system.Objects.Add(new SystemObject
        {
            Position = new Vector3(100, 0, 0),
            AtmosphereRange = 200
        });

        Assert.True(system.InAtmosphere(Vector3.Zero));
        Assert.False(system.InAtmosphere(new Vector3(-500, 0, 0)));
    }

    [Fact]
    public void ZoneDamageAffectsHullAndEquipmentButNotWeapons()
    {
        var ship = new GameObject();
        var shipHealth = Health(ship, 100);
        AddEquipment(ship, new Equipment { Hitpoints = 50 });
        var weaponHealth = AddEquipment(ship, new GunEquipment
        {
            Def = null!,
            Munition = null!,
            Hitpoints = 50
        });

        shipHealth.DamageZone(10);

        Assert.Equal(90, shipHealth.CurrentHealth);
        Assert.Equal(40, ship.Children[0].GetComponent<SHealthComponent>()!.CurrentHealth);
        Assert.Equal(50, weaponHealth.CurrentHealth);
    }

    private static SHealthComponent AddEquipment(GameObject ship, Equipment equipment)
    {
        var child = new GameObject { Parent = ship };
        child.AddComponent(new EquipmentComponent(equipment, child));
        var health = Health(child, equipment.Hitpoints);
        ship.Children.Add(child);
        return health;
    }

    private static SHealthComponent Health(GameObject obj, float value)
    {
        var health = new SHealthComponent(obj) { MaxHealth = value, CurrentHealth = value };
        obj.AddComponent(health);
        return health;
    }
}
