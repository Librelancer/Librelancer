// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using LibreLancer.Data.Missions;

namespace LibreLancer
{

    public class MissionRuntime
    {
        Player player;
        MissionIni msn;
        object _msnLock = new object();
        public MissionRuntime(MissionIni msn, Player player)
        {
            this.msn = msn;
            this.player = player;
            foreach (var t in msn.Triggers)
            {
                if (t.InitState == TriggerInitState.ACTIVE)
                {
                    ActivateTrigger(t);
                }
            }
        }

        void ActivateTrigger(string trigger)
        {
            var tr = msn.Triggers.FirstOrDefault(x => trigger.Equals(x.Nickname, StringComparison.OrdinalIgnoreCase));
            if (tr != null)
                ActivateTrigger(tr);
            else
                FLLog.Error("Mission", $"Failed to activate unknown trigger {trigger}");
        }
        
        void ActivateTrigger(MissionTrigger trigger)
        {
            activeTriggers.Add(new ActiveTrigger()
            {
                Trigger = trigger,
                Conditions = new List<MissionCondition>(trigger.Conditions)
            });
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
            public MissionTrigger Trigger;
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
        
        void ProcessCondition(TriggerConditions cond, Func<MissionCondition, MissionTrigger, bool> action)
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
      
        void CheckMissionScript()
        {
            for (int i = activeTriggers.Count - 1; i >= 0; i--)
            {
                if (activeTriggers[i].Conditions.Count == 0)
                {
                    DoTrigger(activeTriggers[i].Trigger);
                    activeTriggers.RemoveAt(i);
                }
            }
        }
        
        static bool IdEquals(string a, string b) => a.Equals(b, StringComparison.OrdinalIgnoreCase);

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

        public void FinishRTC(string rtc)
        {
            ProcessCondition(TriggerConditions.Cnd_RTCDone, (c) => IdEquals(rtc, c.Entry[0].ToString()));
        }

        public void EnteredSpace()
        {
            ProcessCondition(TriggerConditions.Cnd_SpaceEnter, (c, tr) => IdEquals("FP7_System", tr.System));
        }
        
        void DoTrigger(MissionTrigger tr)
        {
            FLLog.Debug("Mission", "Running trigger " + tr.Nickname);

            foreach (var act in tr.Actions)
            {
                switch(act.Type)
                {
                    case TriggerActions.Act_ActTrig:
                        var trname = act.Entry[0].ToString();
                        ActivateTrigger(trname);
                        break;
                    case TriggerActions.Act_PlaySoundEffect:
                        player.PlaySound(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_ForceLand:
                        player.ForceLand(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_AdjAcct:
                        player.Character.UpdateCredits(player.Character.Credits + act.Entry[0].ToInt32());
                        break;
                    case TriggerActions.Act_SpawnSolar:
                        SpawnSolar(act.Entry[0].ToString(), player);
                        break;
                    case TriggerActions.Act_StartDialog:
                        RunDialog(msn.Dialogs.First((x) => x.Nickname.Equals(act.Entry[0].ToString(), StringComparison.OrdinalIgnoreCase)));
                        break;
                    case TriggerActions.Act_MovePlayer:
                        //gameplay.player.Transform = Matrix4.CreateTranslation(act.Entry[0].ToSingle(), act.Entry[1].ToSingle(), act.Entry[2].ToSingle());
                        //last param seems to always be one?
                        break;
                    case TriggerActions.Act_LightFuse:
                        player.WorldAction(() =>
                        {
                            var fuse = player.World.Server.GameData.GetFuse(act.Entry[1].ToString());
                            var gameObj = player.World.GameWorld.GetObject(act.Entry[0].ToString());
                            var fzr = new SFuseRunnerComponent(gameObj) { Fuse = fuse };
                            gameObj.Components.Add(fzr);
                            fzr.Run();
                        });
                        break;
                    case TriggerActions.Act_PopUpDialog:
                    {
                        var title = act.Entry[0].ToInt32();
                        var contents = act.Entry[1].ToInt32();
                        var id = act.Entry[2].ToString();
                        player.RemoteClient.PopupOpen(title, contents, id);
                        break;
                    }
                    case TriggerActions.Act_PlayMusic:
                        player.PlayMusic(act.Entry[3].ToString());
                        break;
                    case TriggerActions.Act_CallThorn:
                        player.CallThorn(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_AddRTC:
                        player.AddRTC(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_SetShipAndLoadout:
                        if(!act.Entry[0].ToString().Equals("none", StringComparison.OrdinalIgnoreCase))
                        {
                            if (player.Game.GameData.TryGetLoadout(act.Entry[1].ToString(), out var loadout))
                            {
                                player.Character.Ship = player.Game.GameData.GetShip(act.Entry[0].ToString());
                                player.Character.Equipment = new List<NetEquipment>();
                                foreach (var equip in loadout.Equip)
                                {
                                    var e = player.Game.GameData.GetEquipment(equip.Nickname);
                                    if (e == null) continue;
                                    player.Character.Equipment.Add(new NetEquipment()
                                    {
                                        Equipment = e,
                                        Hardpoint = equip.Hardpoint,
                                        Health = 1f
                                    });
                                }
                            }
                        }
                        break;
                }
            }
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
        void RunDialog(MissionDialog dlg)
        {
            var netdlg = new NetDlgLine[dlg.Lines.Count];
            for (int i = 0; i < dlg.Lines.Count; i++)
            {
                var d = dlg.Lines[i];
                var src = msn.Ships.First((x) => x.Nickname.Equals(d.Source, StringComparison.OrdinalIgnoreCase));
                var npc = msn.NPCs.First((x) => x.Nickname.Equals(src.NPC, StringComparison.OrdinalIgnoreCase));
                var hash = FLHash.CreateID(d.Line);
                lock (waitingLines)
                {
                    waitingLines.Add(new PendingLine() { Hash = hash, Line = d.Line});
                }
                netdlg[i] = new NetDlgLine() {
                    Voice = npc.Voice,
                    Hash = hash
                };
            }
            player.PlayDialog(netdlg);
        }
        void SpawnSolar(string solarname, Player p)
        {
            var sol = msn.Solars.First(x => x.Nickname.Equals(solarname, StringComparison.OrdinalIgnoreCase));
            var arch = sol.Archetype;
            p.WorldAction(() => { p.World.SpawnSolar(sol.Nickname, arch, sol.Loadout, sol.Position, sol.Orientation); });
        }
    }
}
