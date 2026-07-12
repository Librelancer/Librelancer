using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.Ini;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Data.Schema.RandomMissions;
using LibreLancer.Interface;
using LibreLancer.Missions;

namespace LibreLancer.Server.RandomMissions;

readonly record struct IdsArgument(char Category, int Ids);
readonly record struct StringArgument(char Category, string Value);

public sealed class GeneratedRandomMission
{
    public const string TargetObjectiveNickname = "rm_target";

    public required RandomMissionOffer Offer;
    public required Faction OfferFaction;
    public required Faction HostileFaction;
    public required Zone TargetZone;
    public required object TargetLocation;
    public required Vector3 TargetPosition;
    public required PossibleMission Vignette;
    public required MissionVariantPath Path;
    public required VignetteStrings Strings;
    public required IReadOnlyDictionary<string, object> Items;
    public required string MissionType;
    public required string OfferText;
    public required string TargetName;
    public required int Seed;
    public required float Difficulty;
    public required int Reward;
    public required ShipArch TargetShipArch;

    public MissionScript CreateScript(GameDataManager gameData)
    {
        if (UsesSingleShipTarget(MissionType))
            return CreateAssassinationScript(gameData);
        return CreateDestroyScript(gameData);
    }

    internal static bool UsesSingleShipTarget(string missionType) =>
        missionType.Equals("AssassinateMission", StringComparison.OrdinalIgnoreCase) ||
        missionType.Equals("BountyMission", StringComparison.OrdinalIgnoreCase) ||
        missionType.Equals("RetrieveMission", StringComparison.OrdinalIgnoreCase) ||
        missionType.Equals("DestroyContrabandMission", StringComparison.OrdinalIgnoreCase);

    MissionScript CreateAssassinationScript(GameDataManager gameData)
    {
        const string targetShip = "rm_target_ship";
        const string targetObjective = TargetObjectiveNickname;
        const string triggerInit = "rm_init";
        const string triggerDestroyed = "rm_target_destroyed";
        var ini = new MissionIni
        {
            Info = new MissionInfo
            {
                MissionTitle = TargetZone.IdsName,
                MissionOffer = 0,
                Reward = Reward
            },
            NPCs =
            {
                new MissionNPC
                {
                    Nickname = "rm_target_npc",
                    Affiliation = HostileFaction.Nickname,
                    IndividualName = 0,
                    NpcShipArch = TargetShipArch.Nickname
                }
            },
            Ships =
            {
                new MissionShip
                {
                    Nickname = targetShip,
                    System = Offer.Base.System,
                    NPC = "rm_target_npc",
                    Position = TargetPosition,
                    RandomName = true
                }
            },
            Objectives =
            {
                new NNObjective
                {
                    Nickname = targetObjective,
                    State = "ACTIVE",
                    Type = NNObjectiveType.navmarker,
                    System = Offer.Base.System!,
                    NameIds = TargetZone.IdsName,
                    ExplanationIds = TargetZone.IdsInfo,
                    Position = TargetPosition
                }
            },
            Triggers =
            {
                new MissionTrigger
                {
                    Nickname = triggerInit,
                    InitState = TriggerInitState.ACTIVE,
                    Conditions = { MakeCondition(TriggerConditions.Cnd_SpaceEnter, "no_params") },
                    Actions =
                    {
                        MakeAction(TriggerActions.Act_SetNNObj, targetObjective, "OBJECTIVE_HISTORY"),
                        MakeAction(TriggerActions.Act_SpawnShip, targetShip, "", TargetPosition.X, TargetPosition.Y, TargetPosition.Z),
                        MakeAction(TriggerActions.Act_SetVibe, targetShip, "Player", "REP_HOSTILE_MAXIMUM"),
                        MakeAction(TriggerActions.Act_ActTrig, triggerDestroyed)
                    }
                },
                new MissionTrigger
                {
                    Nickname = triggerDestroyed,
                    Conditions = { MakeCondition(TriggerConditions.Cnd_Destroyed, targetShip, 1, "EXPLODE") },
                    Actions =
                    {
                        MakeAction(TriggerActions.Act_SetNNState, targetObjective, "COMPLETE"),
                        MakeAction(TriggerActions.Act_AdjAcct, Reward),
                        MakeAction(TriggerActions.Act_PlaySoundEffect, "ui_receive_money"),
                        MakeAction(TriggerActions.Act_PlayMusic, "none", "none", "none", "music_victory", 3, true),
                        MakeAction(TriggerActions.Act_ChangeState, "SUCCEED")
                    }
                }
            }
        };
        return new MissionScript(ini, gameData.Items);
    }

