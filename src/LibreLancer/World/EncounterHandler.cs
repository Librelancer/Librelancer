using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Data.Schema.Voices;

namespace LibreLancer.World;

public static class EncounterHandler
{
    public static EncounterInfo CreateEncounter(
        EncounterIni encounter,
        int level,
        Faction faction,
        GameItemDb db)
    {
        var ei = new EncounterInfo();
        var r = new Random();

        var formation = encounter.Formations[0];

        foreach (var s in formation.Ships)
        {
            var count = r.Next(s.Min, s.Max + 1);
            var cls = db.Ini.ShipClasses.Classes.FirstOrDefault(x =>
                x.Nickname.Equals(s.Archetype, StringComparison.OrdinalIgnoreCase));

            if (cls == null)
            {
                FLLog.Error("Encounter", $"{s.Archetype} not in shipclasses.ini");
                continue;
            }
            var possible = new List<ShipArch>();
            foreach (var m in cls.Members)
            {
                if (faction.ShipsByClass.TryGetValue(m, out var ls))
                {
                    foreach (var x in ls)
                    {
                        if (x.NpcClass.Contains($"d{level}"))
                        {
                            possible.Add(x);
                        }
                    }
                }
            }
            if (possible.Count == 0)
            {
               FLLog.Error("Encounter", $"{faction.Nickname} has no ships for shipclass {cls.Nickname} and d{level}");
               continue;
            }
            for (int i = 0; i < count; i++)
            {
                var arch = possible[r.Next(possible.Count)];
                var v = faction.NpcVoices[r.Next(faction.NpcVoices.Count)];
                ValueRange<int>? firstName = null;
                var g = v?.Gender ?? FLGender.unset;
                if (faction.Properties.FirstNameFemale != null && g == FLGender.female)
                {
                    firstName = faction.Properties.FirstNameFemale;
                }
                else if (faction.Properties.FirstNameMale != null)
                {
                    firstName = faction.Properties.FirstNameMale;
                }
                var fn = firstName != null ? r.Next(firstName.Value) : 0;
                var ln = r.Next(faction.Properties.LastName);
                int[] ids = [fn, ln];
                var name = new ObjectName(ids.Where(x => x != 0).ToArray());
                ei.Ships.Add(new(name, v, arch));
            }
        }

        return ei;
    }
}
