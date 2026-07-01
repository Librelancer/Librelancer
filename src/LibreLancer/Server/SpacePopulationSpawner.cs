using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Missions;
using LibreLancer.Server.Components;
using LibreLancer.World;
using UniverseEncounter = LibreLancer.Data.Schema.Universe.Encounter;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LibreLancer.Server;

public partial class SpacePopulationManager
{
    private void TryRepopulate(ZoneState state, GameObject[] players)
    {
        var context = GetPopulationContext(state, players);
        if (context.Players.Length == 0 || context.Density <= 0)
            return;

        var alive = CountShips(state);
        if (state.InBattle && state.Zone.MaxBattleSize > 0 && alive >= state.Zone.MaxBattleSize)
            return;

        if (alive >= context.Density)
            return;

        var repopTime = Math.Max(1, state.Zone.RepopTime);
        var chance = Math.Clamp((context.Density - alive) / (float)repopTime, 0, 1);
        if (random.NextSingle() > chance)
            return;

        if (!TryChooseEncounter(state.Zone, out var encounterDef))
            return;

        var faction = ChooseFaction(encounterDef);
        if (faction == null)
            return;

        var encounterIni = GetEncounterIni(encounterDef.Archetype);
        if (encounterIni == null)
            return;

        var info = EncounterHandler.CreateEncounter(
            encounterIni,
            encounterDef.Difficulty,
            faction,
            world.Server.GameData.Items,
            random);
        if (info.Ships.Count == 0)
            return;
        if (state.InBattle &&
            state.Zone.MaxBattleSize > 0 &&
            alive + info.Ships.Count > state.Zone.MaxBattleSize)
        {
            return;
        }
        var populationClasses = BuildPopulationClasses(state.Zone, info, faction);
        if (!CanSpawnRestrictedPopulation(state, populationClasses))
            return;
        if (!CanCreateFormation(state, info))
            return;
        var creationDistance = info.FormationDefinition?.ZoneCreationDistance ?? 0;
        if (!TryFindSpawnLocation(state, info, context.Players, creationDistance, false, out var spawn))
            return;

        SpawnGroup(state, info, faction, spawn, populationClasses);
    }

    public void PopulateInitialAroundPlayer(GameObject player)
    {
        if (!Alive(player) || !IsFreetimePlayer(player))
            return;

        var players = new[] { player };
        foreach (var state in zones)
        {
            var context = GetPopulationContext(state, players);
            if (context.Players.Length == 0 || context.Density <= 0)
                continue;

            var attempts = Math.Max(1, context.Density * 2);
            while (CountShips(state) < context.Density && attempts-- > 0)
            {
                TryPopulateInitial(state, context);
            }
        }
    }

    private void TryPopulateInitial(ZoneState state, PopulationContext context)
    {
        var alive = CountShips(state);
        if (state.InBattle && state.Zone.MaxBattleSize > 0 && alive >= state.Zone.MaxBattleSize)
            return;

        if (alive >= context.Density)
            return;

        if (!TryChooseEncounter(state.Zone, out var encounterDef))
            return;

        var faction = ChooseFaction(encounterDef);
        if (faction == null)
            return;

        var encounterIni = GetEncounterIni(encounterDef.Archetype);
        if (encounterIni == null)
            return;

        var info = EncounterHandler.CreateEncounter(
            encounterIni,
            encounterDef.Difficulty,
            faction,
            world.Server.GameData.Items,
            random);
        if (info.Ships.Count == 0)
            return;
        if (state.InBattle &&
            state.Zone.MaxBattleSize > 0 &&
            alive + info.Ships.Count > state.Zone.MaxBattleSize)
        {
            return;
        }
        var populationClasses = BuildPopulationClasses(state.Zone, info, faction);
        if (!CanSpawnRestrictedPopulation(state, populationClasses))
            return;
        if (!CanCreateFormation(state, info))
            return;
        var creationDistance = info.FormationDefinition?.ZoneCreationDistance ?? 0;
        if (!TryFindSpawnLocation(state, info, context.Players, creationDistance, true, out var spawn))
            return;

        SpawnGroup(state, info, faction, spawn, populationClasses);
    }

    private int CountShips(ZoneState state) =>
        state.Groups.Sum(x => x.Ships.Count(Alive));

    private bool TryChooseEncounter(Zone zone, out UniverseEncounter encounter)
    {
        encounter = null!;
        if (zone.Encounters is not { Length: > 0 })
            return false;

        var total = zone.Encounters.Sum(x => Math.Max(0, x.Chance));
        if (total <= 0)
        {
            encounter = zone.Encounters[random.Next(zone.Encounters.Length)];
            return true;
        }

        var roll = total <= 1
            ? random.NextDouble()
            : random.NextDouble() * total;
        var cursor = 0.0;
        foreach (var item in zone.Encounters)
        {
            cursor += Math.Max(0, item.Chance);
            if (roll <= cursor)
            {
                encounter = item;
                return true;
            }
        }
        return false;
    }

