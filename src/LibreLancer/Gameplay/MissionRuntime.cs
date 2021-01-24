// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

using LibreLancer.Data.Missions;
namespace LibreLancer
{

    public class MissionRuntime
    {
        class MissionTimer
        {
            public double T;
        }

        Player player;
        MissionIni msn;
        object _msnLock = new object();
        public MissionRuntime(MissionIni msn, Player player)
        {
            this.msn = msn;
            this.player = player;
            triggered = new BitArray(msn.Triggers.Count);
            active = new BitArray(msn.Triggers.Count);
        }

        public void Update(double elapsed)
        {
            lock (_msnLock)
            {
                foreach (var t in timers)
                {
                    t.Value.T += elapsed;
                }
                CheckMissionScript();
            }
        }

        public void EnsureLoaded()
        {
            /*foreach (var tr in msn.Triggers)
            {
                foreach (var act in tr.Actions)
                {
                    if(act.Type == TriggerActions.Act_PlaySoundEffect)
                        session.Game.Sound.LoadSound(act.Entry[0].ToString());
                    if(act.Type == TriggerActions.Act_LightFuse)
                        session.Game.GameData.GetFuse(act.Entry[1].ToString());
                }
            }*/
        }

        //Implement just enough of the mission script to get the player to a base from
        //FP7_system in vanilla, and set the loadout on disco
        BitArray triggered;
        BitArray active;
        void CheckMissionScript()
        {
            for (int i = 0; i < msn.Triggers.Count; i++)
            {
                if (triggered[i]) continue;
                var tr = msn.Triggers[i];
                if (!active[i] && tr.InitState != Data.Missions.TriggerInitState.ACTIVE)
                    continue;
                active[i] = true;
                if (CheckConditions(tr))
                    DoTrigger(i);
            }
        }

        public void EnterLocation(string room, string bse)
        {
            lock (_msnLock)
            {
                locsEntered.Add(new Tuple<string, string>(room, bse));
                CheckMissionScript();
            }
        }

        public void FinishRTC(string rtc)
        {
            lock (_msnLock)
            {
                CheckMissionScript();
            }
        }

        private bool enteredSpace = false;
        public void EnteredSpace()
        {
            enteredSpace = true;
        }
        
        Dictionary<string, MissionTimer> timers = new Dictionary<string, MissionTimer>();
        List<string> finishedLines = new List<string>();
        List<Tuple<string,string>> locsEntered = new List<Tuple<string, string>>();
        bool CheckConditions(MissionTrigger tr)
        {
            bool cndSatisfied = true;
            foreach (var cnd in tr.Conditions)
            {
                if (cnd.Type == TriggerConditions.Cnd_True ||
                    cnd.Type == TriggerConditions.Cnd_BaseEnter ||
                    cnd.Type == TriggerConditions.Cnd_SpaceExit)
                    cndSatisfied = true;
                else if (cnd.Type == TriggerConditions.Cnd_SpaceEnter)
                {
                    if (!enteredSpace)
                    {
                        if(player.World == null)
                            cndSatisfied = false;
                    }
                }
                else if (cnd.Type == TriggerConditions.Cnd_CommComplete)
                {
                    if (finishedLines.Contains(cnd.Entry[0].ToString()))
                        cndSatisfied = true;
                    else
                    {
                        cndSatisfied = false;
                        break;
                    }
                }
                else if (cnd.Type == TriggerConditions.Cnd_RTCDone)
                {
                    
                }
                else if (cnd.Type == TriggerConditions.Cnd_Timer)
                {
                    MissionTimer t;
                    if(!timers.TryGetValue(tr.Nickname, out t))
                    {
                        t = new MissionTimer();
                        timers.Add(tr.Nickname, t);
                    }
                    if (t.T < cnd.Entry[0].ToSingle())
                    {
                        cndSatisfied = false;
                        break;
                    }
                }
                else if (cnd.Type == TriggerConditions.Cnd_LocEnter)
                {
                    var room = cnd.Entry[0].ToString();
                    var bse = cnd.Entry[1].ToString();
                    bool entered = false;
                    foreach (var c in locsEntered)
                    {
                        if (c.Item1.Equals(room, StringComparison.OrdinalIgnoreCase) &&
                            c.Item2.Equals(bse, StringComparison.OrdinalIgnoreCase))
                        {
                            entered = true;
                            break;
                        }
                    }
                    if (!entered)
                    {
                        cndSatisfied = false;
                        break;
                    }
                }
                else
                {
                    cndSatisfied = false;
                    break;
                }
            }
            return cndSatisfied;
        }
        void DoTrigger(int i)
        {
            active[i] = true;
            var tr = msn.Triggers[i];
            if(!CheckConditions(tr)) return; 
            FLLog.Debug("Mission", "Running trigger " + tr.Nickname);
            if (timers.ContainsKey(tr.Nickname)) timers.Remove(tr.Nickname);
            triggered[i] = true;

            foreach (var act in tr.Actions)
            {
                switch(act.Type)
                {
                    case TriggerActions.Act_ActTrig:
                        var trname = act.Entry[0].ToString();
                        for (int j = 0; j < msn.Triggers.Count; j++)
                        {
                            if (trname.Equals(msn.Triggers[j].Nickname, StringComparison.OrdinalIgnoreCase))
                            {
                                DoTrigger(j);
                                break;
                            }
                        }
                        break;
                    case TriggerActions.Act_PlaySoundEffect:
                        player.PlaySound(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_ForceLand:
                        player.ForceLand(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_AdjAcct:
                        //session.Credits += act.Entry[0].ToInt32();
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
                        /*if(!act.Entry[0].ToString().Equals("none", StringComparison.OrdinalIgnoreCase))
                        {
                            var loadout = session.Game.GameData.Ini.Loadouts.FindLoadout(act.Entry[1].ToString());
                            if (loadout != null)
                            {
                                session.PlayerShip = act.Entry[0].ToString();
                                session.Mounts = new List<EquipMount>();
                                foreach(var equip in loadout.Equip)
                                {
                                    if (equip.Value == null) continue;
                                    var hp = equip.Key.StartsWith("__noHardpoint")
                                        ? null
                                        : equip.Key;
                                    session.Mounts.Add(new EquipMount(hp, equip.Value));
                                }
                            }
                           
                        }*/
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
                        lock (finishedLines) {
                            finishedLines.Add(waitingLines[i].Line);
                        }
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
