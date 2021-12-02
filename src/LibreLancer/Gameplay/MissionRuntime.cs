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

        void DeactivateTrigger(string trigger)
        {
            var x = activeTriggers.FirstOrDefault(x => x.Trigger.Nickname.Equals(trigger, StringComparison.OrdinalIgnoreCase));
            if (x != null)
                activeTriggers.Remove(x);
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
        
        static Func<MissionCondition, bool> TruePredicate = (c) => true;
      
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
                    case TriggerActions.Act_DeactTrig:
                        DeactivateTrigger(act.Entry[0].ToString());
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
                    case TriggerActions.Act_SpawnFormation:
                        SpawnFormation(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_MovePlayer:
                        //unknown 4th param
                        var _mpos = new Vector3(act.Entry[0].ToSingle(), act.Entry[1].ToSingle(),
                            act.Entry[2].ToSingle());
                        player.RemoteClient.ForceMove(_mpos);
                        break;
                    case TriggerActions.Act_RelocateShip:
                        var npos = new Vector3(act.Entry[1].ToSingle(), act.Entry[2].ToSingle(),
                            act.Entry[3].ToSingle());
                        RelocateShip(act.Entry[0].ToString(), npos);
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
                    case TriggerActions.Act_Destroy:
                        DestroySpawned(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_SpawnShip:
                        SpawnShip(act);
                        break;
                    case TriggerActions.Act_SendComm:
                        SendDialog(act.Entry[0].ToString(), act.Entry[1].ToString(), act.Entry[2].ToString());
                        break;
                    case TriggerActions.Act_EtherComm:
                        SendEtherDialog(act);
                        break;
                    case TriggerActions.Act_GiveObjList:
                        GiveObjList(act);
                        break;
                    case TriggerActions.Act_PlayMusic:
                        player.PlayMusic(act.Entry[3].ToString());
                        break;
                    case TriggerActions.Act_CallThorn:
                        CallThorn(act);
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

        void RelocateShip(string ship, Vector3 pos)
        {
            if (msn.Ships.Any(x => x.Nickname.Equals(ship, StringComparison.OrdinalIgnoreCase)))
            {
                player.WorldAction(() =>
                {
                    var obj = player.World.GameWorld.GetObject(ship);
                    obj.SetLocalTransform(Matrix4x4.CreateTranslation(pos));
                });
            }
        }
        void DestroySpawned(string ship)
        {
            if (msn.Ships.Any(x => x.Nickname.Equals(ship, StringComparison.OrdinalIgnoreCase)))
            {
                player.WorldAction(() => { player.World.NPCs.Despawn(player.World.GameWorld.GetObject(ship)); });
            }
        }
        void GiveObjList(MissionAction act)
        {
            var tgt = act.Entry[0].ToString();
            var lst = msn.ObjLists.First(x =>
                x.Nickname.Equals(act.Entry[1].ToString(), StringComparison.OrdinalIgnoreCase));
            var formation = msn.Formations.FirstOrDefault(x => x.Nickname.Equals(tgt, StringComparison.OrdinalIgnoreCase));
            var objlist = ConvertObjList(lst);
            if (objlist == null) return;
            if (formation != null)
            {
                foreach (var s in formation.Ships)
                {
                    player.World.NPCs.NpcDoAction(s,
                        (npc) => { npc.GetComponent<SNPCComponent>().SetState(objlist); });
                }
            }
            else
            {
                player.World.NPCs.NpcDoAction(tgt,
                    (npc) => { npc.GetComponent<SNPCComponent>().SetState(objlist); });
            }
        }
        
        void CallThorn(MissionAction act)
        {
            player.WorldAction(() =>
            {
                int mainObject = 0;
                if (act.Entry.Count > 1)
                {
                    var gameObj = player.World.GameWorld.GetObject(act.Entry[1].ToString());
                    mainObject = gameObj?.NetID ?? 0;
                }
                FLLog.Info("Server", $"Calling Thorn {act.Entry[0].ToString()} with mainObject `{mainObject}`");
                player.CallThorn(act.Entry[0].ToString(), mainObject);
            });
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

        void SendEtherDialog(MissionAction act)
        {
            var voice = act.Entry[0].ToString();
            var line = act.Entry[3].ToString();
            var hash = FLHash.CreateID(line);
            lock (waitingLines)
            {
                waitingLines.Add(new PendingLine() { Hash = hash, Line = line });
            }
            player.PlayDialog(new NetDlgLine[] { new NetDlgLine()
            {
                Voice = voice,
                Hash = hash
            }});
        }
        void SendDialog(string source, string dest, string line)
        {
            var netdlg = new NetDlgLine[1];
            var src = msn.Ships.First((x) => x.Nickname.Equals(source, StringComparison.OrdinalIgnoreCase));
            var npc = msn.NPCs.First((x) => x.Nickname.Equals(src.NPC, StringComparison.OrdinalIgnoreCase));
            var voice = npc.Voice;
            var hash = FLHash.CreateID(line);
            lock (waitingLines)
            {
                waitingLines.Add(new PendingLine() { Hash = hash, Line = line});
            }
            netdlg[0] = new NetDlgLine()
            {
                Voice = voice,
                Hash = hash
            };
            player.PlayDialog(netdlg);
        }
        void RunDialog(MissionDialog dlg)
        {
            var netdlg = new NetDlgLine[dlg.Lines.Count];
            for (int i = 0; i < dlg.Lines.Count; i++)
            {
                var d = dlg.Lines[i];
                string voice = "trent_voice";
                if (!d.Source.Equals("Player", StringComparison.OrdinalIgnoreCase))
                {
                    var src = msn.Ships.First((x) => x.Nickname.Equals(d.Source, StringComparison.OrdinalIgnoreCase));
                    var npc = msn.NPCs.First((x) => x.Nickname.Equals(src.NPC, StringComparison.OrdinalIgnoreCase));
                    voice = npc.Voice;
                }

                var hash = FLHash.CreateID(d.Line);
                lock (waitingLines)
                {
                    waitingLines.Add(new PendingLine() { Hash = hash, Line = d.Line});
                }
                netdlg[i] = new NetDlgLine() {
                    Voice = voice,
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

        //TODO: implement formations
        private static Vector3[] formationOffsets = new Vector3[]
        {
            new Vector3(-60, 0, 0),
            new Vector3(60, 0, 0),
            new Vector3(0, -60, 0),
            new Vector3(0, 60, 0)
        };
        void SpawnFormation(string form)
        {
            var formation = msn.Formations.First(x => x.Nickname.Equals(form, StringComparison.OrdinalIgnoreCase));
            SpawnShip(formation.Ships[0], formation.Position, formation.Orientation, null);
            var mat = Matrix4x4.CreateFromQuaternion(formation.Orientation) *
                      Matrix4x4.CreateTranslation(formation.Position);
            int j = 0;
            for (int i = 1; i < formation.Ships.Count; i++)
            {
                var pos = Vector3.Transform(formationOffsets[j++], mat);
                SpawnShip(formation.Ships[i], pos, formation.Orientation, null);
            }
        }

        void SpawnShip(string msnShip, Vector3? spawnpos, Quaternion? spawnorient, string objList)
        {
            var ship = msn.Ships.First(x =>
                x.Nickname.Equals(msnShip, StringComparison.OrdinalIgnoreCase));
            var npcDef = msn.NPCs.First(x => x.Nickname.Equals(ship.NPC, StringComparison.OrdinalIgnoreCase));
            var shipArch = msn.ShipIni.ShipArches.FirstOrDefault(x =>
                x.Nickname.Equals(npcDef.NpcShipArch, StringComparison.OrdinalIgnoreCase));
            if (shipArch == null)
            {
                shipArch = player.Game.GameData.Ini.NPCShips.ShipArches.First(x =>
                    x.Nickname.Equals(npcDef.NpcShipArch, StringComparison.OrdinalIgnoreCase));
            }

            var pos = spawnpos ?? ship.Position;
            var orient = spawnorient ?? ship.Orientation;
            AiState state = null;
            if (!string.IsNullOrEmpty(objList))
            {
                var lst = msn.ObjLists.First(x =>
                    x.Nickname.Equals(objList, StringComparison.OrdinalIgnoreCase));
                state = ConvertObjList(lst);
            }
            
            player.WorldAction(() =>
            {
                player.World.Server.GameData.TryGetLoadout(shipArch.Loadout, out var ld);
                var obj = player.World.NPCs.DoSpawn(ship.Nickname, ld, pos, orient);
                obj.GetComponent<SNPCComponent>().SetState(state);
            });
        }
        void SpawnShip(MissionAction act)
        {
            var ship = act.Entry[0].ToString();
            string objList = null;
            Vector3? pos = null;
            Quaternion? orient = null;
            if (act.Entry.Count > 1)
            {
                objList = act.Entry[1].ToString();
            }
            if (act.Entry.Count > 2)
            {
                pos = new Vector3(act.Entry[2].ToSingle(), act.Entry[3].ToSingle(), act.Entry[4].ToSingle());
            }
            if (act.Entry.Count > 5)
            {
                orient = new Quaternion(act.Entry[6].ToSingle(), act.Entry[7].ToSingle(), act.Entry[8].ToSingle(),
                    act.Entry[5].ToSingle());
            }
            SpawnShip(ship, pos, orient, objList);
        }
        
        AiObjListState ConvertObjList(ObjList list)
        {
            AiObjListState last = null;
            foreach (var l in list.Commands)
            {
                switch (l.Command)
                {
                    case ObjListCommands.GotoVec:
                        var pos = new Vector3(l.Entry[1].ToSingle(), l.Entry[2].ToSingle(), l.Entry[3].ToSingle());
                        var cruise = !l.Entry[0].ToString().Equals("goto_no_cruise", StringComparison.OrdinalIgnoreCase);
                        var cur = new AiGotoVecState(pos, cruise);
                        if (last != null) last.Next = cur;
                        last = cur;
                        break;
                }
            }
            return last;
        }
    }
}
