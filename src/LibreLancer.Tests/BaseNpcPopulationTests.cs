using System;
using System.Linq;
using LibreLancer.Client;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.GCS;
using LibreLancer.Data.Schema.MBases;
using LibreLancer.Data.Schema.Save;
using LibreLancer.World;
using Xunit;

namespace LibreLancer.Tests;

public class BaseNpcPopulationTests
{
    [Fact]
    public void FixedFixturesKeepTheirConfiguredMarkerAndIgnoreDensity()
    {
        var fixedNpc = new BaseNpc("bartender")
        {
            Placement = new BaseNpcPlacement("Zs/NPC/01/01/A/Stand", Script("fixed"), "bartender")
        };
        var ambient = new BaseNpc("ambient");
        var room = Room(0, fixedNpc, ambient);
        var spots = new[]
        {
            new RoomNpcSpot("Zs/NPC/01/01/A/Stand", false, Posture.Stand),
            new RoomNpcSpot("Zg/NPC/01/02/A/Stand", true, Posture.Stand)
        };

        var result = BaseNpcPopulation.Select(room, spots, (_, _) => null, new Random(42));

        var assignment = Assert.Single(result);
        Assert.True(assignment.Fixed);
        Assert.Equal("bartender", assignment.Npc.Nickname);
        Assert.Equal("Zs/NPC/01/01/A/Stand", assignment.SceneSpot);
    }

    [Fact]
    public void AmbientAssignmentsRerollWithRoomEntrySeed()
    {
        var room = Room(3, new BaseNpc("one"), new BaseNpc("two"), new BaseNpc("three"));
        var spots = new[]
        {
            new RoomNpcSpot("Zg/NPC/01/01/A/Stand", true, Posture.Stand),
            new RoomNpcSpot("Zg/NPC/01/02/A/Stand", true, Posture.Stand),
            new RoomNpcSpot("Zg/NPC/01/03/A/Stand", true, Posture.Stand)
        };

        var signatures = Enumerable.Range(1, 8)
            .Select(seed => string.Join(",", BaseNpcPopulation
                .Select(room, spots, (_, _) => null, new Random(seed))
                .Select(x => $"{x.Npc.Nickname}@{x.SceneSpot}")))
            .Distinct()
            .ToArray();

        Assert.True(signatures.Length > 1);
    }

