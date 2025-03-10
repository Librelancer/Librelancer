// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Missions;
using LibreLancer.Data.Save;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;
using LibreLancer.Missions.Events;
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

        public void RegisterHitEvent(string target)
        {
            Player.MissionWorldAction(() =>
            {
                var obj = Player.Space.World.GameWorld.GetObject(target);
                if (obj != null)
                {
                    obj.GetComponent<SNPCComponent>().ProjectileHitHook = OnProjectileHit;
                }else {
                    FLLog.Warning("Mission", $"Cnd_ProjHit won't register for not spawned {target}");
                }
            });
        }

        public void ActivateTrigger(string trigger)
        {
            var t = Script.AvailableTriggers[trigger];
            var active = new ActiveTrigger() { Trigger = t };
            var conds = new List<ActiveCondition>();
            foreach (var cond in t.Conditions)
            {
                var ac = new ActiveCondition() { Trigger = active, Condition = cond };
                cond.Init(this, ac);
                conds.Add(ac);
            }
            active.Conditions = conds;
            activeTriggers.Add(active);
        }

        public void DeactivateTrigger(string trigger)
        {
            var x = activeTriggers.FirstOrDefault(x => x.Trigger.Nickname.Equals(trigger, StringComparison.OrdinalIgnoreCase));
            if (x != null)
            {
                x.Deactivated = true;
            }
        }

        public TriggerState GetTriggerState(string trigger)
        {
            var at = activeTriggers.FirstOrDefault(x => x.Trigger.Nickname.Equals(trigger, StringComparison.OrdinalIgnoreCase));
            if (at == null)
            {
                return TriggerState.OFF;
            }
            return TriggerState.ON;
        }

        public void SpaceExit() => MsnEvent(new SpaceExitedEvent());

        public void BaseEnter(string _base) => MsnEvent(new BaseEnteredEvent(_base));

        public bool GetSpace(out SpacePlayer space)
        {
            space = Player.Space;
            return space != null;
        }

        public void Update(double elapsed)
        {
            lock (_msnLock)
            {
                foreach (var t in activeTriggers)
                {
                    t.ActiveTime += elapsed;
                    var newSatisfied = t.Satisfied;
                    for (int i = 0; i < t.Conditions.Count; i++)
                    {
                        newSatisfied[i] = t.Conditions[i].Condition.CheckCondition(this, t.Conditions[i], elapsed);
                    }
                    if (t.Satisfied != newSatisfied)
                    {
                        uiUpdate = true;
                    }
                    t.Satisfied = newSatisfied;
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
                var ti = new TriggerInfo() { Name = t.Trigger.Nickname, Satisfied = t.Satisfied };
                foreach (var a in t.Trigger.Actions)
                {
                    ti.Actions.Add(a.Text);
                }
                var sb = new IniBuilder.IniSectionBuilder() { Section = new("") };
                foreach (var c in t.Conditions)
                {
                    sb.Section.Clear();
                    c.Condition.Write(sb);
                    ti.Conditions.Add(sb.Section[0].ToString());
                }
                yield return ti;
            }
        }

        public class TriggerInfo
        {
            public string Name;
            public BitArray128 Satisfied;
            public List<string> Actions = new List<string>();
            public List<string> Conditions = new List<string>();
        }

        private List<ActiveTrigger> activeTriggers = new List<ActiveTrigger>();

        void MsnEvent<T>(T e) where T : struct
        {
            lock (_msnLock) {
                foreach (var tr in activeTriggers) {
                    for (int i = tr.Conditions.Count - 1; i >= 0; i--)
                    {
                        if (tr.Conditions[i].Condition is EventListenerCondition<T> listener)
                        {
                            listener.OnEvent(e, this, tr.Conditions[i]);
                        }
                    }
                }
                CheckMissionScript();
            }
        }

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
                else
                {
                    bool activate = true;
                    for (int j = 0; j < activeTriggers[i].Conditions.Count; j++)
                    {
                        if (!activeTriggers[i].Satisfied[j])
                        {
                            activate = false;
                            break;
                        }
                    }
                    if (activate)
                    {
                        var tr = activeTriggers[i].Trigger;
                        activeTriggers.RemoveAt(i);
                        DoTrigger(tr);
                        uiUpdate = true;
                    }
                }
            }
        }

        static bool IdEquals(string a, string b) => a.Equals(b, StringComparison.OrdinalIgnoreCase);


        public void PlayerLaunch()
        {
            MsnEvent(new PlayerLaunchedEvent());
        }

        void OnProjectileHit(GameObject victim, GameObject attacker)
        {
            MsnEvent(new ProjectileHitEvent(victim.Nickname, attacker.Nickname));
        }

        public void EnterLocation(string room, string _base)
        {
            MsnEvent(new LocationEnteredEvent(room, _base));
        }

        public void ClosePopup(string id)
        {
            MsnEvent(new ClosePopupEvent(id));
        }

        public void StoryNPCSelect(string name, string room, string _base)
        {
            MsnEvent(new CharSelectEvent(name, room, _base));
        }

        public void NpcKilled(string ship)
        {
            MsnEvent(new DestroyedEvent(ship));
        }

        public void TradelaneEntered(string ship, string pointA, string pointB)
        {
            MsnEvent(new TLEnteredEvent(ship, pointA, pointB));
        }

        public void TradelaneExited(string ship, string lane)
        {
            MsnEvent(new TLExitedEvent(ship, lane));
        }

        public void PlayerManeuver(ManeuverType type, string target)
        {
            MsnEvent(new PlayerManeuverEvent(type, target));
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
                MsnEvent(new DestroyedEvent(label));
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
            MsnEvent(new MissionResponseEvent(true));
        }

        public void MissionRejected()
        {
            MsnEvent(new MissionResponseEvent(false));
        }


        public void FinishRTC(string rtc)
        {
            MsnEvent(new RTCDoneEvent(rtc));
        }

        public void EnteredSpace()
        {
            MsnEvent(new SpaceEnteredEvent());
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
                        MsnEvent(new CommCompleteEvent(waitingLines[i].Line));
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
