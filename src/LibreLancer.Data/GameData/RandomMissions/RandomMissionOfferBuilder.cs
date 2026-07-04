using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.MBases;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.Data.GameData.RandomMissions;

public sealed class RandomMissionOffer
{
    public required Base Base;
    public required BaseNpc Npc;
    public required MBaseBaseFaction BaseFaction;
    public required BaseMissionOffer Mission;
    public required List<Zone> EligibleZones;

    public string MissionType => Mission.Type;
    public Faction? Faction => BaseFaction.Faction;
    public float Weight => Mission.Weight;
}

public static class RandomMissionOfferBuilder
{
    public static List<RandomMissionOffer> GetOffers(
        Base sourceBase,
        StarSystem? sourceSystem,
        float? difficulty = null,
        string? roomNickname = null,
        IReadOnlyCollection<string>? allowableZoneTypes = null)
    {
        if (sourceSystem == null ||
            (sourceBase.MinMissionOffers <= 0 && sourceBase.MaxMissionOffers <= 0) ||
            sourceBase.BaseFactions.Count == 0)
        {
            return [];
        }

        var missionNpcs = GetMissionNpcs(sourceBase, roomNickname).ToList();
        if (missionNpcs.Count == 0)
            return [];

        var offers = new List<RandomMissionOffer>();
        foreach (var fac in sourceBase.BaseFactions)
        {
            if (fac.Missions.Count == 0)
                continue;

            foreach (var mission in fac.Missions)
            {
                if (string.IsNullOrWhiteSpace(mission.Type) ||
                    !DifficultyMatches(mission.MinDiff, mission.MaxDiff, difficulty))
                {
                    continue;
                }

                var zones = GetEligibleZones(sourceSystem, mission.Type, fac.Faction, allowableZoneTypes);
                if (zones.Count == 0)
                    continue;

                foreach (var npc in missionNpcs)
                {
                    if (!FactionCanUseNpc(fac, npc) ||
                        !NpcMissionMatches(npc.Mission, mission.Type, difficulty))
                    {
                        continue;
                    }

                    offers.Add(new RandomMissionOffer
                    {
                        Base = sourceBase,
                        Npc = npc,
                        BaseFaction = fac,
                        Mission = mission,
                        EligibleZones = zones,
                    });
                }
            }
        }

        return offers;
    }

    public static List<Zone> GetEligibleZones(
        StarSystem sourceSystem,
        string missionType,
        IReadOnlyCollection<string>? allowableZoneTypes = null)
    {
        return GetEligibleZones(sourceSystem, missionType, null, allowableZoneTypes);
    }

    private static List<Zone> GetEligibleZones(
        StarSystem sourceSystem,
        string missionType,
        Faction? faction,
        IReadOnlyCollection<string>? allowableZoneTypes = null)
    {
        var zoneGroup = MissionZoneGroup(faction?.Properties?.Legality);
        return sourceSystem.Zones
            .Where(z => IsVignetteZone(z) &&
                        MissionTypeMatches(z.MissionType, missionType, zoneGroup) &&
                        ZoneTypeMatches(z.VignetteType, allowableZoneTypes))
            .ToList();
    }

    private static IEnumerable<BaseNpc> GetMissionNpcs(Base sourceBase, string? roomNickname)
    {
        IEnumerable<BaseRoom> rooms = sourceBase.Rooms;
        if (!string.IsNullOrEmpty(roomNickname))
            rooms = rooms.Where(r => r.Nickname?.Equals(roomNickname, StringComparison.OrdinalIgnoreCase) == true);
        return rooms.SelectMany(r => r.Npcs).Where(n => n.Mission != null);
    }

    private static bool FactionCanUseNpc(MBaseBaseFaction faction, BaseNpc npc) =>
        faction.Npcs.Count == 0 ||
        faction.Npcs.Contains(npc.Nickname, StringComparer.OrdinalIgnoreCase);

    private static bool NpcMissionMatches(NpcMission? npcMission, string missionType, float? difficulty) =>
        npcMission != null &&
        npcMission.Kind.Equals(missionType, StringComparison.OrdinalIgnoreCase) &&
        DifficultyMatches(npcMission.Min, npcMission.Max, difficulty);

    private static bool MissionTypeMatches(string[]? supportedTypes, string missionType, Legality? legality = null) =>
        supportedTypes is { Length: > 0 } &&
        (supportedTypes.Contains(missionType, StringComparer.OrdinalIgnoreCase) ||
         (legality != null && supportedTypes.Contains(legality.ToString(), StringComparer.OrdinalIgnoreCase)));

    private static bool IsVignetteZone(Zone zone) =>
        !string.IsNullOrEmpty(zone.VignetteType);

    private static bool ZoneTypeMatches(string? vignetteType, IReadOnlyCollection<string>? allowableZoneTypes) =>
        allowableZoneTypes == null ||
        allowableZoneTypes.Count == 0 ||
        (!string.IsNullOrEmpty(vignetteType) &&
         allowableZoneTypes.Contains(vignetteType, StringComparer.OrdinalIgnoreCase));

    private static bool DifficultyMatches(float min, float max, float? difficulty) =>
        difficulty == null ||
        (difficulty.Value >= min && difficulty.Value <= max);

    private static Legality? MissionZoneGroup(Legality? offeringFactionLegality) =>
        offeringFactionLegality switch
        {
            Legality.Lawful => Legality.Unlawful,
            Legality.Unlawful => Legality.Lawful,
            _ => null,
        };
}