    [Fact]
    public void DensityLimitsOnlyAmbientAndDoesNotDuplicateSpotsOrNpcs()
    {
        var fixedNpc = new BaseNpc("vendor")
        {
            Placement = new BaseNpcPlacement("Zs/NPC/01/01/A/Stand", Script("fixed"), "trader")
        };
        var room = Room(1, fixedNpc, new BaseNpc("one"), new BaseNpc("two"));
        var spots = new[]
        {
            new RoomNpcSpot("Zs/NPC/01/01/A/Stand", false, Posture.Stand),
            new RoomNpcSpot("Zg/NPC/01/02/A/Stand", true, Posture.Stand),
            new RoomNpcSpot("Zg/NPC/01/03/A/Stand", true, Posture.Stand)
        };

        var result = BaseNpcPopulation.Select(room, spots, (_, _) => null, new Random(7));

        Assert.InRange(result.Length, 1, 2);
        Assert.Equal(result.Length, result.Select(x => x.Npc.Nickname).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(result.Length, result.Select(x => x.SceneSpot).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(result, x => x.Fixed && x.Npc.Nickname == "vendor");
    }

    [Fact]
    public void AmbientSlotsCanBeEmptyWhileFixturesRemainPresent()
    {
        var fixedNpc = new BaseNpc("vendor")
        {
            Placement = new BaseNpcPlacement("Zs/NPC/01/01/A/Stand", Script("fixed"), "trader")
        };
        var room = Room(3, fixedNpc, new BaseNpc("one"), new BaseNpc("two"), new BaseNpc("three"));
        var spots = new[]
        {
            new RoomNpcSpot("Zs/NPC/01/01/A/Stand", false, Posture.Stand),
            new RoomNpcSpot("Zg/NPC/01/02/A/Stand", true, Posture.Stand),
            new RoomNpcSpot("Zg/NPC/01/03/A/Stand", true, Posture.Stand),
            new RoomNpcSpot("Zg/NPC/01/04/A/Stand", true, Posture.Stand)
        };

        var counts = Enumerable.Range(1, 32)
            .Select(seed => BaseNpcPopulation
                .Select(room, spots, (_, _) => null, new Random(seed))
                .Count(x => !x.Fixed))
            .ToArray();

        Assert.Contains(0, counts);
        Assert.Contains(3, counts);
    }

    [Fact]
    public void OnlyOneSpawnedNpcCanOfferABribeForEachFaction()
    {
        var liberty = MakeFaction("li_police");
        var corsairs = MakeFaction("corsairs");
        var first = Briber("first", liberty);
        var second = Briber("second", liberty);
        var third = Briber("third", corsairs);
        var assignments = FixedAssignments(first, second, third);

        var selected = BaseNpcPopulation.SelectBribes(
            assignments,
            new ReputationCollection(),
            new Random(42));

        Assert.Equal(2, selected.Count);
        Assert.Single(selected.Values.Where(x => x.Faction == liberty));
        Assert.Single(selected.Values.Where(x => x.Faction == corsairs));
    }

    [Fact]
    public void AlliedFactionBribesAreNotSelected()
    {
        var liberty = MakeFaction("li_police");
        var corsairs = MakeFaction("corsairs");
        var assignments = FixedAssignments(
            Briber("police_contact", liberty),
            Briber("corsair_contact", corsairs));
        var reputations = new ReputationCollection();
        reputations.Reputations[liberty] = Faction.FriendlyThreshold;

        var selected = BaseNpcPopulation.SelectBribes(assignments, reputations, new Random(42));

        Assert.DoesNotContain(selected.Values, x => x.Faction == liberty);
        Assert.Single(selected.Values, x => x.Faction == corsairs);
    }

    [Theory]
    [InlineData("trader", "trader")]
    [InlineData("Equipment", "Equipment")]
    [InlineData("ShipDealer", "ShipDealer")]
    [InlineData("bartender", null)]
    [InlineData(null, null)]
    public void ServiceActionsDispatchOnlyConfiguredDealerActions(string? configured, string? expected)
    {
        var npc = new BaseNpc("npc");
        if (configured != null)
            npc.Placement = new BaseNpcPlacement("spot", Script("fixed"), configured);

        Assert.Equal(expected, BaseNpcPopulation.GetServiceAction(npc));
    }

    [Theory]
    [InlineData("trader", false, "talk_commodity_dealer")]
    [InlineData("Equipment", false, "talk_equip_dealer")]
    [InlineData("ShipDealer", false, "talk_ship_dealer")]
    [InlineData(null, true, "talk_mission")]
    [InlineData(null, false, "talk_blowoff")]
    public void InteractionCursorMatchesConfiguredService(string? action, bool hasMissionOffer, string expected)
    {
        var npc = new BaseNpc("npc");
        if (action != null)
            npc.Placement = new BaseNpcPlacement("spot", Script("fixed"), action);
        if (hasMissionOffer)
            npc.Mission = new NpcMission("DestroyMission", 0, 1);

        Assert.Equal(expected, BaseNpcPopulation.GetInteractionCursor(npc, hasMissionOffer));
    }

    [Fact]
    public void InteractionCursorPrioritizesBribesOverKnowledgeAndRumors()
    {
        var npc = new BaseNpc("npc")
        {
            Know = { new NpcKnow(1, 2, 0, 0) },
            Rumors = { new BaseNpcRumor { Ids = 3 } },
            Bribes =
            {
                new BaseNpcBribe
                {
                    Faction = new Faction { Nickname = "faction", Properties = null },
                    Ids = 4
                }
            }
        };

        Assert.Equal("talk_bribe", BaseNpcPopulation.GetInteractionCursor(npc, false));
    }

    [Fact]
    public void SelectRandomRumorReturnsOnlyRumorsFromTheNpc()
    {
        var expected = new BaseNpcRumor { Ids = 101 };
        var npc = new BaseNpc("npc")
        {
            Rumors = { new BaseNpcRumor { Ids = 0 }, expected, new BaseNpcRumor { Ids = 202 } }
        };

        var selected = BaseNpcPopulation.SelectRandomRumor(npc, new Random(42));

        Assert.NotNull(selected);
        Assert.Contains(selected, npc.Rumors);
        Assert.NotEqual(0, selected!.Ids);
    }

    [Fact]
    public void RoomPopulationSelectsOneRumorForEachNpcWhenItLoads()
    {
        var npc = new BaseNpc("npc")
        {
            Placement = new BaseNpcPlacement("spot", Script("fixed"), "bartender"),
            Rumors = { new BaseNpcRumor { Ids = 101 }, new BaseNpcRumor { Ids = 202 } }
        };
        var spots = new[] { new RoomNpcSpot("spot", false, Posture.Stand) };

        var assignment = Assert.Single(
            BaseNpcPopulation.Select(Room(0, npc), spots, (_, _) => null, new Random(42)));

        Assert.NotNull(assignment.SelectedRumor);
        Assert.Contains(assignment.SelectedRumor, npc.Rumors);
    }

    [Fact]
    public void RumorCursorUsesTheRumorSelectedForTheLoadedNpc()
    {
        var npc = new BaseNpc("npc")
        {
            Rumors = { new BaseNpcRumor { Ids = 101 } }
        };

        Assert.Equal(
            "talk_rumor",
            BaseNpcPopulation.GetInteractionCursor(npc, false, npc.Rumors[0]));
        Assert.Equal(
            "talk_blowoff",
            BaseNpcPopulation.GetInteractionCursor(npc, false, null));
    }

    [Fact]
    public void LegacySaveWithoutNpcStateStillLoadsAndInteractionStateRoundTrips()
    {
        var legacy = SaveGame.FromString("legacy", "[player]\n");
        Assert.Null(legacy.MPlayer);

        var save = SaveGame.FromString(
            "npc-state", "[mplayer]\nrumor = 123, 1\nvnpc = 456, 789, 2, 0\n");

        Assert.NotNull(save.MPlayer);
        Assert.Single(save.MPlayer!.Rumors);
        Assert.Single(save.MPlayer.VNPCs);
        Assert.Equal(new HashValue(123), save.MPlayer.Rumors[0].Item);
        Assert.Equal(new HashValue(456), save.MPlayer.VNPCs[0].ItemA);
    }

    private static BaseRoom Room(int density, params BaseNpc[] npcs) => new()
    {
        SourceFile = "test.ini",
        MaxCharacters = density,
        Npcs = npcs.ToList()
    };

    private static ResolvedThn Script(string name) => new()
    {
        DataPath = name,
        SourcePath = name,
        ReadCallback = null!,
        VFS = null!
    };

    private static BaseNpcAssignment[] FixedAssignments(params BaseNpc[] npcs)
    {
        return npcs.Select((npc, index) => new BaseNpcAssignment(
            npc,
            new RoomNpcSpot($"spot{index}", false, Posture.Stand),
            null,
            true,
            null)).ToArray();
    }

    private static BaseNpc Briber(string nickname, Faction faction) => new(nickname)
    {
        Bribes = { new BaseNpcBribe { Faction = faction, Ids = 1, Price = 1000 } }
    };

    private static Faction MakeFaction(string nickname) => new()
    {
        Nickname = nickname,
        Properties = null
    };
}
