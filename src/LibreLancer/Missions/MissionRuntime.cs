// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.Data.Save;
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
        public string currentSystem;

        public MissionScript Script;
        public Random Random = new();

        public Dictionary<string, MissionLabel> Labels = new(StringComparer.OrdinalIgnoreCase);
        public MissionRuntime(MissionIni msn, Player player, uint[] triggerSave)
        {
            Script = new MissionScript(msn);
            foreach (var lbl in Script.GetLabels())
            {
                Labels[lbl.Name] = lbl;
            }
            this.msn = msn;
            this.Player = player;
            bool doInit = true;
            {
                foreach (var tr in triggerSave)
                {
                    bool found = false;
                    foreach (var t in Script.AvailableTriggers)
                    {
                        if (FLHash.CreateID(t.Key) == tr)
                        {
                            FLLog.Info("Mission", $"Loading from trigger {t.Key} (hash: {tr})");
                            ActivateTrigger(t.Key);
                            doInit = false;
                            found = true;
                            break;
                        }
                    }
                    if (!found) FLLog.Error("Save", $"Unable to find trigger with hash {tr} - this trigger will not be restored");
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

        private string lastSaveTrigger = string.Empty;

        public void WriteActiveTriggers(SaveGame sg)
        {
            FLLog.Info("Mission", $"WriteActiveTriggers called - lastSaveTrigger: '{lastSaveTrigger}'");

            //if we are in space, we only want to save the last save trigger executed, if we are not, save all
            if (Player.Space != null)
            {
                FLLog.Info("Mission", "Player is in space, saving only the last save trigger");
                if (!string.IsNullOrEmpty(lastSaveTrigger))
                {
                    sg.TriggerSave.Add(new TriggerSave() { Trigger = (int)FLHash.CreateID(lastSaveTrigger) });
                    FLLog.Info("Mission", $"Saved mission save trigger: {lastSaveTrigger} (hash: {FLHash.CreateID(lastSaveTrigger)})");
                }
            }
            else
            {
                FLLog.Info("Mission", "Player is not in space, saving all active triggers");
                foreach (var at in activeTriggers)
                {
                    sg.TriggerSave.Add(new TriggerSave() { Trigger = (int)FLHash.CreateID(at.Trigger.Nickname) });
                    FLLog.Info("Mission", $"Saved active trigger: {at.Trigger.Nickname} (hash: {FLHash.CreateID(at.Trigger.Nickname)})");
                }
            }

            FLLog.Info("Mission", $"Total mission save triggers saved: {sg.TriggerSave.Count}, space status: {(Player.Space != null ? "in space" : "not in space")}");
        }

        public void RegisterSaveTrigger(string triggerName)
        {
            // When a save trigger is reached, we register it as the last save trigger
            if (!string.IsNullOrEmpty(triggerName))
            {
                lastSaveTrigger = triggerName;
                FLLog.Info("Mission", $"Registered active save trigger: {triggerName}");
            }
        }

        public void RegisterHitEvent(string target)
        {
            Player.MissionWorldAction(() =>
            {
                var obj = Player.Space.World.GameWorld.GetObject(target);
                if (obj != null)
                {
                    if (obj.TryGetComponent <SHealthComponent>(out var comp))
                    {
                        comp.ProjectileHitHook = OnProjectileHit;
                    }
                    else
                    {
                        // This could still be wrong (?)
                        FLLog.Error("Mission", $"Cnd_ProjHit won't register for invincible {target}");
                    }
                }
                else
                {
                    FLLog.Error("Mission", $"Cnd_ProjHit won't register for not spawned {target}");
                }
            });
        }

        public void ActivateTrigger(string trigger)
        {
            FLLog.Info("Mission", $"ActivateTrigger called for: {trigger}");

            // Don't reactivate deactivated triggers
            if (deactivatedTriggers.Contains(trigger))
            {
                FLLog.Info("Mission", $"Trigger {trigger} is in deactivated list, not activating");
                return;
            }

            // Don't reactivate completed non-repeatable triggers
            if (completedTriggers.Contains(trigger))
            {
                FLLog.Info("Mission", $"Trigger {trigger} is already completed, not activating");
                return;
            }

            // Don't activate already active triggers
            if (activeTriggers.Any(at => at.Trigger.Nickname.Equals(trigger, StringComparison.OrdinalIgnoreCase)))
            {
                FLLog.Info("Mission", $"Trigger {trigger} is already active, not activating");
                return;
            }

            var t = Script.AvailableTriggers[trigger];
            if (t.Conditions.Length == 1 && t.Conditions[0] is Cnd_True)
            {
                FLLog.Info("Mission", $"Trigger {trigger} has only Cnd_True condition, executing immediately");
                DoTrigger(t);
                return;
            }

            FLLog.Info("Mission", $"Adding trigger {trigger} to active triggers list");
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
            FLLog.Info("Mission", $"DeactivateTrigger called for: {trigger}");
            foreach (var at in activeTriggers)
            {
                FLLog.Info("Mission", $"Active trigger: {at.Trigger.Nickname}");
            }
            FLLog.Info("Mission", $"Current deactivated triggers count: {deactivatedTriggers.Count}");
            foreach (var dt in deactivatedTriggers)
            {
                FLLog.Info("Mission", $"Deactivated trigger: {dt}");
            }

            var x = activeTriggers.FirstOrDefault(x => x.Trigger.Nickname.Equals(trigger, StringComparison.OrdinalIgnoreCase));
            if (x != null)
            {
                FLLog.Info("Mission", $"Found trigger {trigger} in active list, deactivating it");
                x.Deactivated = true;
                activeTriggers.Remove(x);
            }
            else
            {
                FLLog.Info("Mission", $"Trigger {trigger} not found in active list, adding to deactivated list");
                deactivatedTriggers.Add(trigger);
            }
        }

        public TriggerState GetTriggerState(string trigger)
        {
            if (completedTriggers.Contains(trigger))
            {
                return TriggerState.COMPLETE;
            }
            var at = activeTriggers.FirstOrDefault(x => x.Trigger.Nickname.Equals(trigger, StringComparison.OrdinalIgnoreCase));
            if (at == null)
            {
                return TriggerState.OFF;
            }
            return TriggerState.ON;
        }

        public void SpaceExit() => MsnEvent(new SpaceExitedEvent());

        public void BaseEnter(string _base) => MsnEvent(new BaseEnteredEvent(_base));

        public void CargoScanned(string scanningShip, string scannedShip) => MsnEvent(new CargoScannedEvent(scanningShip, scannedShip));

        public bool GetSpace(out SpacePlayer space)
        {
            space = Player.Space;
            return space != null;
        }

        public void SystemEnter(string system, string ship)
        {
            string previous = currentSystem;
            currentSystem = system;
            FLLog.Debug("Mission", $"SystemEnter: {system}, previous: {previous}");
            MsnEvent(new SystemEnteredEvent(system, ship, previous));
        }

        public void Update(double elapsed)
        {
            lock (_msnLock)
            {
                foreach (var t in activeTriggers)
                {
                    if (!string.IsNullOrEmpty(t.Trigger.System) && !t.Trigger.System.Equals(Player.System, StringComparison.OrdinalIgnoreCase))
                        continue;
                    t.ActiveTime += elapsed;
                    var newSatisfied = new BitArray128();
                    for (int i = 0; i < t.Conditions.Count; i++)
                    {
                        var condState =  t.Conditions[i].Condition.CheckCondition(this, t.Conditions[i], elapsed);
                        #if DEBUG
                        if (!t.Satisfied[i] && condState)
                        {
                            FLLog.Debug("Mission", $"{t.Trigger.Nickname} satisfied cnd: {t.Conditions[i].Condition}");
                        }
                        #endif
                        newSatisfied[i] = condState;
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
                    ti.Conditions.Add(c.Condition.ToString());
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
        private HashSet<string> deactivatedTriggers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void MsnEvent<T>(T e) where T : struct
        {
            lock (_msnLock) {
                foreach (var tr in activeTriggers) {
                    for (int i = tr.Conditions.Count - 1; i >= 0; i--)
                    {
                        if (tr.Conditions[i].Condition is EventListenerCondition<T> listener)
                        {
                            listener.OnEvent(e, this, tr.Conditions[i]);
                            tr.Satisfied[i] = listener.CheckCondition(this, tr.Conditions[i], 0);
                        }
                    }
                }
                CheckMissionScript();
            }
        }

        private HashSet<string> completedTriggers = new();
        private bool uiUpdate = false;

        public void CheckMissionScript()
        {
            // Create a copy of the list to avoid issues with modification during iteration
            // reason: triggers may deactivate themselves or others while they are being processed
            var triggersToProcess = new List<ActiveTrigger>(activeTriggers);
        
            activeTriggers.Clear();

            foreach (var trigger in triggersToProcess)
            {
                // Check if this trigger was deactivated during previous processing in this cycle
                if (deactivatedTriggers.Contains(trigger.Trigger.Nickname))
                {
                    trigger.Deactivated = true;
                }

                if (trigger.Deactivated)
                {
                    uiUpdate = true;
                }
                else
                {
                    bool activate = true;
                    for (int j = 0; j < trigger.Conditions.Count; j++)
                    {
                        if (!trigger.Satisfied[j])
                        {
                            activate = false;
                            break;
                        }
                    }

                    if (activate)
                    {
                        DoTrigger(trigger.Trigger);
                        if (!trigger.Trigger.Repeatable)
                        {
                            completedTriggers.Add(trigger.Trigger.Nickname);
                        }
                        else
                        {
                            if (!trigger.Deactivated)
                            {
                                // Reset repeatable trigger
                                trigger.ActiveTime = 0;
                                trigger.Satisfied = new BitArray128();
                                activeTriggers.Add(trigger); // Keep it active
                            }
                        }
                        uiUpdate = true;
                    }
                    else
                    {
                        activeTriggers.Add(trigger);
                    }
                }
            }
        }

        public void PlayerLaunch()
        {
            MsnEvent(new PlayerLaunchedEvent());
        }

        public void LaunchComplete(string nickname)
        {
            MsnEvent(new LaunchCompleteEvent(nickname));
        }

        void OnProjectileHit(GameObject victim, GameObject attacker)
        {
            MsnEvent(new ProjectileHitEvent(victim.Nickname, attacker.Nickname));
        }

        public void EnterLocation(string room, string _base)
        {
            MsnEvent(new LocationEnteredEvent(room, _base));
        }

        public void ExitLocation(string room, string _base)
        {
            MsnEvent(new LocationExitedEvent(room, _base));
        }

        public void ClosePopup(string id)
        {
            MsnEvent(new ClosePopupEvent(id));
        }

        public void StoryNPCSelect(string name, string room, string _base)
        {
            MsnEvent(new CharSelectEvent(name, room, _base));
        }

        public void ObjectSpawned(string ship)
        {
            foreach (var l in Labels.Values)
            {
                l.Spawned(ship);
            }
        }

        public void ObjectDestroyed(string nickname)
        {
            foreach (var l in Labels.Values)
            {
                l.Destroyed(nickname);
            }
            MsnEvent(new DestroyedEvent(nickname));
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

        public void LootAcquired(string lootNickname, string acquirerShip)
        {
            FLLog.Info("Mission", $"Loot acquired: {lootNickname} by {acquirerShip}");
            MsnEvent(new LootAcquiredEvent(lootNickname, acquirerShip));
        }

        public void EnteredSpace()
        {
            MsnEvent(new SpaceEnteredEvent());
        }


        void DoTrigger(ScriptedTrigger tr)
        {
            FLLog.Info("Mission", "Running trigger " + tr.Nickname);
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
