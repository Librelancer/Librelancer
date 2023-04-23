// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
using LibreLancer.Data.Missions;
using LibreLancer.Server;
using LibreLancer.Server.Components;

namespace LibreLancer.Missions
{
    public abstract class ScriptedAction
    {
        public string Text { get; private set; }
        protected ScriptedAction(MissionAction a)
        {
            Text = a.Entry.ToString();
        }
        public abstract void Invoke(MissionRuntime runtime, MissionScript script);

        public static IEnumerable<ScriptedAction> Convert(IEnumerable<MissionAction> actions)
        {
            foreach (var a in actions)
            {
                switch (a.Type)
                {
                    case TriggerActions.Act_ActTrig:
                        yield return new Act_ActTrig(a);
                        break;
                    case TriggerActions.Act_DeactTrig:
                        yield return new Act_DeactTrig(a);
                        break;
                    case TriggerActions.Act_PlaySoundEffect:
                        yield return new Act_PlaySoundEffect(a);
                        break;
                    case TriggerActions.Act_ForceLand:
                        yield return new Act_ForceLand(a);
                        break;
                    case TriggerActions.Act_AdjAcct:
                        yield return new Act_AdjAcct(a);
                        break;
                    case TriggerActions.Act_SpawnSolar:
                        yield return new Act_SpawnSolar(a);
                        break;
                    case TriggerActions.Act_StartDialog:
                        yield return new Act_StartDialog(a);
                        break;
                    case TriggerActions.Act_SpawnFormation:
                        yield return new Act_SpawnFormation(a);
                        break;
                    case TriggerActions.Act_Destroy:
                        yield return new Act_Destroy(a);
                        break;
                    case TriggerActions.Act_MovePlayer:
                        yield return new Act_MovePlayer(a);
                        break;
                    case TriggerActions.Act_RelocateShip:
                        yield return new Act_RelocateShip(a);
                        break;
                    case TriggerActions.Act_LightFuse:
                        yield return new Act_LightFuse(a);
                        break;
                    case TriggerActions.Act_PopUpDialog:
                        yield return new Act_PopupDialog(a);
                        break;
                    case TriggerActions.Act_SpawnShip:
                        yield return new Act_SpawnShip(a);
                        break;
                    case TriggerActions.Act_SendComm:
                        yield return new Act_SendComm(a);
                        break;
                    case TriggerActions.Act_EtherComm:
                        yield return new Act_EtherComm(a);
                        break;
                    case TriggerActions.Act_GiveObjList:
                        yield return new Act_GiveObjList(a);
                        break;
                    case TriggerActions.Act_PlayMusic:
                        yield return new Act_PlayMusic(a);
                        break;
                    case TriggerActions.Act_CallThorn:
                        yield return new Act_CallThorn(a);
                        break;
                    case TriggerActions.Act_RevertCam:
                        yield return new Act_RevertCam(a);
                        break;
                    case TriggerActions.Act_AddRTC:
                        yield return new Act_AddRTC(a);
                        break;
                    case TriggerActions.Act_SetShipAndLoadout:
                        yield return new Act_SetShipAndLoadout(a);
                        break;
                    case TriggerActions.Act_SetVibeLblToShip:
                        yield return new Act_SetVibeLblToShip(a);
                        break;
                    case TriggerActions.Act_SetVibe:
                        yield return new Act_SetVibe(a);
                        break;
                    case TriggerActions.Act_SetVibeLbl:
                        yield return new Act_SetVibeLbl(a);
                        break;
                    case TriggerActions.Act_SetVibeShipToLbl:
                        yield return new Act_SetVibeShipToLbl(a);
                        break;
                    case TriggerActions.Act_Invulnerable:
                        yield return new Act_Invulnerable(a);
                        break;
                    case TriggerActions.Act_SetNNObj:
                        yield return new Act_SetNNObj(a);
                        break;
                    case TriggerActions.Act_MarkObj:
                        yield return new Act_MarkObj(a);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public class Act_SetNNObj : ScriptedAction
    {
        public string Objective;

        public Act_SetNNObj(MissionAction act) : base(act)
        {
            Objective = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (script.Objectives.TryGetValue(Objective, out var v)) {
                if (v.Type[0] == "ids")
                {
                    runtime.Player.ObjectiveIds = int.Parse(v.Type[1]);
                    FLLog.Info("Server", $"Set player objective to {runtime.Player.ObjectiveIds}");
                    runtime.Player.RemoteClient.ObjectiveUpdate(runtime.Player.ObjectiveIds);
                }
            }
        }
    }

    public class Act_ActTrig : ScriptedAction
    {
        public string Trigger;

        public Act_ActTrig(MissionAction act) : base(act)
        {
            Trigger = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.ActivateTrigger(Trigger);
        }
    }

    public class Act_DeactTrig : ScriptedAction
    {
        public string Trigger;

        public Act_DeactTrig(MissionAction act) : base(act)
        {
            Trigger = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.DeactivateTrigger(Trigger);
        }
    }

    public class Act_AddRTC : ScriptedAction
    {
        public string RTC;
        public Act_AddRTC(MissionAction act) : base(act)
        {
            RTC = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.AddRTC(RTC);
        }
    }

    public class Act_Invulnerable : ScriptedAction
    {
        public string Object;
        public bool Invulnerable;
        public Act_Invulnerable(MissionAction act) : base(act)
        {
            Object = act.Entry[0].ToString();
            Invulnerable = act.Entry[1].ToBoolean();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.WorldAction(() =>
            {
                var tgt = runtime.Player.World.GameWorld.GetObject(Object);
                if (tgt != null && tgt.TryGetComponent<SHealthComponent>(out var health))
                {
                    health.Invulnerable = Invulnerable;
                }
            });
        }
    }

    public class Act_SetShipAndLoadout : ScriptedAction
    {
        public string Ship;
        public string Loadout;

        public Act_SetShipAndLoadout(MissionAction act) : base(act)
        {
            Ship = act.Entry[0].ToString();
            Loadout = act.Entry[1].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (Ship.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                var p = runtime.Player;
                p.Character.Ship = null;
                p.Character.Items = new List<NetCargo>();
                runtime.Player.UpdateCurrentInventory();
            }
            else
            {
                var p = runtime.Player;
                if (p.Game.GameData.TryGetLoadout(Loadout, out var loadout))
                {
                    p.Character.Ship = p.Game.GameData.Ships.Get(Ship);
                    p.Character.Items = new List<NetCargo>();
                    foreach (var equip in loadout.Items)
                    {
                        p.Character.Items.Add(new NetCargo()
                        {
                            Equipment = equip.Equipment,
                            Hardpoint = string.IsNullOrEmpty(equip.Hardpoint) ? "internal" : equip.Hardpoint,
                            Health = 1f,
                            Count = 1
                        });
                    }
                }
                runtime.Player.UpdateCurrentInventory();
            }
        }
    }
    public class Act_PlaySoundEffect : ScriptedAction
    {
        public string Effect;
        
        public Act_PlaySoundEffect(MissionAction act) : base(act)
        {
            Effect = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.PlaySound(Effect);
        }
    }

    //Sometimes 4 parameters, with the music track being the 4th
    //Sometimes no_params (= stop music? not sure)
    public class Act_PlayMusic : ScriptedAction
    {
        public string Music;
        public float Fade;

        public Act_PlayMusic(MissionAction act) : base(act)
        {
            if(act.Entry.Count > 3) //4th entry seems to = specific music. First 3 maybe change ambient?
                Music = act.Entry[3].ToString();
            if (act.Entry.Count > 4)
                Fade = act.Entry[4].ToSingle();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if(Music != null)
                runtime.Player.PlayMusic(Music, Fade);
        }
    }

    public class Act_ForceLand : ScriptedAction
    {
        public string Base;

        public Act_ForceLand(MissionAction act) : base(act)
        {
            Base = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.ForceLand(Base);
        }
    }

    public class Act_AdjAcct : ScriptedAction
    {
        public int Amount;

        public Act_AdjAcct(MissionAction act) : base(act)
        {
            Amount = act.Entry[0].ToInt32();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.Character.UpdateCredits(runtime.Player.Character.Credits + Amount);
        }
    }
    
    public class Act_LightFuse : ScriptedAction
    {
        public string Target;
        public string Fuse;

        public Act_LightFuse(MissionAction act) : base(act)
        {
            Target = act.Entry[0].ToString();
            Fuse = act.Entry[1].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.WorldAction(() =>
            {
                var fuse = runtime.Player.World.Server.GameData.GetFuse(Fuse);
                var gameObj = runtime.Player.World.GameWorld.GetObject(Target);
                if (!gameObj.TryGetComponent<SFuseRunnerComponent>(out var fr))
                {
                    fr = new SFuseRunnerComponent(gameObj);
                    gameObj.Components.Add(fr);
                }
                fr.Run(fuse);
            });
        }
    }

    public class Act_PopupDialog : ScriptedAction
    {
        public int Title;
        public int Contents;
        public string ID;

        public Act_PopupDialog(MissionAction act) : base(act)
        {
            Title = act.Entry[0].ToInt32();
            Contents = act.Entry[1].ToInt32();
            ID = act.Entry[2].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.RemoteClient.PopupOpen(Title, Contents, ID);
        }
    }

    public class Act_GiveObjList : ScriptedAction
    {
        public string Target;
        public string List;

        public Act_GiveObjList(MissionAction act) : base(act)
        {
            Target = act.Entry[0].ToString();
            List = act.Entry[1].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            var ol = script.ObjLists[List].AiState;
            if (script.Ships.ContainsKey(Target)) {
                runtime.Player.World.NPCs.NpcDoAction(Target,
                    (npc) => { npc.GetComponent<SNPCComponent>().SetState(ol); });
            } else if (script.Formations.TryGetValue(Target, out var formation))
            {
                foreach (var s in formation.Ships)
                {
                    runtime.Player.World.NPCs.NpcDoAction(s,
                        (npc) => { npc.GetComponent<SNPCComponent>().SetState(ol); });
                }
            }
        }
    }

    public class Act_RevertCam : ScriptedAction
    {
        public Act_RevertCam(MissionAction act) : base(act) { }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.CallThorn(null, 0);
        }
    }

    public class Act_CallThorn : ScriptedAction
    {
        public string Thorn;
        public string MainObject;

        public Act_CallThorn(MissionAction act) : base(act)
        {
            Thorn = act.Entry[0].ToString();
            if (act.Entry.Count > 1)
                MainObject = act.Entry[1].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            FLLog.Info("Act_CallThorn", Thorn);
            runtime.Player.WorldAction(() =>
            {
                int mainObject = 0;
                if (MainObject != null)
                {
                    var gameObj = runtime.Player.World.GameWorld.GetObject(MainObject);
                    mainObject = gameObj?.NetID ?? 0;
                }
                FLLog.Info("Server", $"Calling Thorn {Thorn} with mainObject `{mainObject}`");
                runtime.Player.CallThorn(Thorn, mainObject);
            });
        }
    }
    
    
}