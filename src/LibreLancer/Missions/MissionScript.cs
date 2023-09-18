// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Missions;
using LibreLancer.Server.Ai;
using LibreLancer.Server.Ai.ObjList;

namespace LibreLancer.Missions
{
    public class MissionScript
    {
        public MissionIni Ini;

        public Dictionary<string, NPCShipArch> NpcShips =
            new Dictionary<string, NPCShipArch>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, MissionShip> Ships =
            new Dictionary<string, MissionShip>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, MissionSolar> Solars =
            new Dictionary<string, MissionSolar>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, MissionNPC> NPCs =
            new Dictionary<string, MissionNPC>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, MissionFormation> Formations =
                new Dictionary<string, MissionFormation>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, ScriptedTrigger> AvailableTriggers =
            new Dictionary<string, ScriptedTrigger>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, ScriptAiCommands> ObjLists =
            new Dictionary<string, ScriptAiCommands>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, MissionDialog> Dialogs =
            new Dictionary<string, MissionDialog>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, NNObjective> Objectives =
            new Dictionary<string, NNObjective>(StringComparer.OrdinalIgnoreCase);

        public List<string> InitTriggers = new List<string>();

        //Set only the first one
        //Without this, order ships spawn in the wrong place in M01A
        static void Set<T>(Dictionary<string, T> dict, string k, T value)
        {
            if (!dict.ContainsKey(k)) dict[k] = value;
            else {
                FLLog.Warning("Mission", $"Duplicate {typeof(T)} `{k}`, ignoring.");
            }
        }

        public IEnumerable<MissionShip> GetShipsByLabel(string label)
        {
            foreach (var sh in Ships)
            {
                if (sh.Value.Labels.Contains(label, StringComparer.OrdinalIgnoreCase))
                {
                    yield return sh.Value;
                }
            }
        }

