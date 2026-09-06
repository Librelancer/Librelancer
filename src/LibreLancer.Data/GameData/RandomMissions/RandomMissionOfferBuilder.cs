using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        Base sourceBase, StarSystem? sourceSystem, float? difficulty = null,
        string? roomNickname = null, IReadOnlyCollection<string>? allowableZoneTypes = null)
    {
        if (sourceSystem == null ||
            (sourceBase.MinMissionOffers <= 0 && sourceBase.MaxMissionOffers <= 0) ||
            sourceBase.BaseFactions.Count == 0)
            return [];

        var missionNpcs = GetMissionNpcs(sourceBase, roomNickname).ToList();
        if (missionNpcs.Count == 0)
            return [];

        var offers = new List<RandomMissionOffer>();
        foreach (var faction in sourceBase.BaseFactions)
        {
            if (faction.Missions.Count == 0)
                continue;

            foreach (var mission in faction.Missions)
            {
                if (string.IsNullOrWhiteSpace(mission.Type) ||
                    !DifficultyMatches(mission.MinDiff, mission.MaxDiff, difficulty))
                    continue;

                var zones = GetEligibleZones(sourceBase, sourceSystem, faction.Faction, allowableZoneTypes);
                if (zones.Count == 0)
                    continue;

                foreach (var npc in missionNpcs)
                {
                    if (!FactionCanUseNpc(faction, npc) || !NpcMissionMatches(npc.Mission, mission.Type, difficulty))
                        continue;

                    offers.Add(new RandomMissionOffer {
                        Base = sourceBase, Npc = npc, BaseFaction = faction, Mission = mission, EligibleZones = zones
                    });
                }
            }
        }
        return offers;
    }

    static List<Zone> GetEligibleZones(Base sourceBase, StarSystem system, Faction? faction,
        IReadOnlyCollection<string>? allowableZoneTypes)
    {
        var baseObject = system.Objects.FirstOrDefault(x => ReferenceEquals(x.Base, sourceBase));
        if (baseObject == null)
            return [];

        Legality? legality = faction?.Properties?.Legality switch {
            Legality.Lawful => Legality.Unlawful,
            Legality.Unlawful => Legality.Lawful,
            _ => null
        };
        return system.Zones.Where(zone =>
            !string.IsNullOrEmpty(zone.VignetteType) &&
            Vector3.Distance(zone.Position, baseObject.Position) <= 20_000f &&
            zone.MissionType is { Length: > 0 } &&
            (legality == null ||
             zone.MissionType.Contains(legality.ToString(), StringComparer.OrdinalIgnoreCase)) &&
            (allowableZoneTypes == null || allowableZoneTypes.Count == 0 ||
             allowableZoneTypes.Contains(zone.VignetteType, StringComparer.OrdinalIgnoreCase))).ToList();
    }

    static IEnumerable<BaseNpc> GetMissionNpcs(Base sourceBase, string? roomNickname)
    {
        IEnumerable<BaseRoom> rooms = sourceBase.Rooms;
        if (!string.IsNullOrEmpty(roomNickname))
            rooms = rooms.Where(room => room.Nickname?.Equals(roomNickname, StringComparison.OrdinalIgnoreCase) == true);
        return rooms.SelectMany(room => room.Npcs).Where(npc => npc.Mission != null);
    }

    static bool FactionCanUseNpc(MBaseBaseFaction faction, BaseNpc npc) =>
        faction.Npcs.Count == 0 || faction.Npcs.Contains(npc.Nickname, StringComparer.OrdinalIgnoreCase);

    static bool NpcMissionMatches(NpcMission? mission, string type, float? difficulty) =>
        mission != null && mission.Kind.Equals(type, StringComparison.OrdinalIgnoreCase) &&
        DifficultyMatches(mission.Min, mission.Max, difficulty);

    static bool DifficultyMatches(float min, float max, float? difficulty) =>
        difficulty == null || (difficulty.Value >= min && difficulty.Value <= max);
}
