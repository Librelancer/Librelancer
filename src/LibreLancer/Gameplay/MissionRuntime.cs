// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.AI;
using LibreLancer.AI.ObjList;
using LibreLancer.Data.Missions;
using LibreLancer.Missions;

namespace LibreLancer
{

    public class MissionRuntime
    {
        public Player Player;
        MissionIni msn;
        object _msnLock = new object();

        public MissionScript Script;
        public MissionRuntime(MissionIni msn, Player player)
        {
            Script = new MissionScript(msn);
            this.msn = msn;
            this.Player = player;
            foreach (var t in Script.InitTriggers)
            {
                ActivateTrigger(t);
            }
        }

        public void ActivateTrigger(string trigger)
        {
            var t = Script.AvailableTriggers[trigger];
            foreach (var cond in t.Conditions)
            {
                if (cond.Type == TriggerConditions.Cnd_ProjHit)
                {
                    Player.WorldAction(() =>
                    {
                        var obj = Player.World.GameWorld.GetObject(cond.Entry[0].ToString());
                        if (obj != null)
                        {
                            obj.GetComponent<SNPCComponent>().ProjectileHitHook = ProjectileHit;
                        }else {
                            FLLog.Warning("Mission", $"Cnd_ProjHit won't register for not spawned {cond.Entry[0]}");   
                        }
                    });
                }
            }
            activeTriggers.Add(new ActiveTrigger()
            {
                Trigger = t,
                Conditions = new List<MissionCondition>(t.Conditions)
            });
        }

        public void DeactivateTrigger(string trigger)
        {
            var x = activeTriggers.FirstOrDefault(x => x.Trigger.Nickname.Equals(trigger, StringComparison.OrdinalIgnoreCase));
            if (x != null) x.Deactivated = true;
        }
        

        static bool CondTrue(TriggerConditions cond)
        {
            return cond == TriggerConditions.Cnd_True || cond == TriggerConditions.Cnd_SpaceExit ||
                   cond == TriggerConditions.Cnd_BaseEnter;
        }
        
        public void Update(double elapsed)
        {
            lock (_msnLock)
            {
                foreach (var t in activeTriggers)
                {
                    t.ActiveTime += elapsed;
                    for (int i = t.Conditions.Count - 1; i >= 0; i--)
                    {
                        if (t.Conditions[i].Type == TriggerConditions.Cnd_Timer &&
                            t.ActiveTime >= t.Conditions[i].Entry[0].ToSingle())
                        {
                            t.Conditions.RemoveAt(i);
                        } else if (CondTrue(t.Conditions[i].Type))
                            t.Conditions.RemoveAt(i);
                    }
                }
                CheckMissionScript();
            }
        }

        class ActiveTrigger
        {
            public ScriptedTrigger Trigger;
            public bool Deactivated;
            public List<MissionCondition> Conditions = new List<MissionCondition>();
            public double ActiveTime;
        }
        private List<ActiveTrigger> activeTriggers = new List<ActiveTrigger>();

        void ProcessCondition(TriggerConditions cond, Func<MissionCondition, bool> action)
        {
            lock (_msnLock) {
                foreach (var tr in activeTriggers) {
                    for (int i = tr.Conditions.Count - 1; i >= 0; i--)
                    {
                        if (tr.Conditions[i].Type == cond && action(tr.Conditions[i]))
                        {
                            tr.Conditions.RemoveAt(i);
                        }
                    }
                }
            }
        }
        
        void ProcessCondition(TriggerConditions cond, Func<MissionCondition, ScriptedTrigger, bool> action)
        {
            lock (_msnLock) {
                foreach (var tr in activeTriggers) {
                    for (int i = tr.Conditions.Count - 1; i >= 0; i--)
                    {
                        if (tr.Conditions[i].Type == cond && action(tr.Conditions[i], tr.Trigger))
                        {
                            tr.Conditions.RemoveAt(i);
                        }
                    }
                }
            }
        }
        
        static Func<MissionCondition, bool> TruePredicate = (c) => true;
      
        void CheckMissionScript()
        {
            List<ActiveTrigger> toRemove = new List<ActiveTrigger>();
            for (int i = activeTriggers.Count - 1; i >= 0; i--)
            {
                if (activeTriggers[i].Deactivated)
                {
                    activeTriggers.RemoveAt(i);
                } else if (activeTriggers[i].Conditions.Count == 0)
                {
                    DoTrigger(activeTriggers[i].Trigger);
                    activeTriggers.RemoveAt(i);
                }
            }
        }
        
        static bool IdEquals(string a, string b) => a.Equals(b, StringComparison.OrdinalIgnoreCase);


        void ProjectileHit(GameObject victim, GameObject attacker)
        {
            ProcessCondition(TriggerConditions.Cnd_ProjHit, (c) =>
                IdEquals(c.Entry[0].ToString(), victim.Nickname) &&
                    IdEquals(c.Entry[2].ToString(), attacker.Nickname)
            );
        }
        
        public void EnterLocation(string room, string bse)
        {
            ProcessCondition(TriggerConditions.Cnd_LocEnter, (c) => IdEquals(room, c.Entry[0].ToString()) &&
                                                                    IdEquals(bse, c.Entry[1].ToString()));
        }

        public void ClosePopup(string id)
        {
            ProcessCondition(TriggerConditions.Cnd_PopUpDialog, (c) => IdEquals(id, c.Entry[0].ToString()));
        }

        public void StoryNPCSelect(string name, string room, string _base)
        {
            ProcessCondition(TriggerConditions.Cnd_CharSelect, (c) => IdEquals(name,c.Entry[0].ToString()) &&
                                                                      IdEquals(room, c.Entry[1].ToString()) &&
                                                                      IdEquals(_base, c.Entry[2].ToString()));
        }

        public void MissionAccepted()
        {
            ProcessCondition(TriggerConditions.Cnd_MsnResponse, (c) => IdEquals("accept", c.Entry[0].ToString()));
        }
        

        public void FinishRTC(string rtc)
        {
            ProcessCondition(TriggerConditions.Cnd_RTCDone, (c) => IdEquals(rtc, c.Entry[0].ToString()));
        }

        public void EnteredSpace()
        {
            ProcessCondition(TriggerConditions.Cnd_SpaceEnter, TruePredicate);
        }
        
        void DoTrigger(ScriptedTrigger tr)
        {
            FLLog.Debug("Mission", "Running trigger " + tr.Nickname);
            foreach(var act in tr.Actions)
                act.Invoke(this, Script);
        }

        public void LineFinished(uint hash)
        {
            lock (waitingLines)
            {
                for (int i = 0; i < waitingLines.Count; i++)
                {
                    if (waitingLines[i].Hash == hash)
                    {
                        ProcessCondition(TriggerConditions.Cnd_CommComplete, (c) => IdEquals(waitingLines[i].Line, c.Entry[0].ToString()));
                        waitingLines.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        class PendingLine
        {
            public uint Hash;
            public string Line;
        }
        List<PendingLine> waitingLines = new List<PendingLine>();

        public void EnqueueLine(uint hash, string line)
        {
            lock (waitingLines)
            {
                waitingLines.Add(new PendingLine() { Hash = hash, Line = line});
            }
        }

    }
}