        public MissionScript(MissionIni ini)
        {
            this.Ini = ini;
            foreach (var s in ini.Solars)
                Set(Solars, s.Nickname, s);
            foreach (var s in ini.Ships)
                Set(Ships, s.Nickname, s);
            foreach (var n in ini.NPCs)
                Set(NPCs, n.Nickname, n);
            foreach (var f in ini.Formations)
                Set(Formations, f.Nickname, f);
            foreach(var o in ini.Objectives)
                Set(Objectives, o.Nickname, o);
            foreach (var ol in ini.ObjLists)
                Set(ObjLists, ol.Nickname, new ScriptAiCommands(ol.Nickname, ol));
            foreach (var dlg in ini.Dialogs)
                Set(Dialogs, dlg.Nickname, dlg);
            if (ini.ShipIni != null)
            {
                foreach (var s in ini.ShipIni.ShipArches)
                    NpcShips[s.Nickname] = s;
            }
            foreach (var tr in ini.Triggers)
            {
                AvailableTriggers[tr.Nickname] = new ScriptedTrigger() {
                    Nickname = tr.Nickname,
                    Repeatable = tr.Repeatable,
                    Conditions = tr.Conditions.ToArray(),
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
        public ObjList Ini;

        public AiObjListState AiState { get; private set; }

        public ScriptAiCommands(string nickname, ObjList ini)
        {
            this.Nickname = nickname;
            this.Ini = ini;
            AiState = ConvertObjList(Ini);
        }

        static AiObjListState ConvertObjList(ObjList list)
        {
            AiObjListState first = null;
            AiObjListState last = null;
            foreach (var l in list.Commands)
            {
                AiObjListState cur = null;
                switch (l.Command)
                {
                    //goto_type, x, y, z, range, BOOL, throttle
                    case ObjListCommands.GotoVec:
                    {
                        var pos = new Vector3(l.Entry[1].ToSingle(), l.Entry[2].ToSingle(), l.Entry[3].ToSingle());
                        var cruise = AiGotoKind.Goto;
                        if (l.Entry[0].ToString().Equals("goto_cruise", StringComparison.OrdinalIgnoreCase))
                            cruise = AiGotoKind.GotoCruise;
                        if (l.Entry[0].ToString().Equals("goto_no_cruise", StringComparison.OrdinalIgnoreCase))
                            cruise = AiGotoKind.GotoNoCruise;
                        float maxThrottle = 1;
                        float range = 0;
                        if (l.Entry.Count > 4)
                        {
                            range = l.Entry[4].ToSingle();
                        }
                        if (l.Entry.Count > 6)
                        {
                            maxThrottle = l.Entry[6].ToSingle() / 100.0f;
                            if (maxThrottle <= 0) maxThrottle = 1;
                        }
                        cur = new AiGotoVecState(pos, cruise, maxThrottle,range);
                        break;
                    }
                    //goto_type, target, range, BOOL, throttle
                    case ObjListCommands.GotoShip:
                    {
                        var cruise = AiGotoKind.Goto;
                        if (l.Entry[0].ToString().Equals("goto_cruise", StringComparison.OrdinalIgnoreCase))
                            cruise = AiGotoKind.GotoCruise;
                        if (l.Entry[0].ToString().Equals("goto_no_cruise", StringComparison.OrdinalIgnoreCase))
                            cruise = AiGotoKind.GotoNoCruise;
                        float maxThrottle = 1;
                        float range = 0;
                        if (l.Entry.Count > 2)
                        {
                            range = l.Entry[2].ToSingle();
                        }
                        if (l.Entry.Count > 4)
                        {
                            maxThrottle = l.Entry[4].ToSingle() / 100.0f;
                            if (maxThrottle <= 0) maxThrottle = 1;
                        }
                        cur = new AiGotoShipState(l.Entry[1].ToString(), cruise, maxThrottle, range);
                        break;
                    }
                    case ObjListCommands.GotoSpline:
                    {
                        //goto_type
                        //xyz
                        //xyz
                        //xyz
                        //xyz
                        //range
                        //BOOL
                        //throttle
                        var cruise = AiGotoKind.Goto;
                        if (l.Entry[0].ToString().Equals("goto_cruise", StringComparison.OrdinalIgnoreCase))
                            cruise = AiGotoKind.GotoCruise;
                        if (l.Entry[0].ToString().Equals("goto_no_cruise", StringComparison.OrdinalIgnoreCase))
                        cruise = AiGotoKind.GotoNoCruise;
                        var points = new Vector3[]
                        {
                            new (l.Entry[1].ToSingle(), l.Entry[2].ToSingle(), l.Entry[3].ToSingle()),
                            new (l.Entry[4].ToSingle(), l.Entry[5].ToSingle(), l.Entry[6].ToSingle()),
                            new (l.Entry[7].ToSingle(), l.Entry[8].ToSingle(), l.Entry[9].ToSingle()),
                            new (l.Entry[10].ToSingle(), l.Entry[11].ToSingle(), l.Entry[12].ToSingle()),
                        };
                        float maxThrottle = 1;
                        float range = 0;
                        if (l.Entry.Count > 13)
                        {
                            range = l.Entry[13].ToSingle();
                        }
                        if (l.Entry.Count > 15)
                        {
                            maxThrottle = l.Entry[15].ToSingle() / 100.0f;
                            if (maxThrottle <= 0) maxThrottle = 1;
                        }

                        cur = new AiGotoSplineState(points, cruise, maxThrottle, range);
                        break;
                    }
                    case ObjListCommands.Dock:
                    {
                        string exit = l.Entry.Count > 1 ? l.Entry[1].ToString() : null;
                        cur = new AiDockListState(l.Entry[0].ToString(), exit);
                        break;
                    }
                    case ObjListCommands.Delay:
                    {
                        cur = new AiDelayState(l.Entry[0].ToSingle());
                        break;
                    }
                    case ObjListCommands.BreakFormation:
                    {
                        cur = new AiBreakFormationState();
                        break;
                    }
                    case ObjListCommands.MakeNewFormation:
                    {
                        cur = new AiMakeNewFormationState()
                        {
                            FormationDef = l.Entry[0].ToString(),
                            Others = l.Entry.Skip(1).Select(x => x.ToString()).ToArray()
                        };
                        break;
                    }
                    case ObjListCommands.Follow:
                    {
                        //[1] may be range?
                        var off = new Vector3(l.Entry[2].ToSingle(), l.Entry[3].ToSingle(), l.Entry[4].ToSingle());
                        cur = new AiFollowState(l.Entry[0].ToString(), off);
                        break;
                    }
                }

                if (cur != null)
                {
                    if (first == null) first = cur;
                    if (last != null) last.Next = cur;
                    last = cur;
                }
            }
            return first;
        }
    }

    public class ScriptedTrigger
    {
        public string Nickname;
        public bool Repeatable;
        public MissionCondition[] Conditions;
        public ScriptedAction[] Actions;
    }
}
