using LibreLancer.Missions;
using Xunit;

namespace LibreLancer.Tests;

public class MissionLabelTests
{
    [Fact]
    public void AnyAliveRemainsTrueUntilLastSpawnedShipIsDestroyed()
    {
        var label = new MissionLabel("enemies", ["enemy1", "enemy2"]);
        label.Spawned("enemy1");
        label.Spawned("enemy2");

        label.Destroyed("enemy1");
        Assert.True(label.AnyAlive());

        label.Destroyed("enemy2");
        Assert.False(label.AnyAlive());
    }

    [Fact]
    public void AllKilledOnlyConsidersSpawnedLabelMembers()
    {
        var label = new MissionLabel("enemies", ["enemy1", "enemy2"]);
        label.Spawned("enemy1");
        label.Destroyed("enemy1");

        Assert.True(label.IsAllKilled());

        label.Spawned("enemy2");
        Assert.False(label.IsAllKilled());

        label.Destroyed("enemy2");
        Assert.True(label.IsAllKilled());
    }

    [Fact]
    public void AllKilledRequiresAtLeastOneSpawnedMember()
    {
        var label = new MissionLabel("enemies", ["enemy1", "enemy2"]);

        Assert.False(label.IsAllKilled());
    }
}
