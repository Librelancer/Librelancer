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

        GameSession session;
        MissionIni msn;

        public MissionRuntime(MissionIni msn, GameSession session)
        {
            this.msn = msn;
            this.session = session;
            triggered = new BitArray(msn.Triggers.Count);
            active = new BitArray(msn.Triggers.Count);
        }

        public void Update(SpaceGameplay gameplay, TimeSpan elapsed)
        {
            foreach (var t in timers)
            {
                t.Value.T += elapsed.TotalSeconds;
            }
            CheckMissionScript(gameplay);
        }

        //Implement just enough of the mission script to get the player to a base from
        //FP7_system in vanilla, and set the loadout on disco
        BitArray triggered;
        BitArray active;
        void CheckMissionScript(SpaceGameplay gameplay)
        {
            for (int i = 0; i < msn.Triggers.Count; i++)
            {
                if (triggered[i]) continue;
                var tr = msn.Triggers[i];
                if (!active[i] && tr.InitState != Data.Missions.TriggerInitState.ACTIVE)
                    continue;
                active[i] = true;
                if (CheckConditions(tr))
                    DoTrigger(i, gameplay);
            }
        }
        Dictionary<string, MissionTimer> timers = new Dictionary<string, MissionTimer>();
        List<string> finishedLines = new List<string>();
        bool CheckConditions(MissionTrigger tr)
        {
            bool cndSatisfied = true;
            foreach (var cnd in tr.Conditions)
            {
                if (cnd.Type == TriggerConditions.Cnd_True ||
                    cnd.Type == TriggerConditions.Cnd_SpaceEnter ||
                    cnd.Type == TriggerConditions.Cnd_BaseEnter ||
                    cnd.Type == TriggerConditions.Cnd_SpaceExit)
                    cndSatisfied = true;
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
                else
                {
                    cndSatisfied = false;
                    break;
                }
            }
            return cndSatisfied;
        }
        Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();
        void DoTrigger(int i, SpaceGameplay gameplay)
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
                                DoTrigger(j, gameplay);
                                break;
                            }
                        }
                        break;
                    case TriggerActions.Act_PlaySoundEffect:
                        session.Game.Sound.PlaySound(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_ForceLand:
                        session.ForceLand(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_AdjAcct:
                        session.Credits += act.Entry[0].ToInt32();
                        break;
                    case TriggerActions.Act_SpawnSolar:
                        SpawnSolar(act.Entry[0].ToString(), gameplay.world);
                        break;
                    case TriggerActions.Act_StartDialog:
                        RunDialog(msn.Dialogs.First((x) => x.Nickname.Equals(act.Entry[0].ToString(), StringComparison.OrdinalIgnoreCase)));
                        break;
                    case TriggerActions.Act_MovePlayer:
                        gameplay.player.Transform = Matrix4.CreateTranslation(act.Entry[0].ToSingle(), act.Entry[1].ToSingle(), act.Entry[2].ToSingle());
                        //last param seems to always be one?
                        break;
                    case TriggerActions.Act_PlayMusic:
                        session.Game.Sound.PlayMusic(act.Entry[3].ToString());
                        break;
                    case TriggerActions.Act_CallThorn:
                        var thn = new ThnScript(gameplay.FlGame.GameData.ResolveDataPath(act.Entry[0].ToString()));
                        gameplay.Thn = new Cutscene(new ThnScript[] { thn }, gameplay);
                        break;
                    case TriggerActions.Act_SetShipAndLoadout:
                        if(!act.Entry[0].ToString().Equals("none", StringComparison.OrdinalIgnoreCase))
                        {
                            var loadout = session.Game.GameData.Ini.Loadouts.FindLoadout(act.Entry[1].ToString());
                            if (loadout != null)
                            {
                                session.PlayerShip = act.Entry[0].ToString();
                                session.MountedEquipment = new Dictionary<string, string>();
                                foreach(var equip in loadout.Equip)
                                {
                                    if (equip.Key.StartsWith("__noHardpoint")) continue;
                                    if (equip.Value == null) continue;
                                    session.MountedEquipment.Add(equip.Key, equip.Value.Nickname);
                                }
                            }
                           
                        }
                        break;
                }
            }
        }
        void RunDialog(MissionDialog dlg, int index = 0)
        {
            if (index >= dlg.Lines.Count) return;
            var d = dlg.Lines[index];
            var src = msn.Ships.First((x) => x.Nickname.Equals(d.Source, StringComparison.OrdinalIgnoreCase));
            var npc = msn.NPCs.First((x) => x.Nickname.Equals(src.NPC, StringComparison.OrdinalIgnoreCase));
            var hash = FLHash.CreateID(d.Line);
            session.Game.Sound.PlayVoiceLine(npc.Voice, hash, () =>
             {
                 finishedLines.Add(d.Line);
                 RunDialog(dlg, index + 1);
             });
        }
        void SpawnSolar(string solarname, GameWorld world)
        {
            var sol = msn.Solars.First(x => x.Nickname.Equals(solarname, StringComparison.OrdinalIgnoreCase));
            var arch = session.Game.GameData.GetSolarArchetype(sol.Archetype);

            var gameobj = new GameObject(arch, session.Game.ResourceManager, true);
            gameobj.StaticPosition = sol.Position;
            gameobj.Transform = Matrix4.CreateFromQuaternion(sol.Orientation) * Matrix4.CreateTranslation(sol.Position);
            gameobj.Nickname = sol.Nickname;
            gameobj.World = world;
            gameobj.Register(world.Physics);
            world.Objects.Add(gameobj);
            spawned.Add(solarname, gameobj);
        }
    }
}
