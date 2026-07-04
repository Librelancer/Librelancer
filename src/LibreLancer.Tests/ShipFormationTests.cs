using LibreLancer.World;
using Xunit;

namespace LibreLancer.Tests;

public class ShipFormationTests
{
    [Fact]
    public void RemovingLeaderWithMultipleFollowersPromotesSurvivor()
    {
        var lead = Ship();
        var followerA = Ship();
        var followerB = Ship();
        var formation = Form(lead, followerA, followerB);

        formation.Remove(lead);

        Assert.Same(followerA, formation.LeadShip);
        Assert.Single(formation.Followers);
        Assert.Same(followerB, formation.Followers[0]);
        Assert.DoesNotContain(lead, formation.Followers);
        Assert.Same(formation, formation.LeadShip.Formation);
        Assert.All(formation.Followers, follower => Assert.Same(formation, follower.Formation));
        Assert.Null(lead.Formation);
    }

    [Fact]
    public void RemovingLeaderWithOneFollowerDisbandsFormation()
    {
        var lead = Ship();
        var follower = Ship();
        var formation = Form(lead, follower);

        formation.Remove(lead);

        Assert.Null(lead.Formation);
        Assert.Null(follower.Formation);
        Assert.Empty(formation.Followers);
    }

    [Fact]
    public void RemovingLastFollowerDisbandsFormation()
    {
        var lead = Ship();
        var follower = Ship();
        var formation = Form(lead, follower);

        formation.Remove(follower);

        Assert.Null(lead.Formation);
        Assert.Null(follower.Formation);
        Assert.Empty(formation.Followers);
    }

    [Fact]
    public void NetFormationOmitsDestroyedFollowers()
    {
        var lead = Ship();
        var liveFollower = Ship();
        liveFollower.NetID = 7;
        liveFollower.Flags = GameObjectFlags.Exists;
        var destroyedFollower = Ship();
        destroyedFollower.NetID = 8;
        var formation = Form(lead, liveFollower, destroyedFollower);

        var net = formation.ToNetFormation(lead);

        Assert.True(net.Exists);
        Assert.Equal([7], net.Followers);
    }

    private static GameObject Ship() => new();

    private static ShipFormation Form(GameObject lead, params GameObject[] followers)
    {
        var formation = new ShipFormation(lead, followers);
        lead.Formation = formation;
        foreach (var follower in followers)
            follower.Formation = formation;
        return formation;
    }
}
