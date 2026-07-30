using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Server.Components;
using LibreLancer.Utf.Cmp;
using LibreLancer.World;
using LibreLancer.World.Components;
using Xunit;

namespace LibreLancer.Tests;

public class CollisionGroupDamageTests
{
    private sealed class TestWeapon(GameObject parent) : WeaponComponent(parent)
    {
        protected override float TurnRate => 0;
        public override float MaxRange => 0;
        public override int IdsName => 0;
        protected override bool OnFire(Vector3 point, GameWorld world, GameObject? target, bool server) => false;
    }

    private static (GameObject Object, RigidModelPart Wing, SHealthComponent Health) CreateShip(
        bool rootHealthProxy)
    {
        var root = new RigidModelPart { Name = "Root" };
        var wing = new RigidModelPart { Name = "Wing" };
        root.Children = [wing];
        var parts = new ModelPartCollection();
        parts.Add(root);
        parts.Add(wing);
        var rigidModel = new RigidModel
        {
            Root = root,
            AllParts = [root, wing],
            Parts = parts,
            Source = RigidModelSource.Compound
        };
        rigidModel.UpdateTransform();

        var obj = new GameObject
        {
            Model = new DestructibleModel(rigidModel,
            [
                new SeparablePart
                {
                    Part = "Wing",
                    HitPoints = 50,
                    Separable = true,
                    RootHealthProxy = rootHealthProxy
                }
            ])
        };
        var health = new SHealthComponent(obj)
        {
            CurrentHealth = 100,
            MaxHealth = 100
        };
        obj.AddComponent(health);
        return (obj, wing, health);
    }

    [Fact]
    public void RootHealthProxyDamagesPartAndHull()
    {
        var (obj, wing, health) = CreateShip(true);

        Assert.Null(health.Damage(30, 0, null, wing));
        Assert.Equal(70, health.CurrentHealth);
        Assert.True(obj.Model!.TryGetCollisionGroup(wing, out var group));
        Assert.Equal(20, group.CurrentHealth);

        Assert.Same(wing, health.Damage(20, 0, null, wing));
        Assert.Equal(50, health.CurrentHealth);
        Assert.Equal(0, group.CurrentHealth);
    }

    [Fact]
    public void IndependentCollisionGroupDoesNotDamageHull()
    {
        var (obj, wing, health) = CreateShip(false);

        Assert.Null(health.Damage(30, 0, null, wing));
        Assert.Equal(100, health.CurrentHealth);
        Assert.True(obj.Model!.TryGetCollisionGroup(wing, out var group));
        Assert.Equal(20, group.CurrentHealth);
    }

    [Fact]
    public void SelectingAnotherObjectClearsCollisionGroupTarget()
    {
        var owner = new GameObject();
        var selection = new SelectedTargetComponent(owner)
        {
            Selected = new GameObject(),
            SelectedPart = 123
        };

        selection.Selected = new GameObject();

        Assert.Null(selection.SelectedPart);
    }

    [Fact]
    public void DestroyingPartRemovesMountedWeapon()
    {
        var (obj, wing, _) = CreateShip(false);
        var weaponObject = new GameObject
        {
            Parent = obj,
            Attachment = new Hardpoint(new FixedHardpointDefinition("HpWeapon01"), wing)
        };
        weaponObject.AddComponent(new TestWeapon(weaponObject));
        obj.Children.Add(weaponObject);
        var weapons = new WeaponControlComponent(obj);
        obj.AddComponent(weapons);
        weapons.UpdateNetWeapons();

        Assert.Single(weapons.GetUiElements());
        Assert.True(obj.DisableCmpPart("Wing", null, null!, out _));
        Assert.Empty(weapons.GetUiElements());
        Assert.Empty(weapons.NetOrderWeapons!);
        Assert.Equal(0, weapons.GetAverageGunSpeed());
    }
}
