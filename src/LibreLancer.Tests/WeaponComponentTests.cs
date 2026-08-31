using System;
using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
using LibreLancer.World;
using LibreLancer.World.Components;
using Xunit;

namespace LibreLancer.Tests;

public class WeaponComponentTests
{
    [Fact]
    public void TraverseMovementIsLimitedOnBothAxes()
    {
        var ship = new GameObject();
        var mount = new RigidModelPart { Name = "Root" };
        var hardpoint = new Hardpoint(new RevoluteHardpointDefinition("HpWeapon")
        {
            Axis = Vector3.UnitY,
            Min = -MathF.PI / 2,
            Max = MathF.PI / 2
        }, mount);
        var gunObject = new GameObject
        {
            Parent = ship,
            Attachment = hardpoint,
            Model = CreateGunModel()
        };
        ship.Children.Add(gunObject);

        var gun = new GunComponent(gunObject, new GunEquipment
        {
            Nickname = "test_gun",
            Def = new Gun { TurnRate = 90 },
            Munition = new MunitionEquip { Def = new Munition { Lifetime = 1 } }
        });
        gunObject.AddComponent(gun);

        gun.RotateTowards(MathF.PI / 2, MathF.PI / 2);
        using var world = new GameWorld(null, null, null, null, initPhys: false);
        gun.Update(0.1, world);

        var barrel = Assert.IsType<RevConstruct>(gunObject.Model!.RigidModel.AllParts[1].Construct);
        var expectedStep = MathHelper.DegreesToRadians(9);
        Assert.Equal(expectedStep, hardpoint.CurrentRevolution, 5);
        Assert.Equal(expectedStep, barrel.Current, 5);
    }

    private static DestructibleModel CreateGunModel()
    {
        var root = new RigidModelPart
        {
            Name = "Root",
            Children = []
        };
        var barrel = new RigidModelPart
        {
            Name = "Barrel",
            Construct = new RevConstruct
            {
                ParentName = "Root",
                ChildName = "Barrel",
                AxisRotation = Vector3.UnitX,
                Min = -MathF.PI / 2,
                Max = MathF.PI / 2
            }
        };
        root.Children.Add(barrel);

        var model = new RigidModel
        {
            Root = root,
            AllParts = [root, barrel],
            Parts = new ModelPartCollection(),
            Source = RigidModelSource.Compound
        };
        model.Parts.Add(root);
        model.Parts.Add(barrel);
        model.UpdateTransform();
        return new DestructibleModel(model, []);
    }
}
