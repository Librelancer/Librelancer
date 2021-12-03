// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.AI;
using LibreLancer.AI.ObjList;
using LibreLancer.Data.Missions;
using LibreLancer.Missions;

namespace LibreLancer
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
        
        public List<string> InitTriggers = new List<string>();

        public MissionScript(MissionIni ini)
        {
            this.Ini = ini;
            foreach (var s in ini.Solars)
                Solars[s.Nickname] = s;
            foreach (var s in ini.Ships)
                Ships[s.Nickname] = s;
            foreach (var n in ini.NPCs)
                NPCs[n.Nickname] = n;
            foreach (var f in ini.Formations)
                Formations[f.Nickname] = f;
            foreach (var ol in ini.ObjLists)
                ObjLists[ol.Nickname] = new ScriptAiCommands() {
                    Nickname = ol.Nickname,
                    Ini = ol
                };
            foreach (var dlg in ini.Dialogs)
                Dialogs[dlg.Nickname] = dlg;
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
        public AiObjListState Construct() => ConvertObjList(Ini);
        static AiObjListState ConvertObjList(ObjList list)
        {
            AiObjListState last = null;
            foreach (var l in list.Commands)
            {
                switch (l.Command)
                {
                    case ObjListCommands.GotoVec:
                    {
                        var pos = new Vector3(l.Entry[1].ToSingle(), l.Entry[2].ToSingle(), l.Entry[3].ToSingle());
                        var cruise = !l.Entry[0].ToString()
                            .Equals("goto_no_cruise", StringComparison.OrdinalIgnoreCase);
                        float maxThrottle = 1;
                        if (l.Entry.Count > 6)
                        {
                            maxThrottle = l.Entry[6].ToSingle() / 100.0f;
                            if (maxThrottle <= 0) maxThrottle = 1;
                        }
                        var cur = new AiGotoVecState(pos, cruise, maxThrottle);
                        if (last != null) last.Next = cur;
                        last = cur;
                        break;
                    }
                    case ObjListCommands.GotoSpline:
                    {
                        var cruise = !l.Entry[0].ToString()
                            .Equals("goto_no_cruise", StringComparison.OrdinalIgnoreCase);
                        var points = new Vector3[]
                        {
                            new (l.Entry[1].ToSingle(), l.Entry[2].ToSingle(), l.Entry[3].ToSingle()),
                            new (l.Entry[4].ToSingle(), l.Entry[5].ToSingle(), l.Entry[6].ToSingle()),
                            new (l.Entry[7].ToSingle(), l.Entry[8].ToSingle(), l.Entry[9].ToSingle()),
                            new (l.Entry[10].ToSingle(), l.Entry[11].ToSingle(), l.Entry[12].ToSingle()),
                        };
                        float maxThrottle = 1;
                        if (l.Entry.Count > 15)
                        {
                            maxThrottle = l.Entry[15].ToSingle() / 100.0f;
                            if (maxThrottle <= 0) maxThrottle = 1;
                        }
                        var cur = new AiGotoSplineState(points, cruise, maxThrottle);
                        if (last != null) last.Next = cur;
                        last = cur;
                        break;
                    }
                    case ObjListCommands.Delay:
                    {
                        var cur = new AiDelayState(l.Entry[0].ToSingle());
                        if (last != null) last.Next = cur;
                        last = cur;
                        break;
                    }
                }
            }
            return last;
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