    MissionScript CreateDestroyScript(GameDataManager gameData)
    {
        const string targetObjective = TargetObjectiveNickname;
        const string triggerInit = "rm_init";
        const string triggerDestroyed = "rm_destroy_group_destroyed";
        const string groupLabel = "rm_destroy_group";
        const string npc = "rm_destroy_npc";
        var shipCount = GetDestroyShipCount();
        var ini = new MissionIni
        {
            Info = new MissionInfo
            {
                MissionTitle = TargetLocationIds(),
                MissionOffer = 0,
                Reward = Reward
            },
            NPCs =
            {
                new MissionNPC
                {
                    Nickname = npc,
                    Affiliation = HostileFaction.Nickname,
                    IndividualName = 0,
                    NpcShipArch = TargetShipArch.Nickname
                }
            },
            Objectives =
            {
                new NNObjective
                {
                    Nickname = targetObjective,
                    State = "ACTIVE",
                    Type = NNObjectiveType.navmarker,
                    System = Offer.Base.System!,
                    NameIds = TargetLocationIds(),
                    ExplanationIds = TargetZone.IdsInfo,
                    Position = TargetPosition
                }
            }
        };

        for (int i = 0; i < shipCount; i++)
        {
            ini.Ships.Add(new MissionShip
            {
                Nickname = $"rm_destroy_ship_{i + 1}",
                System = Offer.Base.System,
                NPC = npc,
                Labels = { groupLabel },
                Position = DestroyShipPosition(i),
                RandomName = true
            });
        }

        var initTrigger = new MissionTrigger
        {
            Nickname = triggerInit,
            InitState = TriggerInitState.ACTIVE,
            Conditions = { MakeCondition(TriggerConditions.Cnd_SpaceEnter, "no_params") },
            Actions =
            {
                MakeAction(TriggerActions.Act_SetNNObj, targetObjective, "OBJECTIVE_HISTORY"),
                MakeAction(TriggerActions.Act_ActTrig, triggerDestroyed)
            }
        };
        foreach (var ship in ini.Ships)
        {
            initTrigger.Actions.Add(MakeAction(
                TriggerActions.Act_SpawnShip,
                ship.Nickname,
                "",
                ship.Position.X,
                ship.Position.Y,
                ship.Position.Z));
            initTrigger.Actions.Add(MakeAction(
                TriggerActions.Act_SetVibe,
                ship.Nickname,
                "Player",
                "REP_HOSTILE_MAXIMUM"));
        }
        ini.Triggers.Add(initTrigger);
        ini.Triggers.Add(new MissionTrigger
        {
            Nickname = triggerDestroyed,
            Conditions = { MakeCondition(TriggerConditions.Cnd_Destroyed, groupLabel, shipCount) },
            Actions =
            {
                MakeAction(TriggerActions.Act_SetNNState, targetObjective, "COMPLETE"),
                MakeAction(TriggerActions.Act_AdjAcct, Reward),
                MakeAction(TriggerActions.Act_PlaySoundEffect, "ui_receive_money"),
                MakeAction(TriggerActions.Act_PlayMusic, "none", "none", "none", "music_victory", 3, true),
                MakeAction(TriggerActions.Act_ChangeState, "SUCCEED")
            }
        });
        return new MissionScript(ini, gameData.Items);
    }

    int GetDestroyShipCount() =>
        Math.Clamp(2 + (int)MathF.Ceiling(Difficulty), 3, 5);

