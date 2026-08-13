using System;
using System.IO;
using System.Numerics;
using System.Text;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.IO;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Data.Schema.Pilots;
using LibreLancer.Resources;
using LibreLancer.Server.Components;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
using LibreLancer.World;
using LibreLancer.World.Components;
using Xunit;
using GunSchema = LibreLancer.Data.Schema.Equipment.Gun;

namespace LibreLancer.Tests;

public class AutoTurretTests
{
    [Fact]
    public void ParsesAutoTurretEquipmentKey()
    {
        const string source = "[Gun]\nnickname = test_auto_turret\nauto_turret = true\n";
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(source));
        var equipment = new EquipmentIni();

        equipment.ParseIni(stream, "auto_turret_test.ini");

        var gun = Assert.IsType<GunSchema>(Assert.Single(equipment.Equip));
        Assert.True(gun.AutoTurret);
    }

    [Fact]
    public void UniverseSolarAffiliationComesFromReputationField()
    {
        var reputation = CreateFaction("solar_faction");
        var legacyFaction = CreateFaction("legacy_faction");

        Assert.Same(reputation, GameWorld.GetObjectFaction(new SystemObject
        {
            Reputation = reputation,
            Faction = legacyFaction
        }));
        Assert.Same(legacyFaction, GameWorld.GetObjectFaction(new SystemObject
        {
            Faction = legacyFaction
        }));
    }

    [Fact]
    public void AutoTurretsDoNotContributeToCrosshairBallistics()
    {
        var ship = new GameObject();
        AddGun(ship, autoTurret: false, muzzleVelocity: 500, lifetime: 2);
        AddGun(ship, autoTurret: true, muzzleVelocity: 2000, lifetime: 4);
        var weapons = new WeaponControlComponent(ship);

        Assert.Equal(500, weapons.GetAverageGunSpeed());
        Assert.Equal(1000, weapons.GetGunMaxRange());
    }

    [Fact]
    public void OnlyAutoTurretsProduceNoCrosshairBallistics()
    {
        var ship = new GameObject();
        AddGun(ship, autoTurret: true, muzzleVelocity: 2000, lifetime: 4);
        var weapons = new WeaponControlComponent(ship);

        Assert.Equal(0, weapons.GetAverageGunSpeed());
        Assert.Equal(0, weapons.GetGunMaxRange());
    }

    [Fact]
    public void ParentAimPointDoesNotOverrideAutoTurretOrientation()
    {
        var ship = new GameObject();
        var turret = AddGun(ship, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI, maxRotation: MathF.PI);
        var weapons = new WeaponControlComponent(ship)
        {
            AimPoint = new Vector3(100, 0, -100)
        };
        using var world = new GameWorld(null, null, null, null, initPhys: false);

        weapons.Update(1, world);

        Assert.Equal(0, turret.Angles.X);
        Assert.Equal(0, turret.Parent.Attachment!.CurrentRevolution);
    }

    [Fact]
    public void SolarAutoTurretSelectsClosestHostileInsideItsCone()
    {
        var ownerFaction = CreateFaction("owner");
        var enemyFaction = CreateFaction("enemy");
        ownerFaction.Reputations[enemyFaction] = -1;

        var solar = new GameObject { Flags = GameObjectFlags.Exists, Kind = GameObjectKind.Solar };
        var turret = AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI / 2, maxRotation: MathF.PI / 2);
        solar.AddComponent(new SSolarComponent(solar) { Faction = ownerFaction });
        Assert.True(SAutoTurretComponent.TryAdd(solar));
        var controller = Assert.IsType<SAutoTurretComponent>(
            solar.GetComponent<SAutoTurretComponent>());
        Assert.NotNull(solar.GetComponent<WeaponControlComponent>());

        var friendly = CreateTarget("friendly", ownerFaction, new Vector3(0, 0, -25));
        var behind = CreateTarget("behind", enemyFaction, new Vector3(0, 0, 20));
        var closestInCone = CreateTarget("closest", enemyFaction, new Vector3(0, 0, -100));
        var fartherInCone = CreateTarget("farther", enemyFaction, new Vector3(0, 0, -250));
        var outOfRange = CreateTarget("out_of_range", enemyFaction, new Vector3(0, 0, -1200));

        var selected = controller.FindTarget(turret,
            [friendly, behind, fartherInCone, outOfRange, closestInCone]);

        Assert.Same(closestInCone, selected);
    }

    [Fact]
    public void EachAutoTurretSelectsItsOwnTargetInsideItsOwnCone()
    {
        var ownerFaction = CreateFaction("owner");
        var enemyFaction = CreateFaction("enemy");
        ownerFaction.Reputations[enemyFaction] = -1;

        var solar = new GameObject { Flags = GameObjectFlags.Exists, Kind = GameObjectKind.Solar };
        var rightTurret = AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI / 2, maxRotation: 0, hardpointName: "HpWeapon01");
        var leftTurret = AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: 0, maxRotation: MathF.PI / 2, hardpointName: "HpWeapon02");
        solar.AddComponent(new SSolarComponent(solar) { Faction = ownerFaction });
        Assert.True(SAutoTurretComponent.TryAdd(solar));
        var controller = Assert.IsType<SAutoTurretComponent>(solar.GetComponent<SAutoTurretComponent>());

        var rightTarget = CreateTarget("right", enemyFaction, new Vector3(100, 0, -100));
        var leftTarget = CreateTarget("left", enemyFaction, new Vector3(-100, 0, -100));

        Assert.Same(rightTarget, controller.FindTarget(rightTurret, [leftTarget, rightTarget]));
        Assert.Same(leftTarget, controller.FindTarget(leftTurret, [rightTarget, leftTarget]));
    }

    [Fact]
    public void SolarAutoTurretTracksAndRequestsNetworkUpdates()
    {
        var ownerFaction = CreateFaction("owner");
        var enemyFaction = CreateFaction("enemy");
        ownerFaction.Reputations[enemyFaction] = -1;

        var solar = new GameObject { Flags = GameObjectFlags.Exists, Kind = GameObjectKind.Solar };
        AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI / 2, maxRotation: MathF.PI / 2);
        var solarComponent = new SSolarComponent(solar) { Faction = ownerFaction };
        solar.AddComponent(solarComponent);
        Assert.True(SAutoTurretComponent.TryAdd(solar));

        var hostile = CreateTarget("hostile", enemyFaction, new Vector3(0, 0, -100));
        using var world = new GameWorld(null, null, null, null, initPhys: false);
        world.AddObject(solar);
        world.AddObject(hostile);
        solar.Register(world);
        hostile.Register(world);

        world.Update(0.05);

        var controller = Assert.IsType<SAutoTurretComponent>(
            solar.GetComponent<SAutoTurretComponent>());
        Assert.Equal(1, controller.TrackingCount);
        Assert.True(solarComponent.SendAutoTurretUpdate);
    }

    [Fact]
    public void SolarAutoTurretHonorsTargetForcedHostilityToParent()
    {
        var neutralFaction = CreateFaction("neutral");

        var solar = new GameObject { Flags = GameObjectFlags.Exists, Kind = GameObjectKind.Solar };
        var turret = AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI / 2, maxRotation: MathF.PI / 2);
        solar.AddComponent(new SSolarComponent(solar) { Faction = neutralFaction });
        Assert.True(SAutoTurretComponent.TryAdd(solar));
        var controller = Assert.IsType<SAutoTurretComponent>(
            solar.GetComponent<SAutoTurretComponent>());

        var hostile = CreateTarget("hostile", neutralFaction, new Vector3(0, 0, -100));
        hostile.GetComponent<SRepComponent>()!.SetAttitude(solar, RepAttitude.Hostile);

        var selected = controller.FindTarget(turret, [hostile]);

        Assert.Same(hostile, selected);
    }

    [Fact]
    public void SolarAutoTurretFiresProjectilesAndQueuesNetworkSpawn()
    {
        var ownerFaction = CreateFaction("owner");
        var enemyFaction = CreateFaction("enemy");
        ownerFaction.Reputations[enemyFaction] = -1;

        var solar = new GameObject
        {
            Flags = GameObjectFlags.Exists,
            Kind = GameObjectKind.Solar,
            NetID = 12
        };
        AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI / 2, maxRotation: MathF.PI / 2,
            includeFireHardpoint: true, fireHardpointPosition: new Vector3(3, 0, -4));
        solar.AddComponent(new SSolarComponent(solar) { Faction = ownerFaction });
        Assert.True(SAutoTurretComponent.TryAdd(solar));

        var hostile = CreateTarget("hostile", enemyFaction, new Vector3(0, 0, -100));
        var resources = new ServerResourceManager(null, new FileSystem());
        using var world = new GameWorld(null, null, resources, null);
        world.AddObject(solar);
        world.AddObject(hostile);
        solar.Register(world);
        hostile.Register(world);

        world.Update(0.05);
        world.Update(0.05);

        var spawnedProjectile = false;
        foreach (var _ in world.Projectiles.Ids.GetAllocated())
        {
            spawnedProjectile = true;
            break;
        }
        Assert.True(spawnedProjectile);
        foreach (var projectileIndex in world.Projectiles.Ids.GetAllocated())
        {
            var projectile = world.Projectiles.Projectiles[projectileIndex];
            var expectedDirection = Vector3.Normalize(hostile.WorldTransform.Position - projectile.Start);
            Assert.True(Vector3.Dot(expectedDirection, Vector3.Normalize(projectile.Normal)) > 0.99999f);
            break;
        }
        var queued = Assert.Single(world.Projectiles.GetSpawnQueue());
        Assert.Equal(solar.NetID, queued.Owner.Value);
        Assert.NotEqual(0UL, queued.Guns);
    }

    [Fact]
    public void MixedSolarLoadoutKeepsAutoTurretProjectileIndex()
    {
        var solar = new GameObject
        {
            Flags = GameObjectFlags.Exists,
            Kind = GameObjectKind.Solar,
            NetID = 12
        };
        AddGun(solar, autoTurret: false, muzzleVelocity: 200, lifetime: 5,
            includeFireHardpoint: true, hardpointName: "HpWeapon01");
        var autoTurret = AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            includeFireHardpoint: true, hardpointName: "HpWeapon02");
        var weapons = new WeaponControlComponent(solar);
        solar.AddComponent(weapons);

        var resources = new ServerResourceManager(null, new FileSystem());
        using var world = new GameWorld(null, null, resources, null);
        world.AddObject(solar);
        solar.Register(world);

        Assert.True(autoTurret.Fire(new Vector3(0, 0, -100), world));
        var queued = Assert.Single(world.Projectiles.GetSpawnQueue());
        Assert.Equal(2UL, queued.Guns);

        // Static solar clients must construct the complete weapon list. If the
        // regular gun is omitted, bit 1 has no corresponding client weapon and
        // the auto-turret shot is silently discarded.
        Assert.Equal(2, weapons.NetOrderWeapons!.Length);
        Assert.Same(autoTurret, weapons.NetOrderWeapons[1]);
    }

    [Fact]
    public void NetworkSpawnPreservesEachTurretsOwnTarget()
    {
        var solar = new GameObject
        {
            Flags = GameObjectFlags.Exists,
            Kind = GameObjectKind.Solar,
            NetID = 12
        };
        var first = AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            includeFireHardpoint: true, hardpointName: "HpWeapon01");
        var second = AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            includeFireHardpoint: true, hardpointName: "HpWeapon02");
        solar.AddComponent(new WeaponControlComponent(solar));

        var resources = new ServerResourceManager(null, new FileSystem());
        using var world = new GameWorld(null, null, resources, null);
        world.AddObject(solar);
        solar.Register(world);

        var firstTarget = new Vector3(10, 0, -100);
        var secondTarget = new Vector3(-10, 0, -100);
        Assert.True(second.Fire(secondTarget, world));
        Assert.True(first.Fire(firstTarget, world));

        var queued = Assert.Single(world.Projectiles.GetSpawnQueue());
        Assert.Equal(3UL, queued.Guns);
        Assert.Equal(2UL, queued.Unique);
        Assert.Equal(firstTarget, queued.Target);
        Assert.Equal(secondTarget, Assert.Single(queued.OtherTargets));
    }

    [Fact]
    public void FullRotationTurretCanAcquireBehindItsMount()
    {
        var ship = new GameObject();
        var turret = AddGun(ship, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI, maxRotation: MathF.PI);

        Assert.True(turret.CanAimAt(new Vector3(0, 0, 100)));
    }

    [Fact]
    public void FullRotationTurretCrossesAngleWrapWithoutJumping()
    {
        var ship = new GameObject();
        var turret = AddGun(ship, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI * 2, maxRotation: MathF.PI * 2, turnRate: 20);
        turret.Parent.Attachment!.Revolve(MathHelper.DegreesToRadians(179));
        turret.RotateTowards(MathHelper.DegreesToRadians(-179), 0);
        using var world = new GameWorld(null, null, null, null, initPhys: false);

        turret.Update(0.05, world);
        turret.Update(0.05, world);

        Assert.True(turret.Angles.X > MathF.PI);
        Assert.True(MathF.Abs(turret.Angles.X - MathHelper.DegreesToRadians(181)) < 0.0001f);
    }

    [Fact]
    public void RevolvingTurretInvalidatesCachedWorldTransform()
    {
        var ship = new GameObject();
        var turret = AddGun(ship, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI, maxRotation: MathF.PI);
        var before = turret.Parent.WorldTransform;

        var target = new Vector3(100, 0, -100);
        turret.AimTowards(target, 1);
        var after = turret.Parent.WorldTransform;

        Assert.NotEqual(before.Orientation, after.Orientation);
        var forward = Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, after.Orientation));
        Assert.True(Vector3.Dot(forward, Vector3.Normalize(target - after.Position)) > 0.99999f);
    }

    [Fact]
    public void ProjectileStartsAtHpFireInsteadOfTurretOrSolarOrigin()
    {
        var solar = new GameObject { Flags = GameObjectFlags.Exists };
        solar.SetLocalTransform(new Transform3D(new Vector3(100, 200, 300), Quaternion.Identity));
        var turret = AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            includeFireHardpoint: true,
            mountPosition: new Vector3(10, 20, 30),
            fireHardpointPosition: new Vector3(1, 2, -3));

        var resources = new ServerResourceManager(null, new FileSystem());
        using var world = new GameWorld(null, null, resources, null);
        world.AddObject(solar);
        solar.Register(world);

        var expectedMuzzle = new Vector3(111, 222, 327);
        Assert.True(turret.Fire(expectedMuzzle - Vector3.UnitZ * 100, world));

        var found = false;
        foreach (var projectileIndex in world.Projectiles.Ids.GetAllocated())
        {
            Assert.Equal(expectedMuzzle, world.Projectiles.Projectiles[projectileIndex].Start);
            found = true;
            break;
        }
        Assert.True(found);
    }

    [Fact]
    public void AutoTurretWaitsForMuzzleAlignmentAndThenFiresExactlyAtStationaryTarget()
    {
        var ownerFaction = CreateFaction("owner");
        var enemyFaction = CreateFaction("enemy");
        ownerFaction.Reputations[enemyFaction] = -1;

        var solar = new GameObject
        {
            Flags = GameObjectFlags.Exists,
            Kind = GameObjectKind.Solar,
            NetID = 12
        };
        AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI / 2, maxRotation: MathF.PI / 2,
            includeFireHardpoint: true, turnRate: 20);
        solar.AddComponent(new SSolarComponent(solar) { Faction = ownerFaction });
        Assert.True(SAutoTurretComponent.TryAdd(solar));

        var hostile = CreateTarget("hostile", enemyFaction, new Vector3(100, 0, -100));
        var resources = new ServerResourceManager(null, new FileSystem());
        using var world = new GameWorld(null, null, resources, null);
        world.AddObject(solar);
        world.AddObject(hostile);
        solar.Register(world);
        hostile.Register(world);

        world.Update(0.05);
        world.Update(0.05);
        Assert.Empty(world.Projectiles.GetSpawnQueue());

        for (var i = 0; i < 60; i++)
            world.Update(0.05);

        Assert.NotEmpty(world.Projectiles.GetSpawnQueue());
        var found = false;
        foreach (var projectileIndex in world.Projectiles.Ids.GetAllocated())
        {
            var projectile = world.Projectiles.Projectiles[projectileIndex];
            var expectedDirection = Vector3.Normalize(hostile.WorldTransform.Position - projectile.Start);
            Assert.True(Vector3.Dot(expectedDirection, Vector3.Normalize(projectile.Normal)) > 0.99999f);
            found = true;
            break;
        }
        Assert.True(found);
    }

    [Fact]
    public void PilotAccuracyDoesNotMoveAutoTurretAimEveryFrame()
    {
        var solar = new GameObject { Flags = GameObjectFlags.Exists };
        var turret = AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            includeFireHardpoint: true);
        var target = new GameObject { Flags = GameObjectFlags.Exists };
        target.SetLocalTransform(new Transform3D(new Vector3(50, 25, -200), Quaternion.Identity));
        var settings = new GunBlock { FireAccuracyConeAngle = 30 };
        var controller = new SAutoTurretComponent(solar, () => settings);

        var first = controller.GetAimPosition(target, turret);
        for (var i = 0; i < 20; i++)
            Assert.Equal(first, controller.GetAimPosition(target, turret));
    }

    [Fact]
    public void AutoTurretYawAndBarrelReturnToRestAndSendFinalNetworkPose()
    {
        var ownerFaction = CreateFaction("owner");
        var enemyFaction = CreateFaction("enemy");
        ownerFaction.Reputations[enemyFaction] = -1;

        var solar = new GameObject { Flags = GameObjectFlags.Exists, Kind = GameObjectKind.Solar };
        var turret = AddGun(solar, autoTurret: true, muzzleVelocity: 200, lifetime: 5,
            minRotation: -MathF.PI / 2, maxRotation: MathF.PI / 2,
            includeFireHardpoint: true, includeBarrel: true, turnRate: 90);
        var solarComponent = new SSolarComponent(solar) { Faction = ownerFaction };
        solar.AddComponent(solarComponent);
        Assert.True(SAutoTurretComponent.TryAdd(solar));

        var hostile = CreateTarget("hostile", enemyFaction, new Vector3(50, 50, -100));
        var resources = new ServerResourceManager(null, new FileSystem());
        using var world = new GameWorld(null, null, resources, null);
        world.AddObject(solar);
        world.AddObject(hostile);
        solar.Register(world);
        hostile.Register(world);

        for (var i = 0; i < 10; i++)
            world.Update(0.05);

        var barrel = Assert.IsType<RevConstruct>(turret.Parent.Model!.RigidModel.AllParts[1].Construct);
        Assert.True(MathF.Abs(turret.Angles.X) > 0.1f);
        Assert.True(MathF.Abs(barrel.Current) > 0.1f);

        hostile.Flags &= ~GameObjectFlags.Exists;
        var previousPitch = MathF.Abs(barrel.Current);
        world.Update(0.05);
        Assert.True(MathF.Abs(barrel.Current) < previousPitch);
        Assert.True(solarComponent.SendAutoTurretUpdate);

        for (var i = 0; i < 20 && MathF.Abs(barrel.Current) > 0.0001f; i++)
            world.Update(0.05);

        Assert.True(MathF.Abs(turret.Angles.X) <= 0.0001f);
        Assert.True(MathF.Abs(barrel.Current) <= 0.0001f);
        Assert.True(solarComponent.SendAutoTurretUpdate);

        world.Update(0.05);
        Assert.False(solarComponent.SendAutoTurretUpdate);
    }

    private static Faction CreateFaction(string nickname) => new()
    {
        Nickname = nickname,
        Properties = null
    };

    private static GameObject CreateTarget(string nickname, Faction faction, Vector3 position)
    {
        var target = new GameObject
        {
            Nickname = nickname,
            Flags = GameObjectFlags.Exists,
            Kind = GameObjectKind.Ship
        };
        target.SetLocalTransform(new Transform3D(position, Quaternion.Identity));
        target.AddComponent(new SRepComponent(target) { Faction = faction });
        return target;
    }

    private static GunComponent AddGun(GameObject ship, bool autoTurret, float muzzleVelocity, float lifetime,
        float minRotation = -MathF.PI / 2, float maxRotation = MathF.PI / 2,
        bool includeFireHardpoint = false, string hardpointName = "HpWeapon",
        Vector3? mountPosition = null, Vector3? fireHardpointPosition = null,
        bool includeBarrel = false, float turnRate = 360)
    {
        var part = new RigidModelPart();
        var hardpoint = new Hardpoint(new RevoluteHardpointDefinition(hardpointName)
        {
            Axis = Vector3.UnitY,
            Min = minRotation,
            Max = maxRotation,
            Position = mountPosition ?? Vector3.Zero
        }, part);
        var gunObject = new GameObject
        {
            Parent = ship,
            Attachment = hardpoint
        };
        if (includeFireHardpoint || includeBarrel)
        {
            var root = new RigidModelPart
            {
                Name = "Root",
                Children = includeBarrel ? [] : null
            };
            var firePart = root;
            RigidModelPart[] allParts;
            if (includeBarrel)
            {
                var barrel = new RigidModelPart
                {
                    Name = "Barrel",
                    Construct = new RevConstruct
                    {
                        ParentName = "Root",
                        ChildName = "Barrel",
                        Rotation = Quaternion.Identity,
                        AxisRotation = Vector3.UnitX,
                        Min = -MathF.PI / 2,
                        Max = MathF.PI / 2
                    }
                };
                root.Children!.Add(barrel);
                firePart = barrel;
                allParts = [root, barrel];
            }
            else
            {
                allParts = [root];
            }

            if (includeFireHardpoint)
            {
                firePart.Hardpoints.Add(new Hardpoint(new FixedHardpointDefinition("HpFire01")
                {
                    Position = fireHardpointPosition ?? Vector3.Zero
                }, firePart));
            }
            var model = new RigidModel
            {
                Root = root,
                AllParts = allParts,
                Parts = new ModelPartCollection(),
                Source = allParts.Length == 1 ? RigidModelSource.SinglePart : RigidModelSource.Compound
            };
            foreach (var modelPart in allParts)
                model.Parts.Add(modelPart);
            model.UpdateTransform();
            gunObject.Model = new DestructibleModel(model, []);
        }
        var equipment = new GunEquipment
        {
            Nickname = autoTurret ? "auto_turret" : "regular_gun",
            Def = new GunSchema
            {
                AutoTurret = autoTurret,
                MuzzleVelocity = muzzleVelocity,
                TurnRate = turnRate
            },
            Munition = new MunitionEquip
            {
                Def = new Munition { Lifetime = lifetime }
            }
        };
        var gun = new GunComponent(gunObject, equipment);
        gunObject.AddComponent(gun);
        ship.Children.Add(gunObject);
        return gun;
    }
}
