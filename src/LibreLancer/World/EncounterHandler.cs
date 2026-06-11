using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.World;

public static class EncounterHandler
{
    public static EncounterInfo CreateEncounter(
        EncounterIni encounter,
        int level,
        Faction faction,
        GameItemDb db) => CreateEncounter(encounter, level, faction, db, new Random());

    public static EncounterInfo CreateEncounter(
        EncounterIni encounter,
        int level,
        Faction faction,
        GameItemDb db,
        Random random)
    {
        var ei = new EncounterInfo();

        if (encounter.Formations.Count == 0)
        {
            FLLog.Error("Encounter", "Encounter has no formations");
            return ei;
        }

        var formation = ChooseFormation(encounter, random);
        ei.FormationDefinition = formation;
        ei.Formation = ResolveFormation(faction, db, formation.FormationByClass);

        foreach (var s in formation.Ships)
        {
            var count = random.Next(s.Min, s.Max + 1);
            var possible = ResolveShips(s, level, faction, db);
            if (possible.Count == 0)
            {
               FLLog.Error("Encounter", $"{faction.Nickname} has no ships for {s.Archetype} and d{level}");
               continue;
            }

            for (int i = 0; i < count; i++)
            {
                var arch = possible[random.Next(possible.Count)];
                var v = faction.NpcVoices.Count > 0 ? faction.NpcVoices[random.Next(faction.NpcVoices.Count)] : null;
                var name = MakeName(faction, v, random);
                ei.Ships.Add(new(name, v, arch));
            }
        }

        return ei;
    }

    private static EncounterFormation ChooseFormation(EncounterIni encounter, Random random)
    {
        if (encounter.Permutations == null || encounter.Permutations.Permutations.Count == 0)
            return encounter.Formations[0];

        var total = encounter.Permutations.Permutations.Sum(x => Math.Max(0, x.Weight));
        if (total <= 0)
            return encounter.Formations[0];

        var roll = random.NextDouble() * total;
        foreach (var permutation in encounter.Permutations.Permutations)
        {
            roll -= Math.Max(0, permutation.Weight);
            if (roll > 0)
                continue;

            var index = permutation.Index;
            if (index < 0 || index >= encounter.Formations.Count)
                index = permutation.Index - 1;
            if (index >= 0 && index < encounter.Formations.Count)
                return encounter.Formations[index];
            break;
        }
        return encounter.Formations[0];
    }

    private static FormationDef? ResolveFormation(Faction faction, GameItemDb db, string? formationClass)
    {
        if (string.IsNullOrWhiteSpace(formationClass))
            return null;

        if (faction.Formations.TryGetValue(formationClass, out var cached))
            return cached;

        var factionFormation = faction.Properties?.Formation.FirstOrDefault(x =>
            formationClass.Equals(x.EncounterFormation, StringComparison.OrdinalIgnoreCase));
        var def = db.GetFormation(factionFormation?.FormationDef ?? formationClass);
        if (def != null)
            faction.Formations[formationClass] = def;
        return def;
    }

    private static List<ShipArch> ResolveShips(
        EncounterShipDefinition ship,
        int level,
        Faction faction,
        GameItemDb db)
    {
        if (ship.Kind == EncounterShipKind.NPCArch)
        {
            var arch = db.NpcShips.Get(ship.Archetype);
            if (arch == null)
            {
                FLLog.Error("Encounter", $"{ship.Archetype} not in npcships.ini");
                return [];
            }
            return [arch];
        }

        var cls = db.Ini.ShipClasses.Classes.FirstOrDefault(x =>
            x.Nickname.Equals(ship.Archetype, StringComparison.OrdinalIgnoreCase));

        if (cls == null)
        {
            FLLog.Error("Encounter", $"{ship.Archetype} not in shipclasses.ini");
            return [];
        }

        var possible = new List<ShipArch>();
        foreach (var m in cls.Members)
        {
            if (!faction.ShipsByClass.TryGetValue(m, out var ls))
                continue;

            foreach (var x in ls)
            {
                if (ShipMatchesLevel(x, level))
                    possible.Add(x);
            }
        }
        return possible;
    }

    private static bool ShipMatchesLevel(ShipArch arch, int level) =>
        arch.Level == level ||
        arch.NpcClass.Contains($"d{level}", StringComparer.OrdinalIgnoreCase);

    private static ObjectName MakeName(Faction faction, Voice? voice, Random random)
    {
        if (faction.Properties == null)
            return new ObjectName("NULL");

        ValueRange<int>? firstName = null;
        var gender = voice?.Gender ?? FLGender.unset;
        if (faction.Properties.FirstNameFemale != null && gender == FLGender.female)
            firstName = faction.Properties.FirstNameFemale;
        else if (faction.Properties.FirstNameMale != null)
            firstName = faction.Properties.FirstNameMale;

        var fn = firstName != null ? random.Next(firstName.Value) : 0;
        var ln = random.Next(faction.Properties.LastName);
        int[] ids = [fn, ln];
        return new ObjectName(ids.Where(x => x != 0).ToArray());
    }
}