    Vector3 DestroyShipPosition(int index)
    {
        ReadOnlySpan<Vector3> offsets =
        [
            new Vector3(0, 0, 0),
            new Vector3(-250, 0, 150),
            new Vector3(250, 0, -150),
            new Vector3(0, 150, -300),
            new Vector3(0, -150, 300)
        ];
        return TargetPosition + offsets[index % offsets.Length];
    }

    int TargetLocationIds() => TargetLocation switch
    {
        Zone z => z.IdsName,
        Base b => b.IdsName,
        SystemObject o => o.IdsName,
        NamedItem n => n.IdsName,
        IdsArgument i => i.Ids,
        _ => TargetZone.IdsName
    };

    static MissionAction MakeAction(TriggerActions action, params ValueBase[] values) =>
        new(action, Entry(action.ToString(), values));

    static MissionCondition MakeCondition(TriggerConditions condition, params ValueBase[] values) =>
        new(condition, Entry(condition.ToString(), values));

    static Entry Entry(string name, params ValueBase[] values)
    {
        var entry = new Entry(new Section("generated"), name, values.Length);
        foreach (var value in values)
            entry.Add(value);
        return entry;
    }
}

public static class RandomMissionGenerator
{
    public static bool TryGenerate(
        GameDataManager gameData,
        RandomMissionOffer offer,
        int seed,
        out GeneratedRandomMission? mission)
    {
        mission = null;
        if (offer.Faction is not { Properties: not null } offerFaction)
            return false;
        if (offer.Base.System == null ||
            gameData.Items.Systems.Get(offer.Base.System) is not { } system)
            return false;

        var random = new VC6Random(seed);
        foreach (var zone in RandomZoneOrder(offer.EligibleZones, random))
        {
            var zoneType = GetAllowedZoneType(zone);
            if (zoneType == AllowedZoneType.None)
                continue;

            var hostileFaction = ChooseHostileFaction(gameData.Items, system, offerFaction, zone, random);
            if (hostileFaction?.Properties == null)
                continue;
            var targetShipArch = ChooseTargetShip(hostileFaction);
            if (targetShipArch == null)
                continue;

            var difficulty = GetDifficulty(offer, random);
            var leaves = VignetteWalker.Enumerate(
                gameData.Items.VignetteTree,
                new VignetteGraphParameters(offerFaction, hostileFaction, difficulty, zoneType));
            if (leaves.Count == 0)
                continue;

            var selectedLeaf = random.Select(leaves, x => x.EndNode.Weight);
            var selectedPath = random.Select(selectedLeaf.Paths, x => (float)x.Probability);
            var strings = selectedPath.GetStrings(random);
            var missionType = DetermineMissionType(selectedPath, strings);
            var targetName = CreateTargetName(gameData, hostileFaction, random);
            var reward = CalculateReward(gameData, difficulty);
            var bigSolar = ChooseNamedSystemObject(system, zone, null);
            var otherSolar = ChooseNamedSystemObject(system, zone, bigSolar);
            var criticalLoot = ChooseCriticalLoot(offer.Base);
            var targetZoneArgument = ChooseTargetZoneArgument(system, zone);
            var items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["MISSION_DIFFICULTY"] = (int)MathF.Round(difficulty),
                ["REWARD_MONEY"] = reward,
                ["Offer_group"] = offerFaction,
                ["Hostile_group"] = hostileFaction,
                ["TARGET_ZONE"] = targetZoneArgument,
                ["TARGET_FULL_NAME"] = targetName,
                ["OFFER_BASE"] = offer.Base,
                ["Big_solar"] = bigSolar != null ? (object)bigSolar : new IdsArgument('I', zone.IdsName),
                ["OTHER_SOLAR"] = otherSolar != null ? (object)otherSolar :
                    bigSolar != null ? (object)bigSolar : new IdsArgument('I', zone.IdsName),
                ["CRITICAL_LOOT"] = criticalLoot != null ? (object)criticalLoot : new IdsArgument('I', zone.IdsName)
            };

            mission = new GeneratedRandomMission
            {
                Offer = offer,
                OfferFaction = offerFaction,
                HostileFaction = hostileFaction,
                TargetZone = zone,
                TargetLocation = targetZoneArgument,
                TargetPosition = zone.Position,
                Vignette = selectedLeaf,
                Path = selectedPath,
                Strings = strings,
                Items = items,
                MissionType = missionType,
                OfferText = FormatOfferText(gameData, strings, items),
                TargetName = targetName,
                Seed = seed,
                Difficulty = difficulty,
                Reward = reward,
                TargetShipArch = targetShipArch
            };
            return true;
        }

