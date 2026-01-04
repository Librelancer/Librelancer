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
        public MissionInfo Info;

        public readonly Dictionary<string, ShipArch> NpcShips = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ScriptShip> Ships = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ScriptSolar> Solars = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ScriptNPC> NPCs = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ScriptFormation> Formations = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ScriptedTrigger> AvailableTriggers = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ScriptAiCommands> ObjLists = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ScriptDialog> Dialogs = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, NNObjective> Objectives = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ScriptLoot> Loot = new(StringComparer.OrdinalIgnoreCase);

        public readonly List<string> InitTriggers = new();

        public PreloadObject[] CalculatePreloads(GameDataManager gameData)
        {
            HashSet<string> ships = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> equipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sh in Ships.Values)
            {
                var npc = sh.NPC;
                if (npc == null)
                    continue;
                if (!NpcShips.TryGetValue(npc.NpcShipArch, out var shipArch))
                {
                    shipArch = gameData.Items.NpcShips.Get(npc.NpcShipArch);
                }
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

        public MissionScript(MissionIni ini, GameItemDb db)
        {
            Info = ini.Info;

            foreach (var s in ini.Solars)
            {
                Set(Solars, s.Nickname, ScriptSolar.FromIni(s, db));
            }

            foreach (var n in ini.NPCs)
            {
                Set(NPCs, n.Nickname, ScriptNPC.FromIni(n, db));
            }

            foreach (var s in ini.Ships)
            {
                Set(Ships, s.Nickname, ScriptShip.FromIni(s, db, NPCs));
            }

            foreach (var f in ini.Formations)
            {
                Set(Formations, f.Nickname, ScriptFormation.FromIni(f, db, Ships));
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
                Set(Dialogs, dlg.Nickname, ScriptDialog.FromIni(dlg));
            }

            foreach (var loot in ini.Loots)
            {
                Set(Loot, loot.Nickname, ScriptLoot.FromIni(loot, db));
            }

            if (ini.ShipIni != null)
            {
                foreach (var s in ini.ShipIni.ShipArches)
                {
                    Set(NpcShips, s.Nickname, ShipArch.FromIni(s, db));
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
        }
    }

    public class ScriptAiCommands : NicknameItem
    {
        public string System = "";
        public readonly List<MissionDirective> Directives = new();

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

        public ScriptAiCommands()
        {
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
