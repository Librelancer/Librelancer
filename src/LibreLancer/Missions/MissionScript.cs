// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Missions.Actions;
using LibreLancer.Missions.Conditions;

namespace LibreLancer.Missions
{
    public class MissionScript
    {
        public MissionIni Ini;

        public readonly Dictionary<string, NPCShipArch> NpcShips = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, MissionShip> Ships = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, MissionSolar> Solars = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, MissionNPC> NPCs = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, MissionFormation> Formations = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ScriptedTrigger> AvailableTriggers = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ScriptAiCommands> ObjLists = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, MissionDialog> Dialogs = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, NNObjective> Objectives = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, MissionLoot> Loot = new(StringComparer.OrdinalIgnoreCase);

        public readonly List<string> InitTriggers = new();

        public PreloadObject[] CalculatePreloads(GameDataManager gameData)
        {
            HashSet<string> ships = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> equipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sh in Ships.Values)
            {
                NPCs.TryGetValue(sh.NPC, out var npc);
                if (npc == null)
                    continue;
                NpcShips.TryGetValue(npc.NpcShipArch, out var shipArch);
                if (shipArch == null)
                    shipArch = gameData.Items.Ini.NPCShips.ShipArches.FirstOrDefault(x =>
                        x.Nickname.Equals(npc.NpcShipArch, StringComparison.OrdinalIgnoreCase));
                if (shipArch == null)
                    continue;
                gameData.Items.TryGetLoadout(shipArch.Loadout, out var ld);
                if (ld == null)
                    continue;
                ships.Add(ld.Archetype);
                foreach (var cargo in ld.Cargo)
                    equipment.Add(cargo.Item.Nickname);
                foreach (var item in ld.Items)
                    equipment.Add(item.Equipment.Nickname);
            }

            var shipItems = ships.Chunk(31).Select(x => new PreloadObject(PreloadType.Ship, x.Select(x => new HashValue(x)).ToArray()));
            var equipItems = equipment.Chunk(31).Select(x => new PreloadObject(PreloadType.Equipment, x.Select(x => new HashValue(x)).ToArray()));

            return shipItems.Concat(equipItems).ToArray();
        }

        //Set only the first one
        //Without this, order ships spawn in the wrong place in M01A
        static void Set<T>(Dictionary<string, T> dict, string k, T value)
        {
            if (!dict.ContainsKey(k)) dict[k] = value;
            else {
                FLLog.Warning("Mission", $"Duplicate {typeof(T)} `{k}`, ignoring.");
            }
        }

        public IEnumerable<MissionLabel> GetLabels()
        {
            var allLabels = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var sh in Ships)
            {
                foreach (var l in sh.Value.Labels)
                {
                    if (!allLabels.TryGetValue(l, out var list))
                    {
                        list = new List<string>();
                        allLabels.Add(l, list);
                    }
                    list.Add(sh.Key);
                }
            }
            foreach (var sl in Solars)
            {
                foreach (var l in sl.Value.Labels)
                {
                    if (!allLabels.TryGetValue(l, out var list))
                    {
                        list = new List<string>();
                        allLabels.Add(l, list);
                    }
                    list.Add(sl.Key);
                }
            }
            return allLabels.Select(x => new MissionLabel(x.Key, x.Value));
        }

        public MissionScript(MissionIni ini)
        {
            Ini = ini;

            foreach (var s in ini.Solars)
            {
                Set(Solars, s.Nickname, s);
            }

            foreach (var s in ini.Ships)
            {
                Set(Ships, s.Nickname, s);
            }

            foreach (var n in ini.NPCs)
            {
                Set(NPCs, n.Nickname, n);
            }

            foreach (var f in ini.Formations)
            {
                Set(Formations, f.Nickname, f);
            }

            foreach(var o in ini.Objectives)
            {
                Set(Objectives, o.Nickname, o);
            }

            foreach (var ol in ini.ObjLists)
            {
                Set(ObjLists, ol.Nickname, new ScriptAiCommands(ol));
            }

            foreach (var dlg in ini.Dialogs)
            {
                Set(Dialogs, dlg.Nickname, dlg);
            }

            foreach (var loot in ini.Loots)
            {
                Set(Loot, loot.Nickname, loot);
            }

            if (ini.ShipIni != null)
            {
                foreach (var s in ini.ShipIni.ShipArches)
                {
                    NpcShips[s.Nickname] = s;
                }
            }

            foreach (var tr in ini.Triggers)
            {
                AvailableTriggers[tr.Nickname] = new ScriptedTrigger() {
                    Nickname = tr.Nickname,
                    Repeatable = tr.Repeatable,
                    Conditions = ScriptedCondition.Convert(tr.Conditions).ToArray(),
                    Actions =  ScriptedAction.Convert(tr.Actions).ToArray()
                };
                if(tr.InitState == TriggerInitState.ACTIVE)
                    InitTriggers.Add(tr.Nickname);
            }

            Console.WriteLine();
        }
    }

    public class ScriptAiCommands
    {
        public string Nickname;
        public string System = "";
        public readonly List<MissionDirective> Directives;

        public ScriptAiCommands(ObjList ini)
        {
            Nickname = ini.Nickname;
            System = ini.System;
            Directives = ini.Commands.Select(MissionDirective.Convert).ToList();
        }

        public ScriptAiCommands(string nickname)
        {
            Nickname = nickname;
            Directives = new();
        }
    }

    public class ScriptedTrigger
    {
        public string Nickname;
        public bool Repeatable;
        public ScriptedCondition[] Conditions;
        public ScriptedAction[] Actions;
    }
}