    private Faction? ChooseFaction(UniverseEncounter encounter)
    {
        if (encounter.FactionSpawns.Count == 0)
            return null;

        var total = encounter.FactionSpawns.Sum(x => Math.Max(0, x.Chance));
        if (total <= 0)
        {
            foreach (var spawn in encounter.FactionSpawns)
            {
                var faction = world.Server.GameData.Items.Factions.Get(spawn.Faction);
                if (faction != null)
                    return faction;
            }
            return null;
        }

        var roll = total <= 1
            ? random.NextDouble()
            : random.NextDouble() * total;
        foreach (var spawn in encounter.FactionSpawns)
        {
            roll -= Math.Max(0, spawn.Chance);
            if (roll > 0)
                continue;

            var faction = world.Server.GameData.Items.Factions.Get(spawn.Faction);
            if (faction != null)
                return faction;
        }
        return null;
    }

    private EncounterIni? GetEncounterIni(string archetype)
    {
        if (encounterCache.TryGetValue(archetype, out var cached))
            return cached;

        var parameters = world.System.EncounterParameters.FirstOrDefault(x =>
            archetype.Equals(x.Nickname, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(parameters.Nickname))
        {
            FLLog.Warning("SpacePop", $"Encounter parameters {archetype} not found in {world.System.Nickname}");
            encounterCache[archetype] = null;
            return null;
        }

        var path = world.Server.GameData.Items.DataPath(parameters.SourceFile);
        if (path == null)
        {
            encounterCache[archetype] = null;
            return null;
        }

        try
        {
            return encounterCache[archetype] = new EncounterIni(path, world.Server.GameData.Items.VFS);
        }
        catch (Exception ex)
        {
            FLLog.Warning("SpacePop", $"Could not parse encounter {parameters.SourceFile}: {ex.Message}");
            encounterCache[archetype] = null;
            return null;
        }
    }

    private void SpawnGroup(
        ZoneState state,
        EncounterInfo info,
        Faction faction,
        SpawnLocation spawn,
        HashSet<string> populationClasses)
    {
        var items = world.Server.GameData.Items;
        var position = spawn.Position;
        var firstTarget = spawn.InitialPathTarget ?? GetFirstDirectiveTarget(state, info);
        var orientation = spawn.ArrivalObject != null
            ? spawn.Orientation
            : LookRotation(firstTarget - position);
        var group = new PopGroup(state, info)
        {
            ArrivalObject = spawn.ArrivalObject,
            PersistDistance = spawn.PersistDistance
        };
        foreach (var populationClass in populationClasses)
            group.PopulationClasses.Add(populationClass);
        if (spawn.PathIndex >= 0)
            group.PathIndex = spawn.PathIndex;
        group.InitialPathTarget = spawn.InitialPathTarget;

        SDockableComponent? arrivalDockable = null;
        if (spawn.ArrivalObject != null &&
            world.GameWorld.GetObject(spawn.ArrivalObject)?.TryGetComponent<SDockableComponent>(out var dockable) == true)
        {
            arrivalDockable = dockable;
        }

        for (int i = 0; i < info.Ships.Count; i++)
        {
            var arrivalIndex = spawn.ArrivalIndex;
            var reservedArrival = false;
            if (arrivalDockable != null)
            {
                reservedArrival = spawn.ArrivalIndex == 0
                    ? arrivalDockable.TryReserveUndockIndex(out arrivalIndex)
                    : arrivalDockable.TryReserveUndockIndex(arrivalIndex);

                if (!reservedArrival)
                    break;
            }

            var entry = info.Ships[i];
            if (string.IsNullOrWhiteSpace(entry.Ship.Loadout) ||
                !items.TryGetLoadout(entry.Ship.Loadout, out var loadout))
            {
                if (reservedArrival)
                    arrivalDockable!.ReleaseUndockIndex(arrivalIndex);
                FLLog.Warning("SpacePop", $"NPC ship {entry.Ship.Nickname} has no valid loadout");
                continue;
            }

            var stateGraph = string.IsNullOrWhiteSpace(entry.Ship.StateGraph)
                ? "FIGHTER"
                : entry.Ship.StateGraph;
            var pilot = string.IsNullOrWhiteSpace(entry.Ship.Pilot) ? null : items.GetPilot(entry.Ship.Pilot);
            var nickname = $"spacepop_{++spawnCounter}_{i}";
            var offset = spawn.ArrivalObject == null
                ? GetSpawnOffset(info, i, orientation)
                : Vector3.Zero;
            var costume = RandomCostume(faction);
            var obj = world.NPCs.DoSpawn(
                entry.Name,
                nickname,
                faction,
                stateGraph,
                costume,
                loadout,
                pilot,
                position + offset,
                orientation,
                spawn.ArrivalObject,
                arrivalIndex,
                null,
                false,
                reservedArrival);
            group.Ships.Add(obj);
        }

        if (group.Ships.Count == 0)
            return;

        state.Groups.Add(group);
        RecordFormationCreation(state, info);
        if (info.Formation != null && group.Ships.Count > 1)
        {
            FormationTools.MakeNewFormation(
                group.Ships[0],
                world.GameWorld,
                info.Formation.Nickname,
                group.Ships.Skip(1).Select(x => x.Nickname).ToList());
        }
        AssignDirectives(group);
    }

    private bool CanSpawnRestrictedPopulation(ZoneState state, HashSet<string> populationClasses)
    {
        if (state.Zone.DensityRestrictions is not { Length: > 0 })
            return true;

        foreach (var restriction in state.Zone.DensityRestrictions)
        {
            var type = NormalizePopulationClass(restriction.Type);
            if (!populationClasses.Contains(type))
                continue;

            if (restriction.Count <= 0)
                return false;

            if (CountPopulationClass(type) >= restriction.Count)
                return false;
        }
        return true;
    }

    private int CountPopulationClass(string populationClass)
    {
        var count = 0;
        foreach (var state in zones)
        {
            foreach (var group in state.Groups)
            {
                if (group.PopulationClasses.Contains(populationClass) &&
                    group.Ships.Any(Alive))
                {
                    count++;
                }
            }
        }
        return count;
    }

    private static HashSet<string> BuildPopulationClasses(Zone zone, EncounterInfo info, Faction faction)
    {
        var classes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddPopulationClasses(classes, zone.PopType);
        foreach (var entry in info.Ships)
            AddPopulationClass(classes, entry.MakeClass);

        if (faction.Properties?.Legality == Legality.Unlawful)
            classes.Add("unlawfuls");
        else
            classes.Add("lawfuls");
        return classes;
    }

    private static void AddPopulationClasses(HashSet<string> classes, string[]? values)
    {
        if (values == null)
            return;

        foreach (var value in values)
            AddPopulationClass(classes, value);
    }

    private static void AddPopulationClass(HashSet<string> classes, string? value)
    {
        var normalized = NormalizePopulationClass(value);
        if (normalized.Length == 0)
            return;

        classes.Add(normalized);
        if (normalized.EndsWith("_patroller", StringComparison.OrdinalIgnoreCase))
            classes.Add("patroller");
    }

    private static string NormalizePopulationClass(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();
        return value.StartsWith("class_", StringComparison.OrdinalIgnoreCase)
            ? value["class_".Length..]
            : value;
    }

    private bool CanCreateFormation(ZoneState state, EncounterInfo info)
    {
        var formation = info.FormationDefinition;
        if (formation == null)
            return true;

        if (!formation.AllowSimultaneousCreation && state.Groups.Any(x =>
                ReferenceEquals(x.Encounter.FormationDefinition, formation) &&
                x.Ships.Any(Alive)))
        {
            return false;
        }

        if (!TryGetTimesToCreate(formation, out var limit))
            return true;

        if (!state.FormationCreateCounts.TryGetValue(formation, out var created))
            return true;

        return created < limit;
    }

    private static void RecordFormationCreation(ZoneState state, EncounterInfo info)
    {
        var formation = info.FormationDefinition;
        if (formation == null || !TryGetTimesToCreate(formation, out _))
            return;

        state.FormationCreateCounts[formation] =
            state.FormationCreateCounts.TryGetValue(formation, out var created)
                ? created + 1
                : 1;
    }

    private static bool TryGetTimesToCreate(EncounterFormation formation, out int limit)
    {
        limit = 0;
        var value = formation.TimesToCreate;
        if (string.IsNullOrWhiteSpace(value) ||
            value.Equals("infinite", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(value, out limit) && limit >= 0;
    }

    private CostumeEntry? RandomCostume(Faction faction)
    {
        if (faction.Properties?.SpaceCostume.Count > 0)
        {
            var costume = faction.Properties.SpaceCostume[random.Next(faction.Properties.SpaceCostume.Count)];
            return new CostumeEntry([costume.Head, costume.Body, costume.Extra], world.Server.GameData.Items);
        }
        return null;
    }

    private static Vector3 GetSpawnOffset(EncounterInfo info, int index, Quaternion orientation)
    {
        if (info.Formation != null && index < info.Formation.Positions.Count)
            return Vector3.Transform(info.Formation.Positions[index], orientation);

        if (index == 0)
            return Vector3.Zero;

        var angle = (MathF.PI * 2f / Math.Max(2, index + 1)) * index;
        return Vector3.Transform(new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * 75f, orientation);
    }
}
