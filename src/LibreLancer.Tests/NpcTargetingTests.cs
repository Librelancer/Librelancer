using LibreLancer.Data.Schema.Pilots;
using LibreLancer.Data.Schema.Ships;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using Xunit;
using GameDataShip = LibreLancer.Data.GameData.Ship;

namespace LibreLancer.Tests;

public class NpcTargetingTests
{
    [Theory]
    [InlineData(ShipType.Fighter, AttackTarget.Fighter)]
    [InlineData(ShipType.Freighter, AttackTarget.Freighter)]
    [InlineData(ShipType.Gunboat, AttackTarget.Gunboat)]
    [InlineData(ShipType.Cruiser, AttackTarget.Cruiser)]
    [InlineData(ShipType.Transport, AttackTarget.Transport)]
    [InlineData(ShipType.Capital, AttackTarget.Capital)]
    [InlineData(ShipType.Mining, AttackTarget.Anything)]
    public void ClassifiesShipAttackTargets(ShipType shipType, AttackTarget expected)
    {
        var obj = new GameObject();
        obj.AddComponent(new ShipComponent(new GameDataShip { ShipType = shipType }, obj));

        Assert.Equal(expected, SNPCComponent.ClassifyAttackTarget(obj));
    }
}
