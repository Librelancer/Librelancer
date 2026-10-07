using System;
using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
using LibreLancer.World;
using LibreLancer.World.Components;
using Xunit;
using Gun = LibreLancer.World.Equipments.Gun;

namespace LibreLancer.Tests;

public class WeaponObjectTests
{
    [Fact]
    public void TraverseMovementIsLimitedOnBothAxes()
    {
        var mock = new MockData();
        mock.HashAndAdd(new GunEquipment
        {
            Nickname = "guntest",
            Def = new() { TurnRate = 90 },
            Munition = new() { Def = new() { Lifetime = 1 }},
            ModelFile = new()
            {
                ModelFile = "guntest.cmp", SourcePath = "guntest.cmp", LibraryFiles = []
            }
        }, mock.GameData.Items.Equipment);


        var ship = new GameObject();
        var mount = new RigidModelPart { Name = "Root" };
        var hardpoint = new Hardpoint(new RevoluteHardpointDefinition("HpWeapon")
        {
            Axis = Vector3.UnitY,
            Min = -MathF.PI / 2,
            Max = MathF.PI / 2
        }, mount);

        var gunObject = new Gun(ship, hardpoint,
            (GunEquipment)mock.GameData.Items.Equipment.Get("guntest")!,
            EquipmentType.Server, mock.Resources);
        ship.AddChild(gunObject);

        gunObject.RotateTowards(MathF.PI / 2, MathF.PI / 2);
        using var world = new GameWorld(null, null, null, null, initPhys: false);
        ship.Update(0.1, world);
        var barrel = Assert.IsType<RevConstruct>(gunObject.Model!.RigidModel.AllParts[1].Construct);
        var expectedStep = MathHelper.DegreesToRadians(9);
        Assert.Equal(expectedStep, hardpoint.CurrentRevolution, 5);
        Assert.Equal(expectedStep, barrel.Current, 5);
    }
}
