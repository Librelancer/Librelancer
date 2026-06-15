using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Client;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using Xunit;

namespace LibreLancer.Tests;

public class NavmapBestPathPlannerTests
{
    [Fact]
    public void SameSystemWithoutUsefulLane_UsesFinalWaypointOnly()
    {
        var system = MakeSystem("sys");
        AddTradelane(system, "lane_start", new Vector3(100, 0, 0), "lane_end", new Vector3(300, 0, 0));
        var visited = Visited(system, "lane_start", "lane_end");

        var route = NavmapBestPathPlanner.Compute(
            [system],
            system,
            Vector3.Zero,
            system,
            new Vector3(400, 0, 0),
            visited.Contains,
            300f);

        Assert.Single(route);
        Assert.Equal(UserWaypointKind.ManualDestination, route[0].Kind);
    }

    [Fact]
    public void SameSystemWithUsefulLane_AddsTradelaneEntry()
    {
        var system = MakeSystem("sys");
        AddTradelane(system, "lane_start", Vector3.Zero, "lane_end", new Vector3(10000, 0, 0));
        var visited = Visited(system, "lane_start", "lane_end");

        var route = NavmapBestPathPlanner.Compute(
            [system],
            system,
            Vector3.Zero,
            system,
            new Vector3(10100, 0, 0),
            visited.Contains,
            300f);

        Assert.Equal(3, route.Count);
        Assert.Equal(UserWaypointKind.TradelaneEntry, route[0].Kind);
        Assert.Equal(FLHash.CreateID("lane_start"), route[0].TargetObjectHash);
        Assert.Equal(UserWaypointKind.TradelaneExit, route[1].Kind);
        Assert.Equal(FLHash.CreateID("lane_end"), route[1].TargetObjectHash);
        Assert.Equal(UserWaypointKind.ManualDestination, route[2].Kind);
    }

    [Fact]
    public void SameSystemWithMiddleEntry_UsesNearestTradelaneRing()
    {
        var system = MakeSystem("sys");
        AddTradelaneChain(system,
            ("lane_a", new Vector3(0, 0, 0)),
            ("lane_b", new Vector3(5000, 0, 0)),
            ("lane_c", new Vector3(10000, 0, 0)));
        var visited = Visited(system, "lane_a", "lane_b", "lane_c");

        var route = NavmapBestPathPlanner.Compute(
            [system],
            system,
            new Vector3(4800, 0, 0),
            system,
            new Vector3(10200, 0, 0),
            visited.Contains,
            300f);

        Assert.Equal(UserWaypointKind.TradelaneEntry, route[0].Kind);
        Assert.Equal(FLHash.CreateID("lane_b"), route[0].TargetObjectHash);
        Assert.Equal(UserWaypointKind.TradelaneExit, route[1].Kind);
        Assert.Equal(FLHash.CreateID("lane_c"), route[1].TargetObjectHash);
    }

    [Fact]
    public void MultiSystemRoute_UsesFastestWeightedJumpPath()
    {
        var systemA = MakeSystem("sys_a");
        var systemB = MakeSystem("sys_b");
        var systemC = MakeSystem("sys_c");

        AddJump(systemA, "jump_ab", Vector3.Zero, "sys_b", "exit_ab");
        AddJump(systemB, "exit_ab", Vector3.Zero, "sys_a", "jump_ab");
        AddJump(systemA, "jump_ac", Vector3.Zero, "sys_c", "exit_ac");
        AddJump(systemC, "exit_ac", Vector3.Zero, "sys_a", "jump_ac");
        AddJump(systemC, "jump_cb", Vector3.Zero, "sys_b", "exit_cb");
        AddJump(systemB, "exit_cb", new Vector3(30000, 0, 0), "sys_c", "jump_cb");

        var visited = Visited(systemA, "jump_ab", "jump_ac");
        visited.Add(systemB.CRC);
        visited.Add(systemC.CRC);
        visited.Add(FLHash.CreateID("exit_ab"));
        visited.Add(FLHash.CreateID("exit_ac"));
        visited.Add(FLHash.CreateID("jump_cb"));
        visited.Add(FLHash.CreateID("exit_cb"));

        var route = NavmapBestPathPlanner.Compute(
            [systemA, systemB, systemC],
            systemA,
            Vector3.Zero,
            systemB,
            new Vector3(30000, 0, 0),
            visited.Contains,
            300f);

        Assert.Equal(3, route.Count);
        Assert.Equal(FLHash.CreateID("jump_ac"), route[0].TargetObjectHash);
        Assert.Equal(FLHash.CreateID("jump_cb"), route[1].TargetObjectHash);
        Assert.Equal(UserWaypointKind.ManualDestination, route[2].Kind);
    }

