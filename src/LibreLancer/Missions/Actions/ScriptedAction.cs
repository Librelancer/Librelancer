// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net;
using LibreLancer.Server;
using LibreLancer.Server.Components;
using LibreLancer.World;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LibreLancer.Missions.Actions
{
    public abstract class ScriptedAction : TriggerEntry
    {
        public string Text { get; private set; }

        protected ScriptedAction()
        {
            Text = "";
        }

        protected ScriptedAction(MissionAction a)
        {
            Text = a.Entry.ToString();
        }


        public virtual void Invoke(MissionRuntime runtime, MissionScript script)
        {
            FLLog.Warning("Missions", $"{GetType().Name}.Invoke() is not implemented!");
        }

        public virtual void Write(IniBuilder.IniSectionBuilder section)
        {
            FLLog.Warning("Missions", $"{GetType().Name}.Write() is not implemented!");
        }


        public static readonly TriggerActions[] Unsupported =
        [
            TriggerActions.Act_SetFlee,
            TriggerActions.Act_SpawnShipRel,
            TriggerActions.Act_RepChangeRequest,
            TriggerActions.Act_RelocateForm,
            TriggerActions.Act_PilotParams,
            TriggerActions.Act_PlayNN,
            TriggerActions.Act_PlayerForm,
            TriggerActions.Act_GiveMB
        ];


        public static IEnumerable<ScriptedAction> Convert(IEnumerable<MissionAction> actions)
        {
            foreach (var a in actions)
            {
                yield return a.Type switch
                {
                    TriggerActions.Act_StaticCam => new Act_StaticCam(a),
                    TriggerActions.Act_StartDialog => new Act_StartDialog(a),
                    TriggerActions.Act_SpawnSolar => new Act_SpawnSolar(a),
                    //TriggerActions.Act_SpawnShipRel => new Act_SpawnShipRel(a),
                    TriggerActions.Act_SpawnShip => new Act_SpawnShip(a),
                    TriggerActions.Act_SpawnLoot => new Act_SpawnLoot(a),
                    TriggerActions.Act_SpawnFormation => new Act_SpawnFormation(a),
                    TriggerActions.Act_SetVibeOfferBaseHack => new Act_SetVibeOfferBaseHack(a),
                    TriggerActions.Act_SetVibeShipToLbl => new Act_SetVibeShipToLbl(a),
                    TriggerActions.Act_SetVibeLblToShip => new Act_SetVibeLblToShip(a),
                    TriggerActions.Act_SetVibeLbl => new Act_SetVibeLbl(a),
                    TriggerActions.Act_SetVibe => new Act_SetVibe(a),
                    TriggerActions.Act_SetTitle => new Act_SetTitle(a),
                    TriggerActions.Act_SetShipAndLoadout => new Act_SetShipAndLoadout(a),
                    TriggerActions.Act_SetRep => new Act_SetRep(a),
                    TriggerActions.Act_SetOrient => new Act_SetOrient(a),
                    TriggerActions.Act_SetOffer => new Act_SetOffer(a),
                    TriggerActions.Act_SetNNState => new Act_SetNNState(a),
                    TriggerActions.Act_SetNNObj => new Act_SetNNObj(a),
                    TriggerActions.Act_SetNNHidden => new Act_SetNNHidden(a),
                    TriggerActions.Act_SetLifeTime => new Act_SetLifetime(a),
                    TriggerActions.Act_SetInitialPlayerPos => new Act_SetInitialPlayerPos(a),
                    //TriggerActions.Act_SetFlee => new Act_SetFlee(a),
                    TriggerActions.Act_SendComm => new Act_SendComm(a),
                    TriggerActions.Act_Save => new Act_Save(a),
                    TriggerActions.Act_RpopTLAttacksEnabled => new Act_RpopTLAttacksEnabled(a),
                    TriggerActions.Act_RpopAttClamp => new Act_RpopAttClamp(a),
                    TriggerActions.Act_RevertCam => new Act_RevertCam(a),
                    //TriggerActions.Act_RepChangeRequest => new Act_RepChangeRequest(a),
                    TriggerActions.Act_RemoveRTC => new Act_RemoveRTC(a),
                    TriggerActions.Act_RemoveCargo => new Act_RemoveCargo(a),
                    TriggerActions.Act_RemoveAmbient => new Act_RemoveAmbient(a),
                    TriggerActions.Act_RelocateShip => new Act_RelocateShip(a),
                    //TriggerActions.Act_RelocateForm => new Act_RelocateForm(a),
                    TriggerActions.Act_RandomPopSphere => new Act_RandomPopSphere(a),
                    TriggerActions.Act_RandomPop => new Act_RandomPop(a),
                    TriggerActions.Act_SetPriority => new Act_SetPriority(a),
                    TriggerActions.Act_PopUpDialog => new Act_PopupDialog(a),
                    TriggerActions.Act_PobjIdle => new Act_PobjIdle(a),
                    //TriggerActions.Act_PilotParams => new Act_PilotParams(a),
                    TriggerActions.Act_PlaySoundEffect => new Act_PlaySoundEffect(a),
                    //TriggerActions.Act_PlayNN => new Act_PlayNN(a),
                    TriggerActions.Act_PlayMusic => new Act_PlayMusic(a),
                    //TriggerActions.Act_PlayerForm => new Act_PlayerForm(a),
                    TriggerActions.Act_PlayerEnemyClamp => new Act_PlayerEnemyClamp(a),
                    TriggerActions.Act_PlayerCanTradelane => new Act_PlayerCanTradelane(a),
                    TriggerActions.Act_PlayerCanDock => new Act_PlayerCanDock(a),
                    TriggerActions.Act_NNIds => new Act_NNIds(a),
                    TriggerActions.Act_NNPath => new Act_NNPath(a),
                    TriggerActions.Act_NagOff => new Act_NagOff(a),
                    TriggerActions.Act_NagGreet => new Act_NagGreet(a),
                    TriggerActions.Act_NagDistTowards => new Act_NagDistTowards(a),
                    TriggerActions.Act_NagDistLeaving => new Act_NagDistLeaving(a),
                    TriggerActions.Act_NagClamp => new Act_NagClamp(a),
                    TriggerActions.Act_MovePlayer => new Act_MovePlayer(a),
                    TriggerActions.Act_MarkObj => new Act_MarkObj(a),
                    TriggerActions.Act_LockManeuvers => new Act_LockManeuvers(a),
                    TriggerActions.Act_LockDock => new Act_LockDock(a),
                    TriggerActions.Act_LightFuse => new Act_LightFuse(a),
                    TriggerActions.Act_Jumper => new Act_Jumper(a),
                    TriggerActions.Act_Invulnerable => new Act_Invulnerable(a),
                    TriggerActions.Act_HostileClamp => new Act_HostileClamp(a),
                    TriggerActions.Act_GiveObjList => new Act_GiveObjList(a),
                    TriggerActions.Act_GiveNNObjs => new Act_GiveNNObjs(a),
                    // TriggerActions.Act_GiveMB => new Act_GiveMB(a),
                    TriggerActions.Act_GCSClamp => new Act_GcsClamp(a),
                    TriggerActions.Act_ForceLand => new Act_ForceLand(a),
                    TriggerActions.Act_EtherComm => new Act_EtherComm(a),
                    TriggerActions.Act_EnableManeuver => new Act_EnableManeuver(a),
                    TriggerActions.Act_EnableEnc => new Act_EnableEnc(a),
                    TriggerActions.Act_DockRequest => new Act_DockRequest(a),
                    TriggerActions.Act_DisableTradelane => new Act_DisableTradelane(a),
                    TriggerActions.Act_DisableFriendlyFire => new Act_DisableFriendlyFire(a),
                    TriggerActions.Act_DisableEnc => new Act_DisableEnc(a),
                    TriggerActions.Act_Destroy => new Act_Destroy(a),
                    TriggerActions.Act_DebugMsg => new Act_DebugMsg(a),
                    TriggerActions.Act_DeactTrig => new Act_DeactTrig(a),
                    TriggerActions.Act_Cloak => new Act_Cloak(a),
                    TriggerActions.Act_ChangeState => new Act_ChangeState(a),
                    TriggerActions.Act_CallThorn => new Act_CallThorn(a),
                    TriggerActions.Act_AdjHealth => new Act_AdjHealth(a),
                    TriggerActions.Act_AdjAcct => new Act_AdjAcct(a),
                    TriggerActions.Act_AddRTC => new Act_AddRTC(a),
                    TriggerActions.Act_AddAmbient => new Act_AddAmbient(a),
                    TriggerActions.Act_ActTrig => new Act_ActTrig(a),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
    }

    public class Act_SetNNObj : ScriptedAction
    {
        public string Objective = string.Empty;
        public bool History = false;

        public Act_SetNNObj()
        {
        }

        public Act_SetNNObj(MissionAction act) : base(act)
        {
            Objective = act.Entry[0].ToString();
            if (act.Entry.Count > 1 &&
                act.Entry[1].ToString()!.Equals("OBJECTIVE_HISTORY", StringComparison.OrdinalIgnoreCase))
            {
                History = true;
            }
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            if (History)
            {
                section.Entry("Act_SetNNObj", Objective, "OBJECTIVE_HISTORY");
            }
            else
            {
                section.Entry("Act_SetNNObj", Objective);
            }
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (!script.Objectives.TryGetValue(Objective, out var v))
            {
                return;
            }

            switch (v.Type)
            {
                case NNObjectiveType.ids:
                    runtime.Player.SetObjective(new NetObjective(v.NameIds), History);
                    break;
                case NNObjectiveType.navmarker:
                    runtime.Player.SetObjective(
                        new NetObjective(
                            v.NameIds,
                            v.ExplanationIds,
                            v.System,
                            v.Position
                        ), History);
                    break;
                case NNObjectiveType.rep_inst:
                    runtime.Player.SetObjective(
                        new NetObjective(
                            v.NameIds,
                            v.ExplanationIds,
                            v.System,
                            v.SolarNickname
                            ), History);
                    break;
            }

        }
    }

    public class Act_ActTrig : ScriptedAction
    {
        public string Trigger = string.Empty;

        public Act_ActTrig()
        {
        }

        public Act_ActTrig(MissionAction act) : base(act)
        {
            GetString(nameof(Trigger), 0, out Trigger, act.Entry);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.ActivateTrigger(Trigger);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_ActTrig", Trigger);
        }
    }

    public class Act_DeactTrig : ScriptedAction
    {
        public string Trigger = string.Empty;

        public Act_DeactTrig()
        {
        }
        public Act_DeactTrig(MissionAction act) : base(act)
        {
            GetString(nameof(Trigger), 0, out Trigger, act.Entry);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.DeactivateTrigger(Trigger);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_DeactTrig", Trigger);
        }
    }

    public class Act_AddRTC : ScriptedAction
    {
        public string RTC = string.Empty;
        public bool Repeatable = false;

        public Act_AddRTC()
        {
        }

        public Act_AddRTC(MissionAction act) : base(act)
        {
            GetString(nameof(RTC), 0, out RTC, act.Entry);
            if (act.Entry.Count > 1)
                Repeatable = "repeatable".Equals(act.Entry[1].ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.AddRTC(RTC);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            if (Repeatable)
                section.Entry("Act_AddRTC", RTC, "repeatable");
            else
                section.Entry("Act_AddRTC", RTC);
        }
    }

    public class Act_RemoveRTC : ScriptedAction
    {
        public string RTC = string.Empty;

        public Act_RemoveRTC()
        {
        }
        public Act_RemoveRTC(MissionAction act) : base(act)
        {
            GetString(nameof(RTC), 0, out RTC, act.Entry);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.RemoveRTC(RTC);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_RemoveRTC", RTC);
        }
    }

    public class Act_AddAmbient : ScriptedAction
    {
        public string Script = string.Empty;
        public string Room = string.Empty;
        public string Base = string.Empty;

        public Act_AddAmbient()
        {
        }

        public Act_AddAmbient(MissionAction act) : base(act)
        {
            GetString(nameof(Script), 0, out Script, act.Entry);
            GetString(nameof(Room), 1, out Room, act.Entry);
            GetString(nameof(Base), 2, out Base, act.Entry);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.AddAmbient(Script, Room, Base);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_AddAmbient", Script, Room, Base);
        }
    }

    public class Act_RemoveAmbient : ScriptedAction
    {
        public string Script = string.Empty;

        public Act_RemoveAmbient()
        {
        }
        public Act_RemoveAmbient(MissionAction act) : base(act)
        {
            GetString(nameof(Script), 0, out Script, act.Entry);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.RemoveAmbient(Script);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_RemoveAmbient", Script);
        }
    }

    public class Act_Invulnerable : ScriptedAction
    {
        public string Object = string.Empty;
        public bool Invulnerable = false;
        public bool Unknown = false;
        public float Threshold;

        public Act_Invulnerable()
        {
        }

        public Act_Invulnerable(MissionAction act) : base(act)
        {
            GetString(nameof(Object), 0, out Object, act.Entry);
            GetBoolean(nameof(Invulnerable), 1, out Invulnerable, act.Entry);
            Unknown = Invulnerable;
            if(act.Entry.Count > 2)
                Unknown = act.Entry[2].ToBoolean();
            if(act.Entry.Count > 3)
                Threshold = act.Entry[3].ToSingle();
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            if (Threshold > float.Epsilon)
                section.Entry("Act_Invulnerable", Object, Invulnerable, Unknown, Threshold);
            else if (Invulnerable != Unknown)
                section.Entry("Act_Invulnerable", Object, Invulnerable, Unknown);
            else
                section.Entry("Act_Invulnerable", Object, Invulnerable);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.MissionWorldAction(() =>
            {
                var tgt = runtime.Player.Space.World.GameWorld.GetObject(Object);
                if (tgt != null && tgt.TryGetComponent<SHealthComponent>(out var health))
                {
                    health.Invulnerable = Invulnerable;
                }
            });
        }
    }

    public class Act_SetShipAndLoadout : ScriptedAction
    {
        public string Ship = string.Empty;
        public string Loadout = string.Empty;

        public Act_SetShipAndLoadout()
        {
        }

        public Act_SetShipAndLoadout(MissionAction act) : base(act)
        {
            GetString(nameof(Ship), 0, out Ship, act.Entry);
            GetString(nameof(Loadout), 1, out Loadout, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SetShipAndLoadout", Ship, Loadout);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (Ship.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                var p = runtime.Player;
                using (var c = p.Character.BeginTransaction())
                {
                    c.UpdateShip(null);
                    c.ClearAllCargo();
                    p.Character.Items = new List<NetCargo>();
                }
                runtime.Player.UpdateCurrentInventory();
            }
            else
            {
                var p = runtime.Player;
                if (p.Game.GameData.Items.TryGetLoadout(Loadout, out var loadout))
                {
                    using (var c = p.Character.BeginTransaction())
                    {
                        c.UpdateShip(p.Game.GameData.Items.Ships.Get(Ship));
                        p.Character.Items = new List<NetCargo>();
                        c.ClearAllCargo();
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

                }
                runtime.Player.UpdateCurrentInventory();
            }
        }
    }
    public class Act_PlaySoundEffect : ScriptedAction
    {
        public string Effect;

        public Act_PlaySoundEffect()
        {
            Effect = "";
        }

        public Act_PlaySoundEffect(MissionAction act) : base(act)
        {
            GetString("Effect", 0, out Effect, act.Entry);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.RpcClient.PlaySound(Effect);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_PlaySoundEffect", Effect);
        }
    }

    //Sometimes 4 parameters, with the music track being the 4th
    //Sometimes no_params (= stop music? not sure)
    public class Act_PlayMusic : ScriptedAction
    {
        public bool Reset;
        public string Space = string.Empty;
        public string Danger = string.Empty;
        public string Battle = string.Empty;
        public string Motif = string.Empty;
        public float Fade;
        public bool Unknown;

        public Act_PlayMusic()
        {
        }

        public Act_PlayMusic(MissionAction act) : base(act)
        {
            if (act.Entry[0].ToString()!
                .Equals("no_params", StringComparison.OrdinalIgnoreCase)) {
                Reset = true;
                return;
            }
            GetString(nameof(Space), 0, out Space, act.Entry);
            GetString(nameof(Danger), 1, out Danger, act.Entry);
            GetString(nameof(Battle), 2, out Battle, act.Entry);
            if(act.Entry.Count > 3)
                Motif = act.Entry[3].ToString();
            if (act.Entry.Count > 4)
                Fade = act.Entry[4].ToSingle();
            if(act.Entry.Count > 5)
                Unknown = act.Entry[5].ToBoolean();
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            var space = Space == "" ? "none" : Space;
            var danger = Danger == "" ? "none" : Danger;
            var battle = Battle == "" ? "none" : Battle;
            if (Reset)
            {
                section.Entry("Act_PlayMusic", "no_params");
            }
            else
            {
                List<ValueBase> values = [space, danger, battle];
                if (!string.IsNullOrWhiteSpace(Motif))
                {
                    values.Add(Motif);
                    if (Fade != 0)
                    {
                        values.Add(Fade);
                    }
                    if (Unknown)
                        values.Add(true);
                }
                section.Entry("Act_PlayMusic", values.ToArray());
            }
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if(!Reset)
                runtime.Player.RpcClient.PlayMusic(Motif == "" ? Battle : Motif, Fade);
        }
    }

    public class Act_ForceLand : ScriptedAction
    {
        public string Base = string.Empty;

        public Act_ForceLand()
        {
        }

        public Act_ForceLand(MissionAction act) : base(act)
        {
            GetString(nameof(Base), 0, out Base, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_ForceLand", Base);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.ForceLand(Base);
        }
    }

    public class Act_DisableTradelane : ScriptedAction
    {
        public string Tradelane = string.Empty;

        public Act_DisableTradelane()
        {
        }

        public Act_DisableTradelane(MissionAction act) : base(act)
        {
            GetString(nameof(Tradelane), 0, out Tradelane, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_DisableTradelane", Tradelane);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.MissionWorldAction(() =>
            {
                var gameObj = runtime.Player.Space.World.GameWorld.GetObject(Tradelane);
                var firstChild = gameObj.GetFirstChildComponent<SShieldComponent>();
                if (firstChild != null)
                {
                    firstChild.Damage(float.MaxValue);
                }
            });
        }
    }

    public class Act_AdjAcct : ScriptedAction
    {
        public int Amount;

        public Act_AdjAcct()
        {
        }

        public Act_AdjAcct(MissionAction act) : base(act)
        {
            GetInt(nameof(Amount), 0, out Amount, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_AdjAcct", Amount);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.AddCash(Amount);
        }
    }

    public class Act_LightFuse : ScriptedAction
    {
        public string Target = string.Empty;
        public string Fuse = string.Empty;

        public Act_LightFuse()
        {
        }

        public Act_LightFuse(MissionAction act) : base(act)
        {
            GetString(nameof(Target), 0, out Target, act.Entry);
            GetString(nameof(Fuse), 1, out Fuse, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_LightFuse", Target, Fuse);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.MissionWorldAction(() =>
            {
                var fuse = runtime.Player.Space.World.Server.GameData.Items.Fuses.Get(Fuse);
                var gameObj = runtime.Player.Space.World.GameWorld.GetObject(Target);
                if (gameObj == null)
                {
                    FLLog.Error("Mission", $"Act_LightFuse can't find target {Target}");
                    return;
                }
                if (!gameObj.TryGetComponent<SFuseRunnerComponent>(out var fr))
                {
                    fr = new SFuseRunnerComponent(gameObj);
                    gameObj.AddComponent(fr);
                }
                fr.Run(fuse);
            });
        }
    }

    public class Act_PopupDialog : ScriptedAction
    {
        public int Title;
        public int Contents;
        public string ID = string.Empty;

        public Act_PopupDialog()
        {
        }
        public Act_PopupDialog(MissionAction act) : base(act)
        {
            GetInt(nameof(Title), 0, out Title, act.Entry);
            GetInt(nameof(Contents), 1, out Contents, act.Entry);
            GetString(nameof(ID), 2, out ID, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_PopUpDialog", Title, Contents, ID);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.RpcClient.PopupOpen(Title, Contents, ID);
        }
    }

    public class Act_GiveObjList : ScriptedAction
    {
        public string Target = string.Empty;
        public string List = string.Empty;

        public Act_GiveObjList()
        {
        }

        public Act_GiveObjList(MissionAction act) : base(act)
        {
            GetString(nameof(Target), 0, out Target, act.Entry);
            GetString(nameof(List), 1, out List, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_GiveObjList", Target, List);
        }

        void GiveObjList(GameObject obj, MissionDirective[] directives)
        {
            if (obj.TryGetComponent<SPlayerComponent>(out var player))
            {
                player.SetDirectives(directives);
            }
            else if (obj.TryGetComponent<DirectiveRunnerComponent>(out var dr))
            {
                dr.SetDirectives(directives);
            }
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            MissionDirective[] ol;
            if ("no_ol".Equals(List, StringComparison.OrdinalIgnoreCase))
            {
                ol = null;
            }
            else
            {
                if (!script.ObjLists.ContainsKey(List))
                {
                    FLLog.Error("Mission", $"Could not find objlist {List}");
                    return;
                }
                ol = script.ObjLists[List].Directives.ToArray();
            }

            if (script.Formations.TryGetValue(Target, out var formation))
            {
                foreach (var s in formation.Ships)
                {
                    runtime.Player.Space.World.NPCs.NpcDoAction(s.Nickname,
                            (npc) => { GiveObjList(npc, ol); });
                }
            }
            else
            {
                runtime.Player.Space.World.EnqueueAction(() =>
                {
                    var tgt = runtime.Player.Space.World.GameWorld.GetObject(Target);
                    if (tgt == null)
                    {
                        FLLog.Error("Server", $"Act_GiveObjList can't find '{Target}'");
                    }
                    else
                    {
                        GiveObjList(tgt, ol);
                    }
                });
            }
        }
    }

    public class Act_ChangeState : ScriptedAction
    {
        public bool Succeed;
        public int Ids;

        public Act_ChangeState()
        {
        }

        public Act_ChangeState(MissionAction act) : base(act)
        {
            GetString("Succeed", 0, out var succeed, act.Entry);
            Succeed = succeed.Equals("SUCCEED", StringComparison.OrdinalIgnoreCase);
            if (act.Entry.Count > 1)
                Ids = act.Entry[1].ToInt32();
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            if(Ids == 0)
                section.Entry("Act_ChangeState", Succeed ? "SUCCEED" : "FAILED");
            else
                section.Entry("Act_ChangeState", Succeed ? "SUCCEED" : "FAIL", Ids);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (Succeed)
            {
                runtime.Player.MissionSuccess();
            }
            else if (!Succeed)
            {
                runtime.Player.SPMissionFailure(Ids);
            }
        }
    }

    public class Act_RevertCam : ScriptedAction
    {
        public Act_RevertCam()
        {
        }

        public Act_RevertCam(MissionAction act) : base(act) { }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_RevertCam", "no_params");
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.RpcClient.CallThorn(null, default);
        }
    }

    public class Act_CallThorn : ScriptedAction
    {
        public string Thorn = string.Empty;
        public string MainObject = string.Empty;

        public Act_CallThorn()
        {
        }
        public Act_CallThorn(MissionAction act) : base(act)
        {
            GetString(nameof(Thorn), 0, out Thorn, act.Entry);
            Thorn = act.Entry[0].ToString();
            if (act.Entry.Count > 1)
                MainObject = act.Entry[1].ToString();
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            List<ValueBase> values = [Thorn];
            if (!string.IsNullOrEmpty(MainObject))
            {
                values.Add(MainObject);
            }

            section.Entry("Act_CallThorn", values.ToArray());
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            FLLog.Info("Act_CallThorn", Thorn);
            runtime.Player.MissionWorldAction(() =>
            {
                ObjNetId mainObject = default;
                if (MainObject != null)
                {
                    var gameObj = runtime.Player.Space.World.GameWorld.GetObject(MainObject);
                    mainObject = gameObj;
                }
                FLLog.Info("Server", $"Calling Thorn {Thorn} with mainObject `{mainObject}`");
                runtime.Player.RpcClient.CallThorn(Thorn, mainObject);
            });
        }
    }


}
