// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Missions;
using LibreLancer.Data.Save;
using LibreLancer.Server;
using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Missions
{

    public class MissionRuntime
    {
        public Player Player;
        MissionIni msn;
        object _msnLock = new object();

        public MissionScript Script;
        public MissionRuntime(MissionIni msn, Player player, uint[] triggerSave)
        {
            Script = new MissionScript(msn);
            this.msn = msn;
            this.Player = player;
            bool doInit = true;
            if (triggerSave != null && triggerSave.Length > 0)
            {
                foreach (var tr in triggerSave)
                {
                    bool found = false;
                    foreach (var t in Script.AvailableTriggers)
                    {
                        if (FLHash.CreateID(t.Key) == tr)
                        {
                            FLLog.Debug("Mission", $"Loading from trigger {t.Key}");
                            ActivateTrigger(t.Key);
                            doInit = false;
                            found = true;
                            break;
                        }
                    }
                    if (!found) FLLog.Error("Save", $"Unable to find trigger {triggerSave}");
                }
            }
            if (doInit)
            {
                FLLog.Debug("Mission", "Loading init triggers");
                foreach (var t in Script.InitTriggers)
                {
                    ActivateTrigger(t);
                }
            }
            UpdateUiTriggers();
        }

        public void WriteActiveTriggers(SaveGame sg)
        {
            foreach (var t in activeTriggers)
            {
                sg.TriggerSave.Add(new TriggerSave() { Trigger = (int)FLHash.CreateID(t.Trigger.Nickname)});
            }
        }

        public void ActivateTrigger(string trigger)
        {
            var t = Script.AvailableTriggers[trigger];
            foreach (var cond in t.Conditions)
            {
                if (cond.Type == TriggerConditions.Cnd_ProjHit)
                {
                    Player.MissionWorldAction(() =>
                    {
                        var obj = Player.Space.World.GameWorld.GetObject(cond.Entry[0].ToString());
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
            if (x != null)
            {
                x.Deactivated = true;
            }
        }

        public void SpaceExit() =>  ProcessCondition(TriggerConditions.Cnd_SpaceExit, TruePredicate);

        public void BaseEnter(string _base) => ProcessCondition(TriggerConditions.Cnd_BaseEnter,
            (x) => IdEquals(x.Entry[0].ToString(), _base));

        bool CheckPerTickCond(TriggerConditions cond, MissionCondition data, float time)
        {
            if (cond == TriggerConditions.Cnd_True)
                return true;

            if (cond == TriggerConditions.Cnd_WatchTrigger)
            {
                bool on = data.Entry[1].ToString().Equals("on", StringComparison.OrdinalIgnoreCase);
                if (on)
                {
                    return activeTriggers.Any(x => x.Trigger.Nickname.Equals(
                        data.Entry[0].ToString(), StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    return !activeTriggers.Any(x => x.Trigger.Nickname.Equals(
                        data.Entry[0].ToString(), StringComparison.OrdinalIgnoreCase));
                }
            }

            if (cond == TriggerConditions.Cnd_DistVec)
            {
                if (Player.Space == null) return false;

                bool inside = data.Entry[0].ToString() == "inside";
                var objA = Player.Space.World.GameWorld.GetObject(data.Entry[1].ToString());
                if (objA == null) return false;
                var point = new Vector3(data.Entry[2].ToSingle(), data.Entry[3].ToSingle(), data.Entry[4].ToSingle());
                var d = data.Entry[5].ToSingle();
                bool satisfied;
                if (Vector3.Distance(objA.LocalTransform.Position, point) < (d * d))
                    satisfied = inside;
                else
                    satisfied = !inside;
                return satisfied;
            }
            if (cond == TriggerConditions.Cnd_DistShip)
            {
                if (Player.Space == null) return false;

                bool inside = data.Entry[0].ToString() == "inside";
                var objA = Player.Space.World.GameWorld.GetObject(data.Entry[1].ToString());
                var objB = Player.Space.World.GameWorld.GetObject(data.Entry[2].ToString());
                if (objA == null || objB == null) return false;

                var d = data.Entry[3].ToSingle();
                d *= d;
                bool satisfy;
                if (Vector3.DistanceSquared(objA.LocalTransform.Position, objB.LocalTransform.Position) < d)
                    satisfy = inside;
                else
                    satisfy = !inside;
                if (data.Entry.Count > 5 &&
                    IdEquals(data.Entry[5].ToString(), "TICK_AWAY"))
                {
                    if (satisfy) {
                        data.Data += time;
                        if (data.Data > data.Entry[4].ToSingle()) return true;
                    }
                    return false;
                }
                return satisfy;
            }

            return false;
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
                            FLLog.Debug("Mission", $"Satisfied - {t.Trigger.Nickname}: {t.Conditions[i].Entry}");
                            t.Conditions.RemoveAt(i);
                        } else if (CheckPerTickCond(t.Conditions[i].Type, t.Conditions[i], (float) elapsed))
                        {
                            FLLog.Debug("Mission", $"Satisfied - {t.Trigger.Nickname}: {t.Conditions[i].Entry}");
                            t.Conditions.RemoveAt(i);
                        }
                    }
                }
                CheckMissionScript();
                if (uiUpdate)
                {
                    uiUpdate = false;
                    UpdateUiTriggers();
                }
            }
        }

        public TriggerInfo[] ActiveTriggersInfo = new TriggerInfo[0];
        void UpdateUiTriggers()
        {
            ActiveTriggersInfo = GetTriggerInfo().ToArray();
        }

        IEnumerable<TriggerInfo> GetTriggerInfo()
        {
            foreach (var t in activeTriggers)
            {
                var ti = new TriggerInfo() {Name = t.Trigger.Nickname};
                foreach (var a in t.Trigger.Actions)
                {
                    ti.Actions.Add(a.Text);
                }
                foreach (var c in t.Conditions)
                {
                    ti.Conditions.Add(c.Entry.ToString());
                }
                yield return ti;
            }
        }

        public class TriggerInfo
        {
            public string Name;
            public List<string> Actions = new List<string>();
            public List<string> Conditions = new List<string>();
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
                            FLLog.Debug("Mission", $"Satisfied - {tr.Trigger.Nickname}: {tr.Conditions[i].Entry}");
                            tr.Conditions.RemoveAt(i);
                            uiUpdate = true;
                        }
                    }
                }
            }
        }

        static Func<MissionCondition, bool> TruePredicate = (c) => true;

        private bool uiUpdate = false;

        public void CheckMissionScript()
        {
            for (int i = activeTriggers.Count - 1; i >= 0; i--)
            {
                if (activeTriggers[i].Deactivated)
                {
                    activeTriggers.RemoveAt(i);
                    uiUpdate = true;
                }
                else if (activeTriggers[i].Conditions.Count == 0)
                {
                    var tr = activeTriggers[i].Trigger;
                    activeTriggers.RemoveAt(i);
                    DoTrigger(tr);
                    uiUpdate = true;
                }
            }
        }

        static bool IdEquals(string a, string b) => a.Equals(b, StringComparison.OrdinalIgnoreCase);


        public void PlayerLaunch()
        {
            ProcessCondition(TriggerConditions.Cnd_PlayerLaunch, TruePredicate);
        }

        void ProjectileHit(GameObject victim, GameObject attacker)
        {
            ProcessCondition(TriggerConditions.Cnd_ProjHit, (c) =>
                c.Entry.Count > 2 && //Need to include projectile hit counter for other form
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

        public void NpcKilled(string ship)
        {
            ProcessCondition(TriggerConditions.Cnd_Destroyed, (c) => IdEquals(ship, c.Entry[0].ToString()));
        }

        public void TradelaneEntered(string ship, string pointA, string pointB)
        {
            ProcessCondition(TriggerConditions.Cnd_TLEntered, (c) =>
            {
                return IdEquals(c.Entry[0].ToString(), ship) &&
                       IdEquals(c.Entry[1].ToString(), pointA) &&
                       IdEquals(c.Entry[2].ToString(), pointB);
            });
        }

        public void TradelaneExited(string ship, string lane)
        {
            ProcessCondition(TriggerConditions.Cnd_TLExited, (c) =>
            {
                return IdEquals(c.Entry[0].ToString(), ship) &&
                       IdEquals(c.Entry[1].ToString(), lane);
            });
        }

        public void PlayerManeuver(string type, string target)
        {
            ProcessCondition(TriggerConditions.Cnd_PlayerManeuver, (c) =>
            {
                return IdEquals(c.Entry[0].ToString(), type) &&
                       IdEquals(c.Entry[1].ToString(), target);
            });
        }

        //TODO: Bad tracking

        private Dictionary<string, int> labelCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public void LabelIncrement(string label)
        {
            labelCounts.TryGetValue(label, out int c);
            c++;
            labelCounts[label] = c;
        }

        public void LabelKilled(string label)
        {
            labelCounts.TryGetValue(label, out int c);
            c--;
            if (c <= 0)
            {
                c = 0;
                ProcessCondition(TriggerConditions.Cnd_Destroyed, (c) => IdEquals(label, c.Entry[0].ToString()));
            }
            labelCounts[label] = c;
        }

        public void LabelDecrement(string label)
        {
            labelCounts.TryGetValue(label, out int c);
            c--;
            if (c <= 0) c = 0;
            labelCounts[label] = c;
        }


        public void MissionAccepted()
        {
            Player.Story?.MissionAccepted(Player);
            ProcessCondition(TriggerConditions.Cnd_MsnResponse, (c) => IdEquals("accept", c.Entry[0].ToString()));
        }

        public void MissionRejected()
        {
            ProcessCondition(TriggerConditions.Cnd_MsnResponse, (c) => IdEquals("reject", c.Entry[0].ToString()));
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
