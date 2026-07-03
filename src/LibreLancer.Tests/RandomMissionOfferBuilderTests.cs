using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.MBases;
using Xunit;

namespace LibreLancer.Tests;

public class RandomMissionOfferBuilderTests
{
    [Fact]
    public void ReturnsOfferWhenBaseNpcFactionAndZoneMatch()
    {
        var sourceBase = CreateBase();
        var system = CreateSystem();

        var offers = RandomMissionOfferBuilder.GetOffers(sourceBase, system, 0.1f);

        Assert.Single(offers);
        Assert.Equal("DestroyMission", offers[0].MissionType);
        Assert.Equal("mission_npc", offers[0].Npc.Nickname);
        Assert.Single(offers[0].EligibleZones);
    }

    [Fact]
    public void RejectsOfferWhenNpcDifficultyDoesNotMatch()
    {
        var sourceBase = CreateBase();
        var system = CreateSystem();

        var offers = RandomMissionOfferBuilder.GetOffers(sourceBase, system, 0.5f);

        Assert.Empty(offers);
    }

    [Fact]
    public void AllowsOfferWhenOnlyMaxMissionOffersIsPositive()
    {
        var sourceBase = CreateBase();
        sourceBase.MinMissionOffers = 0;
        var system = CreateSystem();

        var offers = RandomMissionOfferBuilder.GetOffers(sourceBase, system, 0.1f);

        Assert.Single(offers);
    }

    [Fact]
    public void RejectsOfferWhenSystemHasNoEligibleMissionZone()
    {
        var sourceBase = CreateBase();
        var system = CreateSystem();
        system.Zones[0].MissionEligible = false;

        var offers = RandomMissionOfferBuilder.GetOffers(sourceBase, system, 0.1f);

        Assert.Empty(offers);
    }

    [Fact]
    public void FiltersByRoomNickname()
    {
        var sourceBase = CreateBase();
        var system = CreateSystem();

        var offers = RandomMissionOfferBuilder.GetOffers(sourceBase, system, 0.1f, "deck");

        Assert.Empty(offers);
    }

    [Fact]
    public void FiltersByAllowableZoneTypes()
    {
        var sourceBase = CreateBase();
        var system = CreateSystem();

        var offers = RandomMissionOfferBuilder.GetOffers(
            sourceBase,
            system,
            0.1f,
            allowableZoneTypes: ["exclusion"]);

        Assert.Empty(offers);
    }

    private static Base CreateBase()
    {
        var sourceBase = new Base
        {
            Nickname = "test_base",
            MinMissionOffers = 1,
            MaxMissionOffers = 3,
        };
        var bar = new BaseRoom { Nickname = "bar", CRC = 1, SourceFile = "bar.ini" };
        bar.Npcs.Add(new BaseNpc("mission_npc")
        {
            Mission = new NpcMission("DestroyMission", 0, 0.2f),
        });
        sourceBase.Rooms.Add(bar);
        sourceBase.Rooms.Add(new BaseRoom { Nickname = "deck", CRC = 2, SourceFile = "deck.ini" });
        sourceBase.BaseFactions.Add(new MBaseBaseFaction
        {
            Npcs = ["mission_npc"],
            Missions =
            [
                new BaseMissionOffer
                {
                    Type = "DestroyMission",
                    MinDiff = 0,
                    MaxDiff = 0.2f,
                    Weight = 30,
                }
            ],
        });
        return sourceBase;
    }

    private static StarSystem CreateSystem()
    {
        var system = new StarSystem { Nickname = "test_system", SourceFile = "test.ini" };
        system.Zones.Add(new Zone
        {
            Nickname = "mission_zone",
            MissionEligible = true,
            MissionType = ["DestroyMission"],
            VignetteType = "open",
        });
        return system;
    }
}