        return false;
    }

    internal static string DetermineMissionType(MissionVariantPath path, VignetteStrings strings)
    {
        if (strings.CommSequences.TryGetValue("GLOBAL_SUCCESS", out var success))
        {
            var fromSuccessComm = success.Comm?.ToLowerInvariant() switch
            {
                "rmb_success_ships" => "DestroyMission",
                "rmb_success_ass" => "AssassinateMission",
                "rmb_success_returnloot" => "BountyMission",
                "rmb_success_tractorloot" => "RetrieveMission",
                "rmb_success_destroyloot" => "DestroyContrabandMission",
                "rmb_success_solar" => "DestroyInstallationMission",
                _ => null
            };
            if (fromSuccessComm != null)
                return fromSuccessComm;
        }

        var decisions = path.Decisions;
        if (decisions.DestroySolarsMission)
            return "DestroyInstallationMission";

        // Assassinate_mission is the outer branch containing both the
        // Assassinate_Ship and Pk_all_ships variants. It is not itself the
        // final mission type.
        if (!decisions.AssassinateMission || !decisions.AssassinateShip)
            return "DestroyMission";

        if (!decisions.TargetDropsCriticalLoot)
            return "AssassinateMission";

        if (decisions.BringBackLoot)
            return "BountyMission";

        if (decisions.TractorInLoot)
            return "RetrieveMission";

        return "DestroyContrabandMission";
    }

    static LibreLancer.Data.GameData.Items.Equipment? ChooseCriticalLoot(Base offerBase) =>
        offerBase.SoldGoods
            .Select(x => x.Good.Equipment)
            .FirstOrDefault(x => x.IdsName > 0);

    static List<Zone> RandomZoneOrder(IReadOnlyList<Zone> zones, VC6Random random)
    {
        var output = new List<Zone>(zones);
        for (int i = output.Count - 1; i > 0; i--)
        {
            var j = random.Next() % (i + 1);
            (output[i], output[j]) = (output[j], output[i]);
        }
        return output;
    }

    static object ChooseTargetZoneArgument(StarSystem system, Zone zone)
    {
        var field = FindContainingNamedField(system, zone.Position);
        if (field != null)
            return field;

        return new StringArgument('Z', NavmapGrid(system, zone.Position));
    }

    static Zone? FindContainingNamedField(StarSystem system, Vector3 position)
    {
        var fieldZones = new List<Zone>();
        foreach (var asteroid in system.AsteroidFields)
        {
            if (asteroid.Zone is { IdsName: > 0 } zone && zone.ContainsPoint(position))
                fieldZones.Add(zone);
        }
        foreach (var nebula in system.Nebulae)
        {
            if (nebula.Zone is { IdsName: > 0 } zone && zone.ContainsPoint(position))
                fieldZones.Add(zone);
        }
        foreach (var zone in system.Zones)
        {
            if (zone.IdsName <= 0 || !IsNamedFieldZone(zone) || !zone.ContainsPoint(position))
                continue;
            fieldZones.Add(zone);
        }
        if (fieldZones.Count == 0)
            return null;
        fieldZones.Sort((x, y) =>
            Vector3.DistanceSquared(x.Position, position).CompareTo(Vector3.DistanceSquared(y.Position, position)));
        return fieldZones[0];
    }

    static bool IsNamedFieldZone(Zone zone) =>
        (zone.PropertyFlags & (
            ZonePropFlags.Debris |
            ZonePropFlags.Rock |
            ZonePropFlags.Ice |
            ZonePropFlags.Lava |
            ZonePropFlags.Nomad |
            ZonePropFlags.Crystal |
            ZonePropFlags.Cloud |
            ZonePropFlags.Badlands |
            ZonePropFlags.GasPockets)) != 0;

    static string NavmapGrid(StarSystem system, Vector3 position)
    {
        var sector = system.WaypointSector(position);
        return sector.Length >= 2 ? $"{sector[1]}{sector[0]}" : sector;
    }

    static SystemObject? ChooseNamedSystemObject(StarSystem system, Zone zone, SystemObject? except)
    {
        SystemObject? best = null;
        var bestDistance = float.MaxValue;
        foreach (var obj in system.Objects)
        {
            if (ReferenceEquals(obj, except) || obj.IdsName == 0)
                continue;
            var distance = Vector3.DistanceSquared(obj.Position, zone.Position);
            if (distance < bestDistance)
            {
                best = obj;
                bestDistance = distance;
            }
        }
        return best;
    }

    static float GetDifficulty(RandomMissionOffer offer, VC6Random random)
    {
        var min = offer.Mission.MinDiff;
        var max = offer.Mission.MaxDiff;
        if (max <= min)
            return min;
        return min + ((random.Next() / 32767f) * (max - min));
    }

    static int CalculateReward(GameDataManager gameData, float difficulty) =>
        Math.Max(500, gameData.Items.VignetteDifficulty.GetMissionReward(Math.Max(0, difficulty)));

    static AllowedZoneType GetAllowedZoneType(Zone zone)
    {
        if (Enum.TryParse<AllowedZoneType>(zone.VignetteType, true, out var type))
            return type;
        return AllowedZoneType.Open;
    }

    static Faction? ChooseHostileFaction(
        GameItemDb items,
        StarSystem system,
        Faction offerFaction,
        Zone vignetteZone,
        VC6Random random)
    {
        var candidates = new List<HostileFactionCandidate>();
        var vignetteBox = GetZoneBounds(vignetteZone);
        foreach (var zone in system.Zones)
        {
            if (zone.Encounters is not { Length: > 0 } ||
                !vignetteBox.Intersects(GetZoneBounds(zone)))
                continue;

            foreach (var encounter in zone.Encounters)
            {
                foreach (var spawn in encounter.FactionSpawns)
                {
                    var faction = items.Factions.Get(spawn.Faction);
                    if (faction?.Properties == null ||
                        ReferenceEquals(faction, offerFaction) ||
                        !IsHostile(offerFaction, faction) ||
                        ChooseTargetShip(faction) == null)
                        continue;

                    var encounterWeight = encounter.Chance > 0 ? encounter.Chance : 1;
                    var factionWeight = spawn.Chance > 0 ? spawn.Chance : 1;
                    candidates.Add(new HostileFactionCandidate(faction, encounterWeight * factionWeight));
                }
            }
        }
        return candidates.Count > 0 ? random.Select(candidates, x => x.Weight).Faction : null;
    }

    private readonly record struct HostileFactionCandidate(Faction Faction, float Weight);

    static bool IsHostile(Faction offerFaction, Faction faction) =>
        offerFaction.GetReputation(faction) <= Faction.HostileThreshold ||
        faction.GetReputation(offerFaction) <= Faction.HostileThreshold;

    static BoundingBox GetZoneBounds(Zone zone)
    {
        var size = zone.Shape switch
        {
            ShapeKind.Sphere => new Vector3(zone.Size.X * 2),
            ShapeKind.Cylinder or ShapeKind.Ring => new Vector3(zone.Size.X * 2, zone.Size.Y, zone.Size.X * 2),
            _ => zone.Size
        };
        var half = Vector3.Abs(size) * 0.5f;
        return new BoundingBox(zone.Position - half, zone.Position + half);
    }

    static ShipArch? ChooseTargetShip(Faction hostileFaction) =>
        hostileFaction.NpcShips.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.Nickname) &&
            !string.IsNullOrWhiteSpace(x.Loadout) &&
            !string.IsNullOrWhiteSpace(x.Pilot));

    static string CreateTargetName(GameDataManager gameData, Faction hostileFaction, VC6Random random)
    {
        var props = hostileFaction.Properties;
        if (props == null || props.LastName.Min == 0)
            return gameData.GetString(hostileFaction.IdsName) ?? hostileFaction.Nickname;

        ValueRange<int>? firstNameRange = null;
        if (props.FirstNameMale != null && props.FirstNameFemale != null)
            firstNameRange = random.NextInt(0, 1) == 1 ? props.FirstNameMale : props.FirstNameFemale;
        else if (props.FirstNameFemale != null)
            firstNameRange = props.FirstNameFemale;
        else if (props.FirstNameMale != null)
            firstNameRange = props.FirstNameMale;

        for (int i = 0; i < 8; i++)
        {
            var firstName = firstNameRange != null
                ? gameData.GetString(random.NextInt(firstNameRange.Value.Min, firstNameRange.Value.Max))
                : "";
            var lastName = gameData.GetString(random.NextInt(props.LastName.Min, props.LastName.Max));
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                return $"{firstName} {lastName}";
            if (!string.IsNullOrWhiteSpace(lastName))
                return lastName;
        }

        return gameData.GetString(hostileFaction.IdsName) ?? hostileFaction.Nickname;
    }

    static string FormatOfferText(GameDataManager gameData, VignetteStrings strings, Dictionary<string, object> items)
    {
        var output = new List<string>();
        foreach (var offerText in strings.OfferText)
        {
            if (offerText.Type == OfferTextType.singular &&
                items[offerText.Args[0]] is Faction s &&
                s.Properties?.NicknamePlurality != NicknamePlurality.Singular)
                continue;
            if (offerText.Type == OfferTextType.plural &&
                items[offerText.Args[0]] is Faction p &&
                p.Properties?.NicknamePlurality != NicknamePlurality.Plural)
                continue;
            output.Add(CleanResourceString(Format(gameData, offerText.Ids, offerText.Args, items)));
        }
        if (output.Count > 0)
            return string.Join("", output);
        return "Mission generated from vignette data.";
    }

    internal static string CleanResourceString(string value) =>
        value.Replace("\0", "");

    internal static string Format(GameDataManager gameData, int ids, string[] args, IReadOnlyDictionary<string, object> items)
    {
        List<IdsFormatItem> resolvedArgs = [];
        int indexS = 0;
        int indexD = 0;
        int indexF = 0;
        int indexZ = 0;
        int indexI = 0;
        foreach (var a in args)
        {
            if (!items.TryGetValue(a, out var value))
                value = "";
            switch (value)
            {
                case string s:
                    resolvedArgs.Add(new('s', indexS++, s));
                    break;
                case int d:
                    resolvedArgs.Add(new('d', indexD++, d.ToString()));
                    break;
                case Faction f:
                    resolvedArgs.Add(new('F', indexF++, f.IdsName));
                    break;
                case Zone z:
                    resolvedArgs.Add(new('Z', indexZ++, z.IdsName));
                    break;
                case SystemObject o:
                    resolvedArgs.Add(new('I', indexI++, o.IdsName));
                    break;
                case NamedItem n:
                    resolvedArgs.Add(new('I', indexI++, n.IdsName));
                    break;
                case IdsArgument i:
                    resolvedArgs.Add(new(i.Category, NextIndex(i.Category, ref indexS, ref indexD, ref indexF, ref indexZ, ref indexI), i.Ids));
                    break;
                case StringArgument s:
                    resolvedArgs.Add(new(s.Category, NextIndex(s.Category, ref indexS, ref indexD, ref indexF, ref indexZ, ref indexI), s.Value));
                    break;
            }
        }
        return IdsFormatting.Format(gameData.GetString(ids),
            gameData.Items.Ini.Infocards, resolvedArgs.ToArray());
    }

    static int NextIndex(char category, ref int indexS, ref int indexD, ref int indexF, ref int indexZ, ref int indexI)
    {
        switch (category)
        {
            case 's':
                return indexS++;
            case 'd':
                return indexD++;
            case 'F':
                return indexF++;
            case 'Z':
                return indexZ++;
            case 'I':
                return indexI++;
            default:
                return indexI++;
        }
    }

}