    [Fact]
    public void UnvisitedIntermediateSystem_IsNotUsed()
    {
        var systemA = MakeSystem("sys_a");
        var systemB = MakeSystem("sys_b");
        var systemC = MakeSystem("sys_c");

        AddJump(systemA, "jump_ac", Vector3.Zero, "sys_c", "exit_ac");
        AddJump(systemC, "exit_ac", Vector3.Zero, "sys_a", "jump_ac");
        AddJump(systemC, "jump_cb", Vector3.Zero, "sys_b", "exit_cb");
        AddJump(systemB, "exit_cb", Vector3.Zero, "sys_c", "jump_cb");

        var visited = Visited(systemA, "jump_ac");
        visited.Add(systemB.CRC);
        visited.Add(FLHash.CreateID("exit_cb"));

        var route = NavmapBestPathPlanner.Compute(
            [systemA, systemB, systemC],
            systemA,
            Vector3.Zero,
            systemB,
            Vector3.Zero,
            visited.Contains,
            300f);

        Assert.Empty(route);
    }

    private static StarSystem MakeSystem(string nickname) => new()
    {
        Nickname = nickname,
        CRC = FLHash.CreateID(nickname),
        SourceFile = ""
    };

    private static void AddJump(StarSystem system, string nickname, Vector3 position, string targetSystem, string exit)
    {
        system.Objects.Add(new SystemObject
        {
            Nickname = nickname,
            Position = position,
            Dock = new DockAction
            {
                Kind = DockKinds.Jump,
                Target = targetSystem,
                Exit = exit
            }
        });
    }

    private static void AddTradelane(StarSystem system, string startNickname, Vector3 startPosition, string endNickname,
        Vector3 endPosition)
    {
        system.Objects.Add(new SystemObject
        {
            Nickname = startNickname,
            Position = startPosition,
            Dock = new DockAction
            {
                Kind = DockKinds.Tradelane,
                Target = endNickname,
                TargetLeft = null
            }
        });
        system.Objects.Add(new SystemObject
        {
            Nickname = endNickname,
            Position = endPosition,
            Dock = new DockAction
            {
                Kind = DockKinds.Tradelane,
                Target = null,
                TargetLeft = "used"
            }
        });
    }

    private static void AddTradelaneChain(StarSystem system, params (string Nickname, Vector3 Position)[] rings)
    {
        for (var i = 0; i < rings.Length; i++)
        {
            system.Objects.Add(new SystemObject
            {
                Nickname = rings[i].Nickname,
                Position = rings[i].Position,
                Dock = new DockAction
                {
                    Kind = DockKinds.Tradelane,
                    Target = i < rings.Length - 1 ? rings[i + 1].Nickname : null,
                    TargetLeft = i > 0 ? rings[i - 1].Nickname : null
                }
            });
        }
    }

    private static HashSet<uint> Visited(StarSystem system, params string[] objectNicknames)
    {
        var visited = new HashSet<uint> { system.CRC };
        foreach (var nickname in objectNicknames)
            visited.Add(FLHash.CreateID(nickname));
        return visited;
    }
}
